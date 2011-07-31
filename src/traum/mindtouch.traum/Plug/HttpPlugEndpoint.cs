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
                var buffer = new StringBuilder();
                buffer.AppendLine(uri.ToString());
                foreach(KeyValuePair<string, string> header in request.Headers) {
                    buffer.Append(header.Key).Append(": ").Append(header.Value).AppendLine();
                }
            }

            // initialize request
            var httpRequest = (HttpWebRequest)WebRequest.Create(uri.ToUri());
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
            httpRequest.ProtocolVersion = HttpVersion.Version10;

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
            foreach(var header in request.Headers) {
                httpRequest.AddHeader(header.Key, header.Value);
            }
            var completion = new TaskCompletionSource<DreamMessage2>();
            // send message stream
            if((request.ContentLength != 0) || (verb == Verb.POST)) {
                Task<Stream> getRequestStream = null;
                try {
                    getRequestStream = Task.Factory.FromAsync<Stream>(httpRequest.BeginGetRequestStream, httpRequest.EndGetRequestStream, null);
                } catch(Exception e) {
                    return HandleResponse(e, httpRequest, null, timeout);
                }

                // send request
                getRequestStream.ContinueWith(t1 => {
                    if(t1.IsFaulted) {
                        var e = t1.UnwrapFault();
                        HandleResponse(e, httpRequest, null, timeout, completion);
                        return;
                    }
                    Stream outStream = t1.Result;

                    // copy data
                    using(outStream) {
                        try {
                            // await
                            request.ToStream().CopyToAsync(outStream, (int)request.ContentLength);
                        } catch(Exception e) {
                            return HandleResponse(e, httpRequest, null, timeout);
                        }
                    }
                });

            } else {

            }
            request = null;

            // wait for response
            HttpWebResponse httpResponse = null;
            try {
                // await
                httpResponse = (HttpWebResponse)httpRequest.GetResponseAsync();
            } catch(Exception e) {
                return HandleResponse(e, httpRequest, null, timeout);
            }

            // handle response
            return HandleResponse(null, httpRequest, httpResponse, timeout);
        }

        private void HandleResponse(Exception exception, HttpWebRequest httpRequest, HttpWebResponse httpResponse, TimeSpan timeout, TaskCompletionSource<DreamMessage2> completion) {
            completion.SetResult(HandleResponse2(exception, httpRequest, httpResponse, timeout));
        }

        private Task<DreamMessage2> HandleResponse(Exception exception, HttpWebRequest httpRequest, HttpWebResponse httpResponse, TimeSpan timeout) {
            return HandleResponse2(exception, httpRequest, httpResponse, timeout).AsCompletedTask();
        }

        private DreamMessage2 HandleResponse2(Exception exception, HttpWebRequest httpRequest, HttpWebResponse httpResponse, TimeSpan timeout) {
            if(exception != null) {
                if(exception is WebException) {
                    httpResponse = (HttpWebResponse)((WebException)exception).Response;
                } else {
                    try {
                        httpResponse.Close();
                    } catch { }
                    httpRequest.Abort();
                    return new DreamMessage2(DreamStatus.UnableToConnect, exception);
                }
            }

            // check if a response was obtained, otherwise fail
            if(httpResponse == null) {
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
                stream = new BufferedStream(httpResponse.GetResponseStream());
            } else {

                // TODO (arnec): If we get a response with a stream, but no content-type, we're currently dropping the stream. Might want to revisit that.
                _log.DebugFormat("response ({0}) has not content-type and content length of {1}", statusCode, contentLength);
                contentType = null;
                stream = Stream.Null;
                httpResponse.Close();
            }

            // encapsulate the response in a dream message
            return new DreamMessage2((DreamStatus)(int)statusCode, new DreamHeaders(headers), contentType, contentLength, stream);
        }
    }
}
