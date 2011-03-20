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
using log4net;

using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream.Test {

    /// <summary>
    /// Provides an <see cref="IDreamService"/> skeleton implemenation with static instance accessor and callback mechanism to externally
    /// intercept service behavior.
    /// </summary>
    [DreamService("MockService", "Copyright (c) 2006-2011 MindTouch, Inc.",
        Info = "",
        SID = new[] { SID }
        )]
    public class MockService : DreamService {

        //--- Constants ---

        /// <summary>
        /// Service Identifier of <see cref="MockService"/>.
        /// </summary>
        public new const string SID = "http://services.mindtouch.com/dream/stable/2008/10/mock";

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        internal static Dictionary<string, MockService> MockRegister = new Dictionary<string, MockService>();

        //--- Static Methods ---

        /// <summary>
        /// Create a new mock service instance.
        /// </summary>
        /// <param name="hostInfo">Host info.</param>
        /// <param name="extraConfig">Additional service configuration.</param>
        /// <returns>New mock service info instance.</returns>
        public static MockServiceInfo CreateMockService(DreamHostInfo hostInfo, XDoc extraConfig) {
            string path = StringUtil.CreateAlphaNumericKey(8);
            XDoc config = new XDoc("config")
                .Elem("class", typeof(MockService).FullName)
                .Elem("path", path);
            if(extraConfig != null) {
                foreach(XDoc extra in extraConfig["*"]) {
                    config.Add(extra);
                }
            }
            hostInfo.Host.Self.At("services").Post(config);
            _log.DebugFormat("path: {0}", path);
            return new MockServiceInfo(hostInfo, path, MockRegister[path]);
        }

        /// <summary>
        /// Create a new mock service instance.
        /// </summary>
        /// <param name="hostInfo">Host info.</param>
        /// <returns>New mock service info instance.</returns>
        public static MockServiceInfo CreateMockService(DreamHostInfo hostInfo) {
            return CreateMockService(hostInfo, null);
        }

        //--- Fields ---

        /// <summary>
        /// Synchronous catch all callback (mutually exclusive with <see cref="CatchAllCallbackAsync"/>).
        /// </summary>
        public Action<DreamContext,DreamMessage,Result<DreamMessage>> CatchAllCallback;

        /// <summary>
        /// Asynchronous catch all callback (mutually exclusive with <see cref="CatchAllCallback"/>).
        /// </summary>
        public Func<DreamContext,DreamMessage,Result<DreamMessage>,Result<DreamMessage>> CatchAllCallbackAsync;

        /// <summary>
        /// Service configuration.
        /// </summary>
        public XDoc ServiceConfig;

        //--- Features ---

        /// <summary>
        /// Catch all feature.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        [DreamFeature("*://*", "catchall")]
        public IEnumerator<IYield> CatchAll(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            _log.DebugFormat("Catchall called on {0}", context.Uri);
            if(CatchAllCallbackAsync != null) {
                Result<DreamMessage> subresponse;
                yield return subresponse = CatchAllCallbackAsync(context, request, new Result<DreamMessage>()).Catch();
                response.Return(subresponse);
            } else {
                CatchAllCallback(context, request, response);
            }
            yield break;
        }

        //--- Methods ---
        /// <summary>
        /// Mock start.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected override IEnumerator<IYield> Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _log.DebugFormat("registered: {0}", config["path"].Contents);
            MockRegister.Add(config["path"].Contents, this);
            ServiceConfig = config;
            result.Return();
        }
    }
}
