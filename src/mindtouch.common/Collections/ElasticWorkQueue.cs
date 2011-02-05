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

using MindTouch.Threading;

namespace MindTouch.Collections {
    
    /// <summary>
    /// Provides a mechanism for dispatching work items against an <see cref="ElasticPriorityThreadPool"/>.
    /// </summary>
    /// <remarks>
    /// Does not implement <see cref="IThreadsafeQueue{T}.TryDequeue"/>, since all enqueued items are internally dispatched.
    /// </remarks>
    /// <typeparam name="T">Type of work item that can be dispatched.</typeparam>
    public class ElasticWorkQueue<T> : IThreadsafeQueue<T>, IDisposable {

        //--- Fields ---
        private readonly ElasticThreadPool _pool;
        private readonly Action<T> _handler;
        private bool _disposed;

        //--- Constructors ---

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="minReservedThreads">Minimum number of threads that will always be available to the work queue.</param>
        /// <param name="maxParallelThreads">Maximum number of threads the work queue will use for dispatching work.</param>
        /// <param name="handler">Dispatch action for work item Type.</param>
        public ElasticWorkQueue(int minReservedThreads, int maxParallelThreads, Action<T> handler) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            _pool = new ElasticThreadPool(minReservedThreads, maxParallelThreads);
            _handler = handler;
        }

        /// <summary>
        /// Finalizer for queue to dispose of worker pool.
        /// </summary>
        ~ElasticWorkQueue() {
            if(!_disposed && !Environment.HasShutdownStarted) {

                // NOTE (steveb): we need to invoke Dispose() on the ElasticThreadPool, because it's being referenced by the GlobalClock which
                //                will prevent it from ever being garbage collected.
                _pool.Dispose();
            }
        }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the queue is empty.
        /// </summary>
        public bool IsEmpty { get { return _pool.ItemCount == 0; } }

        /// <summary>
        /// Total number of items in queue.
        /// </summary>
        public int Count { get { return (int)_pool.ItemCount; } }

        //--- Methods ---

        /// <summary>
        /// Try to queue a work item for dispatch.
        /// </summary>
        /// <param name="item">Item to add to queue.</param>
        /// <returns><see langword="True"/> if the enqueue succeeded.</returns>
        public bool TryEnqueue(T item) {
            try {
                _pool.QueueWorkItem(() => _handler(item));
                return true;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// This method is not supported and throws <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"/>
        public bool TryDequeue(out T item) {
            throw new NotSupportedException("ElasticWorkQueue does not support TryDequeue(out T)");
        }

        /// <summary>
        /// Release the resources reserved by the work queue from the global thread pool.
        /// </summary>
        public void Dispose() {
            if(!_disposed) {
                GC.SuppressFinalize(this);
                _disposed = true;
                _pool.Dispose();
            }
        }
    }
}
