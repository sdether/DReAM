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
using System.Threading;
using MindTouch.Collections;

using MindTouch.Tasking;
using MindTouch.Threading.Timer;

namespace MindTouch.Threading {
    internal static class DispatchThreadManager {

        //--- Constants ---
        private static readonly TimeSpan IDLE_TIME_LIMIT = TimeSpan.FromSeconds(6);

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static object _syncRoot = new object();
        private static readonly IThreadsafeStack<KeyValuePair<DispatchThread, Result<Action>>> _idleThreads = new LockFreeStack<KeyValuePair<DispatchThread, Result<Action>>>();
        private static readonly int _maxThreads;
        private static int _allocatedThreads;
        private static TimeSpan _idleTime = TimeSpan.Zero;

        //--- Class Constructors ---
        static DispatchThreadManager() {

            // read system wide max-thread setting
            if(!int.TryParse(System.Configuration.ConfigurationManager.AppSettings["max-dispatch-threads"], out _maxThreads)) {

                // TODO (steveb): we should base this on available memory (e.g. total_memory / 2 / 1MB_stack_size_per_thread)
                _maxThreads = 1000;
            }

            // add maintenance callback
            GlobalClock.AddCallback("DispatchThreadManager", Tick);
        }

        //--- Class Properties ---
        public static int AllocatedThreadCount { get { return _allocatedThreads; } }
        public static int MaxThreadCount { get { return _maxThreads; } }
        public static int AvailableThreadCount { get { return _maxThreads - _allocatedThreads; } }

        //--- Class Methods ---
        public static bool RequestThread(IDispatchHost host, out DispatchThread thread, out Result<Action> result) {
            if(host == null) {
                throw new ArgumentNullException("host");
            }

            // reset idle time
            _idleTime = TimeSpan.Zero;

            // check if an idle thread is available
            KeyValuePair<DispatchThread, Result<Action>> entry;
            if(!_idleThreads.TryPop(out entry)) {
                bool create = false;
                lock(_syncRoot) {

                    // check if we can create another thread
                    if(_allocatedThreads < _maxThreads) {
                        Interlocked.Increment(ref _allocatedThreads);
                        create = true;
                    } else {
                        _log.InfoMethodCall("RequestThread: max threads reached for app domain");
                    }
                }

                // NOTE (steveb): moved outside of the lock, just in case
                if(create) {

                    // create a new thread
                    thread = new DispatchThread(host);
                    result = null;
                    return true;
                }

                // unable to find or create a new thread
                thread = null;
                result = null;
                return false;
            }

            // bind idle thread to new host
            entry.Key.Host = host;

            // set out parameters
            thread = entry.Key;
            result = entry.Value;
            return true;
        }

        public static void ReleaseThread(IDispatchHost host, DispatchThread thread, Result<Action> result) {
            if(host == null) {
                throw new ArgumentNullException("host");
            }
            if(thread == null) {
                throw new ArgumentNullException("thread");
            }
            if(result == null) {
                throw new ArgumentNullException("result");
            }

            // validate that the right host is releasing this thread
            if(!ReferenceEquals(thread.Host, host)) {
                throw new InvalidOperationException("thread is allocated to another host");
            }

            // unbind thread from current host
            thread.Host = null;

            // add thread to list of idle threads
            if(!_idleThreads.TryPush(new KeyValuePair<DispatchThread, Result<Action>>(thread, result))) {
                throw new NotSupportedException("TryPush failed");
            }
        }

        private static void Tick(DateTime now, TimeSpan elapsed) {

            // check if resource manager has been idle for a while
            _idleTime += elapsed;
            if(_idleTime > IDLE_TIME_LIMIT) {
                _idleTime = TimeSpan.Zero;

                // try discarding an idle thread
                KeyValuePair<DispatchThread, Result<Action>> entry;
                if(_idleThreads.TryPop(out entry)) {
                    Interlocked.Decrement(ref _allocatedThreads);
                    entry.Value.Throw(new DispatchThreadShutdownException());
                }                
            }
        }
    }
}
