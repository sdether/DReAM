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

namespace MindTouch.Traum {

    internal class ResourcePlugEndpoint : IPlugEndpoint2 {

        //--- Methods ---
        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            normalized = uri;
            switch(uri.Scheme) {
            case "resource":
                return 1;
            default:
                return 0;
            }
        }

        Task<DreamMessage2> IPlugEndpoint2.Invoke(Plug2 plug, string verb, XUri uri, DreamMessage2 request, TimeSpan timeout) {

            // we only support GET as verb
            TaskCompletionSource<DreamMessage2> result = new TaskCompletionSource<DreamMessage2>();
            DreamMessage2 reply;
            if((verb != Verb.GET) && (verb != Verb.HEAD)) {
                reply = new DreamMessage2(DreamStatus.MethodNotAllowed, null, null);
                reply.Headers.Allow = Verb.GET + "," + Verb.HEAD;
            } else {
                bool head = (verb == Verb.HEAD);

                // try to load the assembly
                System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(uri.Host);
                Version version = assembly.GetName().Version;
                DateTime timestamp = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);

                // check if request is just about re-validation
                if(!head && request.CheckCacheRevalidation(timestamp)) {
                    reply = DreamMessage2.NotModified();
                } else {
                    try {
                        System.IO.Stream stream = assembly.GetManifestResourceStream(uri.Path.Substring(1));
                        if(stream != null) {
                            MimeType mime = MimeType.New(uri.GetParam(DreamOutParam.TYPE, null)) ?? MimeType.BINARY;
                            reply = new DreamMessage2(DreamStatus.Ok, null, mime, stream.Length, head ? System.IO.Stream.Null : stream);
                            if(head) {
                                stream.Close();
                            } else {
                                reply.SetCacheMustRevalidate(timestamp);
                            }
                        } else {
                            reply = DreamMessage2.NotFound("could not find resource");
                        }
                    } catch(System.IO.FileNotFoundException) {
                        reply = DreamMessage2.NotFound("could not find resource");
                    } catch(Exception e) {
                        reply = DreamMessage2.InternalError(e);
                    }
                }
            }
            request.Close();
            result.SetResult(reply);
            return result.Task;
        }
    }
}
