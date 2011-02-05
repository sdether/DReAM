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
    /// Does not implement <see cref="IThreadsafePriorityQueue{T}.TryDequeue"/>, since all enqueued items are internally dispatched.
    /// </remarks>
    /// <typeparam name="T">Type of work item that can be dispatched.</typeparam>
    public class ElasticPriorityWorkQueue<T> : IThreadsafePriorityQueue<T>, IDisposable {

        //--- Fields ---
        private readonly ElasticPriorityThreadPool _pool;
        private readonly Action<T> _handler;
        private bool _disposed;

        //--- Constructors ---

        /// <summary>
        /// Create an instance of the work queue.
        /// </summary>
        /// <param name="minReservedThreads">Minimum number of threads that will always be available to the work queue.</param>
        /// <param name="maxParallelThreads">Maximum number of threads the work queue will use for dispatching work.</param>
        /// <param name="maxPriority">Maximum priority for <see cref="TryEnqueue"/>.</param>
        /// <param name="handler">Dispatch action for work item Type.</param>
        public ElasticPriorityWorkQueue(int minReservedThreads, int maxParallelThreads, int maxPriority, Action<T> handler) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            _pool = new ElasticPriorityThreadPool(minReservedThreads, maxParallelThreads, maxPriority);
            _handler = handler;
        }

        /// <summary>
        /// Finalizer for work queue to dispose of worker pool.
        /// </summary>
        ~ElasticPriorityWorkQueue() {
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
        /// Maximum allowed priority for an enqueued item.
        /// </summary>
        public int MaxPriority { get { return _pool.MaxPriority; } }

        //--- Methods ---
        /// <summary>
        /// Try to queue a work item for dispatch.
        /// </summary>
        /// <param name="priority">Priority of the added item.</param>
        /// <param name="item">Item to add to queue.</param>
        /// <returns><see langword="True"/> if the enqueue succeeded.</returns>
        public bool TryEnqueue(int priority, T item) {
            try {
                _pool.QueueWorkItem(priority, () => _handler(item));
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
