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

using log4net;
using MindTouch;

namespace System {

    /// <summary>
    /// Static utility class containing extension and helper methods for Debug instrumentation.
    /// </summary>
    public static class DebugUtil {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        /// <summary>
        /// Global flag to signal whether stack traces should be captured.
        /// </summary>
        public static bool CaptureStackTrace = false;

        //--- Class Methods ---

        /// <summary>
        /// Get the current StackTrace.
        /// </summary>
        /// <returns>Currently applicable StackTrace.</returns>
        public static System.Diagnostics.StackTrace GetStackTrace() {
            if(CaptureStackTrace) {
                return new System.Diagnostics.StackTrace(1, true);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Wrap a stopwatch aroudn the execution of an action.
        /// </summary>
        /// <param name="handler">The action to be timed.</param>
        /// <returns>Time elapsed during the handler's execution.</returns>
        public static TimeSpan Stopwatch(Action handler) {
            var s = new Diagnostics.Stopwatch();
            s.Start();
            handler();
            s.Stop();
            return s.Elapsed;
        }
    }
}