/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Configuration;
using System.Linq;
using System.Text;
using MindTouch.Dream;
using MindTouch.Tasking;

namespace MindTouch.plug {
    public class DebugLogPlugEndpoint : IPlugEndpoint {

        //--- Constants ---
        private const string INTERCEPT_MARKER = "dream.debug.intercept";

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly HashSet<string> _debugInterceptPaths = new HashSet<string>();

        //--- Class Methods ---

        /// <summary>
        /// Contains the path prefixes that will be intercepted if the endpoint and debugging is enabled
        /// </summary>
        public static HashSet<string> DebugInterceptPaths {
            get {
                lock(_debugInterceptPaths) {
                    return _debugInterceptPaths;
                }
            }
        }

        /// <summary>
        /// Enable or disable the endpoint. However, log4net also needs to have debug logging enabled before the
        /// endpoint actually logs requests.
        /// </summary>
        public static bool Enabled { get; set; }

        /// <summary>
        /// Create a new endpoint (only exposed for <see cref="Plug"/> discovery)
        /// </summary>
        public DebugLogPlugEndpoint() {
            Configure();
        }

        private void Configure() {

            // TODO (arnec): need to pick up changes when appsettings change at runtime
            string intercepts = ConfigurationManager.AppSettings["plug.debug.uris"];
            string enabledSetting = ConfigurationManager.AppSettings["plug.debug.enabled"];
            if(!string.IsNullOrEmpty(enabledSetting)) {
                bool enabled;
                bool.TryParse(enabledSetting, out enabled);
                if(enabled && !string.IsNullOrEmpty(intercepts)) {
                    string[] uris = intercepts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(var uri in uris) {
                        DebugInterceptPaths.Add(uri);
                    }
                }
            }
        }

        int IPlugEndpoint.GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            if(Enabled && _log.IsDebugEnabled && !uri.GetParam(INTERCEPT_MARKER, false)) {
                var interceptPaths = DebugInterceptPaths;
                if(interceptPaths.Count > 0 && interceptPaths.Where(x => uri.Path.StartsWith(x)).Any()) {
                    normalized = uri;
                    return int.MaxValue;
                }
            }
            normalized = null;
            return 0;
        }

        IEnumerator<IYield> IPlugEndpoint.Invoke(Plug plug, string verb, XUri uri, DreamMessage request, Result<DreamMessage> response) {
            _log.DebugFormat("Debug intercept of {0}:{1}", verb, this);
            var builder = new StringBuilder();
            foreach(var header in request.Headers) {
                builder.Append(header);
                builder.Append(",");
            }
            if(builder.Length > 0) {
                _log.DebugFormat("--Headers: {0}", builder.ToString());
            }
            if(request.HasDocument) {
                _log.DebugFormat("--Documents:\r\n{0}", request.ToDocument().ToPrettyString());
            }
            Result<DreamMessage> res;
            yield return res = plug.With(INTERCEPT_MARKER, true).InvokeEx(verb, request, new Result<DreamMessage>());
            response.Return(res);
            //yield return plug.With(INTERCEPT_MARKER, true).InvokeEx(verb, request, new Result<DreamMessage>()).WhenDone(response.Return);
        }
    }
}
