/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Web;

namespace MindTouch.Traum.Http {

    internal class HttpPlugEndpoint : IPlugEndpoint2 {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();
        private static readonly Dictionary<Guid, List<Task<DreamMessage2>>> _requests = new Dictionary<Guid, List<Task<DreamMessage2>>>();

        //--- Methods ---
        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            normalized = uri;
            switch(uri.Scheme.ToLowerInvariant()) {
            case "http":
            case "https":
                return 1;
            case "ext-http":
                normalized = normalized.WithScheme("http");
                return int.MaxValue;
            case "ext-https":
                normalized = normalized.WithScheme("https");
                return int.MaxValue;
            default:
                return 0;
            }
        }

        public Task<DreamMessage2> Invoke(Plug2 plug, string verb, XUri uri, DreamMessage2 request, TimeSpan timeout) {

            // register activity
            Action<string> activity = delegate(string message) { };
            activity("pre Invoke");

            // await
            var res = HandleInvoke(activity, plug, verb, uri, request, timeout);
            activity("post Invoke");
            request.Close();

            // unregister activity
            activity(null);

            // return response
            return res;
        }

        private Task<DreamMessage2> HandleInvoke(Action<string> activity, Plug2 plug, string verb, XUri uri, DreamMessage2 request, TimeSpan timeout) {

            // remove internal headers
            request.Headers.DreamTransport = null;

            // set request headers
            request.Headers.Host = uri.Host;
            if(request.Headers.UserAgent == null) {
                request.Headers.UserAgent = "Dream/" + DreamUtil.DreamVersion;
            }

            // add cookies to request
            if(request.HasCookies) {
                request.Headers[DreamHeaders.COOKIE] = DreamCookie.RenderCookieHeader(request.Cookies);
            }

            // check if we can pool the request with an existing one
            if((plug.Credentials == null) && StringUtil.ContainsInvariantIgnoreCase(verb, "GET")) {

                // create the request hashcode
                StringBuilder buffer = new StringBuilder();
                buffer.AppendLine(uri.ToString());
                foreach(KeyValuePair<string, string> header in request.Headers) {
                    buffer.Append(header.Key).Append(": ").Append(header.Value).AppendLine();
                }
                Guid hash = new Guid(StringUtil.ComputeHash(buffer.ToString()));

                // check if an active connection exists
                //Task<DreamMessage2> relay = null;
                //lock(_requests) {
                //    List<Task<DreamMessage2>> pending;
                //    relay = new TaskCompletionSource<DreamMessage2>();
                //    if(_requests.TryGetValue(hash, out pending)) {
                //        pending.Add(relay);
                //    } else {
                //        pending = new List<Task<DreamMessage2>>();
                //        pending.Add(response);
                //        _requests[hash] = pending;
                //    }
                //}

                // check if we're pooling a request
                //if(relay != null) {

                //    // wait for the relayed response
                //    yield return relay;
                //    response.Return(relay);
                //    yield break;
                //} else {

                // NOTE (steveb): we use TaskEnv.Instantaneous so that we don't exit the current stack frame before we've executed the continuation;
                //                otherwise, we'll trigger an exception because our result object may not be set.

                // create new handler to multicast the response to the relays
                //response = new Result<DreamMessage2>(response.Timeout, TaskEnv.Instantaneous);
                //response.WhenDone(_ => {
                //    List<Result<DreamMessage2>> pending;
                //    lock(_requests) {
                //        _requests.TryGetValue(hash, out pending);
                //        _requests.Remove(hash);
                //    }

                //    // this check should never fail!
                //    if(response.HasException) {

                //        // send the exception to all relays
                //        foreach(Result<DreamMessage2> result in pending) {
                //            result.Throw(response.Exception);
                //        }
                //    } else {
                //        DreamMessage2 original = response.Value;

                //        // only memorize the message if it needs to be cloned
                //        if(pending.Count > 1) {

                //            // clone the message to all relays
                //            foreach(Result<DreamMessage2> result in pending) {
                //                result.Return(original.Clone());
                //            }
                //        } else {

                //            // relay the original message
                //            pending[0].Return(original);
                //        }
                //    }
                //});
                //} **relay** 
            }

            // initialize request
            activity("pre WebRequest.Create");
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(uri.ToUri());
            activity("post WebRequest.Create");
            httpRequest.Method = verb;
            httpRequest.Timeout = System.Threading.Timeout.Infinite;
            httpRequest.ReadWriteTimeout = System.Threading.Timeout.Infinite;

            // Note from http://support.microsoft.com/kb/904262
            // The HTTP request is made up of the following parts:
            // 1.   Sending the request is covered by using the HttpWebRequest.Timeout method.
            // 2.   Getting the response header is covered by using the HttpWebRequest.Timeout method.
            // 3.   Reading the body of the response is not covered by using the HttpWebResponse.Timeout method. In ASP.NET 1.1 and in later versions, reading the body of the response 
            //      is covered by using the HttpWebRequest.ReadWriteTimeout method. The HttpWebRequest.ReadWriteTimeout method is used to handle cases where the response headers are 
            //      retrieved in a timely manner but where the reading of the response body times out.

            httpRequest.KeepAlive = false;
            httpRequest.ProtocolVersion = System.Net.HttpVersion.Version10;

            // set credentials
            if(plug.Credentials != null) {
                httpRequest.Credentials = plug.Credentials;
                httpRequest.PreAuthenticate = true;
            } else if(!string.IsNullOrEmpty(uri.User) || !string.IsNullOrEmpty(uri.Password)) {
                httpRequest.Credentials = new NetworkCredential(uri.User ?? string.Empty, uri.Password ?? string.Empty);
                httpRequest.PreAuthenticate = true;
                var authbytes = Encoding.ASCII.GetBytes(string.Concat(uri.User ?? string.Empty, ":", uri.Password ?? string.Empty));
                var base64 = Convert.ToBase64String(authbytes);
                httpRequest.Headers.Add("Authorization", "Basic " + base64);
            }

            // add request headres
            foreach(KeyValuePair<string, string> header in request.Headers) {
                HttpUtil.AddHeader(httpRequest, header.Key, header.Value);
            }
            // send message stream
            if((request.ContentLength != 0) || (verb == Verb.POST)) {
                Task<Stream> getRequestStream = null;
                try {
                    activity("pre BeginGetRequestStream");
                    getRequestStream = httpRequest.GetRequestStreamAsync();
                    activity("post BeginGetRequestStream");
                } catch(Exception e) {
                    activity("pre HandleResponse 1");
                    return HandleResponse(activity, e, httpRequest, null, timeout);
                    //if(response == null) {
                    //    _log.ErrorExceptionMethodCall(e, "HandleInvoke@BeginGetRequestStream", verb, uri);
                    //    throw e;
                    //}
                    //return response;
                }
                activity("pre await getRequestStream");
                // send request
                Stream outStream;
                try {
                    // await
                    outStream = getRequestStream;
                    activity("post await getRequestStream");
                } catch(Exception e) {
                    activity("pre HandleResponse 2");
                    return HandleResponse(activity, e, httpRequest, null, timeout);
                    //if(response == null) {
                    //    _log.ErrorExceptionMethodCall(e, "HandleInvoke@getRequestStream", verb, uri);
                    //    throw e;
                    //}
                    //return response;
                }

                // copy data
                using(outStream) {
                    try {
                        activity("pre yield CopyStream");
                        // await
                        request.ToStream().CopyToAsync(outStream, (int)request.ContentLength);
                        activity("post yield CopyStream");
                    } catch(Exception e) {
                        activity("pre HandleResponse 3");
                        return HandleResponse(activity, e, httpRequest, null, timeout);
                        //if(response == null) {
                        //    _log.ErrorExceptionMethodCall(e, "HandleInvoke@CopyToAsync", verb, uri);
                        //    throw e;
                        //}
                        //return response;
                    }
                }
            }
            request = null;

            // wait for response
            HttpWebResponse httpResponse = null;
            try {
                activity("pre await GetResponseAsync");
                // await
                httpResponse = (HttpWebResponse) httpRequest.GetResponseAsync();
                activity("post await GetResponseAsync");
            } catch(Exception e) {
                activity("pre HandleResponse 4");
                return HandleResponse(activity, e, httpRequest, null, timeout);
                //if(response == null) {
                //    _log.ErrorExceptionMethodCall(e, "HandleInvoke@GetResponseAsync", verb, uri);
                //    throw e;
                //}
                //return response;
            }

            // handle response
            activity("pre HandleResponse 6");
            return HandleResponse(activity, null, httpRequest, httpResponse, timeout);
            //if(response == null) {
            //    _log.ErrorExceptionMethodCall(e, "HandleInvoke@GetResponseAsync", verb, uri);
            //    throw e;
            //}
            //return response;
        }

