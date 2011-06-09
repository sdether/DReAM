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
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Traum {

    /// <summary>
    /// Provides an implementation of <see cref="IPlugEndpoint2"/> to intercept <see cref="Verb.GET"/> plug invocations
    /// and proxy the response to a document held by the instance.
    /// </summary>
    public class ProxyPlugEndpoint : IPlugEndpoint2 {

        //--- Class Fields ---
        private static Dictionary<XUri, XDoc> _map = new Dictionary<XUri, XDoc>();

        //--- Class Methods ---

        /// <summary>
        /// Add a document for a uri.
        /// </summary>
        /// <param name="uri">Uri to intercept.</param>
        /// <param name="doc">Document to return for interception.</param>
        public static void Add(XUri uri, XDoc doc) {
            if(uri == null) {
                throw new ArgumentException("uri");
            }
            if(doc == null) {
                throw new ArgumentException("doc");
            }
            lock(_map) {
                _map[uri] = doc;
            }
        }

        /// <summary>
        /// Remove a uri from the proxy.
        /// </summary>
        /// <param name="uri">Uri to remove.</param>
        public static void Remove(XUri uri) {
            lock(_map) {
                _map.Remove(uri);
            }
        }

        //--- Interface Methods ---
        int IPlugEndpoint2.GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            XDoc doc;
            normalized = uri;
            lock(_map) {
                if(!_map.TryGetValue(uri, out doc)) {
                    return 0;
                }
            }
            return uri.MaxSimilarity;
        }

        Task<DreamMessage2> IPlugEndpoint2.Invoke(Plug2 plug, string verb, XUri uri, DreamMessage2 request, TimeSpan timeout) {
            
            // we only support GET as verb
            TaskCompletionSource<DreamMessage2> result = new TaskCompletionSource<DreamMessage2>();
            DreamMessage2 reply;
            if(verb != Verb.GET) {
                reply = new DreamMessage2(DreamStatus.MethodNotAllowed, null, null);
                reply.Headers.Allow = Verb.GET;
            } else {
                XDoc doc;
                lock(_map) {
                    if(!_map.TryGetValue(uri, out doc)) {
                        reply = DreamMessage2.NotFound("resource has been removed");
                    } else {
                        reply = DreamMessage2.Ok(doc);
                    }
                }
            }
            request.Close();
            result.SetResult(reply);
            return result.Task;
        }
    }
}
