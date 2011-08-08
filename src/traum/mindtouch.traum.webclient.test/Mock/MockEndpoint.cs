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
using log4net;

namespace MindTouch.Traum.Webclient.Test.Mock {
    internal class MockEndpoint : IPlugEndpoint2 {

        //--- Class Fields ---
        public static readonly MockEndpoint Instance = new MockEndpoint();

        // Note (arnec): This is a field, not constant so that access triggers the static constructor
        public readonly static string DEFAULT = "mock://mock";
        private static readonly MockPlug2.IMockInvokee DefaultInvokee = new MockPlug2.MockInvokee(null, (p, v, u, r) => DreamMessage2.Ok().AsCompletedTask(), int.MaxValue);
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Constructors ---
        static MockEndpoint() {
            Plug2.AddEndpoint(Instance);
        }

        //--- Fields ---
        private readonly Dictionary<XUri, MockPlug2.IMockInvokee> _registry = new Dictionary<XUri, MockPlug2.IMockInvokee>();
        private readonly XUriMap<MockPlug2.IMockInvokee> _map = new XUriMap<MockPlug2.IMockInvokee>();

        //--- Events ---
        public event EventHandler AllDeregistered;

        //--- Constructors ---
        private MockEndpoint() { }

        //--- Methods ---
        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            var match = GetBestMatch(uri);
            normalized = uri;
            _log.DebugFormat("considering uri '{0}' with score {1}", uri, match == null ? 0 : match.EndPointScore);
            return match == null ? 0 : match.EndPointScore;
        }


        private MockPlug2.IMockInvokee GetBestMatch(XUri uri) {
            MockPlug2.IMockInvokee invokee;

            // using _registry as our guard for both _map and _registry, since they are always modified in sync
            lock(_registry) {
                int result;
                _map.TryGetValue(uri, out invokee, out result);
            }
            if(invokee != null) {
                return invokee;
            }
            return uri.SchemeHostPort.EqualsInvariant(DEFAULT) ? DefaultInvokee : null;
        }

        public Task<DreamMessage2> Invoke(Plug2 plug, string verb, XUri uri, DreamMessage2 request, TimeSpan timeout) {
            var match = GetBestMatch(uri);
            _log.DebugFormat("invoking uri '{0}'", uri);
            return Task.Factory.StartNew(() => match.Invoke(plug, verb, uri, MemorizeAndClone(request)).Result);
        }

        public void Register(MockPlug2.IMockInvokee invokee) {
            lock(_registry) {
                if(_registry.ContainsKey(invokee.Uri)) {
                    throw new ArgumentException("the uri already has a mock registered");
                }
                _registry.Add(invokee.Uri, invokee);
                _map.Add(invokee.Uri, invokee);
            }
        }

        public void Deregister(XUri uri) {
            lock(_registry) {
                if(!_registry.ContainsKey(uri)) {
                    return;
                }
                _registry.Remove(uri);
                _map.Remove(uri);
            }
        }

        public void DeregisterAll() {
            lock(_registry) {
                _registry.Clear();
                _map.Clear();
                if(AllDeregistered != null) {
                    AllDeregistered(this, EventArgs.Empty);
                }
                AllDeregistered = null;
            }
        }

        private DreamMessage2 MemorizeAndClone(DreamMessage2 request) {
            return request.IsCloneable ? request.Clone() : new DreamMessage2(request.Status,request.Headers,request.ContentType,request.ToBytes());
        }
    }
}