        private DreamMessage2 HandleResponse(Action<string> activity, Exception exception, HttpWebRequest httpRequest, HttpWebResponse httpResponse, TimeSpan timeout) {
            if(exception != null) {
                if(exception is WebException) {
                    activity("pre WebException");
                    httpResponse = (HttpWebResponse)((WebException)exception).Response;
                    activity("post WebException");
                } else {
                    activity("pre HttpWebResponse close");
                    try {
                        httpResponse.Close();
                    } catch { }
                    activity("HandleResponse exit 1");
                    httpRequest.Abort();
                    return new DreamMessage2(DreamStatus.UnableToConnect, exception);
                }
            }

            // check if a response was obtained, otherwise fail
            if(httpResponse == null) {
                activity("HandleResponse exit 2");
                httpRequest.Abort();
                return new DreamMessage2(DreamStatus.UnableToConnect, exception);
            }

            // determine response type
            MimeType contentType;
            Stream stream;
            HttpStatusCode statusCode = httpResponse.StatusCode;
            WebHeaderCollection headers = httpResponse.Headers;
            long contentLength = httpResponse.ContentLength;

            if(!string.IsNullOrEmpty(httpResponse.ContentType)) {
                contentType = new MimeType(httpResponse.ContentType);
                activity("pre new BufferedStream");
                stream = new BufferedStream(httpResponse.GetResponseStream());
                activity("post new BufferedStream");
            } else {

                // TODO (arnec): If we get a response with a stream, but no content-type, we're currently dropping the stream. Might want to revisit that.
                _log.DebugFormat("response ({0}) has not content-type and content length of {1}", statusCode, contentLength);
                contentType = null;
                stream = Stream.Null;
                httpResponse.Close();
            }

            // encapsulate the response in a dream message
            activity("HandleResponse exit 3");
            return new DreamMessage2((DreamStatus)(int)statusCode, new DreamHeaders(headers), contentType, contentLength, stream);
        }
    }
}
