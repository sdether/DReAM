/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2013 MindTouch, Inc.
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
using System.Linq;
using log4net;
using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;

namespace MindTouch.Aws {
    public class SqsPollClient : ISqsPollClient {

        //--- Types ---
        public class Item {

            //--- Fields ---
            public readonly AwsSqsMessage Message;

            //--- Constructors ---
            public Item(AwsSqsMessage message) {
                Message = message;
            }

            //--- Properties ---
            public bool IsDeleted { get; private set; }

            //--- Methods ---
            public void Delete() {
                IsDeleted = true;
            }
        }

        private class Listener : IDisposable {

            //--- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog();

            //--- Fields ---
            private readonly string _queuename;
            private readonly Action<IEnumerable<Item>> _callback;
            private readonly IAwsSqsClient _client;
            private readonly int _maxMessagesPerPoll;
            private readonly TaskTimer _pollTimer;
            private readonly ExpiringHashSet<string> _cache;
            private bool _isDisposed;
            private readonly TimeSpan _cacheTimer;

            //--- Constructors ---
            public Listener(string queuename, Action<IEnumerable<Item>> callback, IAwsSqsClient client, TaskTimerFactory timerFactory, TimeSpan interval, int maxMessagesPerPoll) {
                _queuename = queuename;
                _callback = callback;
                _client = client;
                _maxMessagesPerPoll = maxMessagesPerPoll;
                _cache = new ExpiringHashSet<string>(timerFactory);
                _cacheTimer = ((interval.TotalSeconds * 2 < 60) ? 60 : interval.TotalSeconds * 2 + 1).Seconds();
                _pollTimer = timerFactory.New(tt => Coroutine.Invoke(PollSqs, new Result()).WhenDone(r => _pollTimer.Change(interval, TaskEnv.None)), null);
                _pollTimer.Change(0.Seconds(), TaskEnv.None);
            }

            //--- Methods ---
            private IEnumerator<IYield> PollSqs(Result result) {
                _log.DebugFormat("polling SQS queue '{0}'", _queuename);
                var messages = new List<Item>();
                while(!_isDisposed && messages.Count < _maxMessagesPerPoll) {
                    Result<IEnumerable<AwsSqsMessage>> messageResult;
                    yield return messageResult = _client.Receive(_queuename, Math.Min(AwsSqsDefaults.MAX_MESSAGES,_maxMessagesPerPoll), new Result<IEnumerable<AwsSqsMessage>>()).Catch();
                    if(messageResult.HasException) {
                        LogError(messageResult.Exception, "fetching messages");
                        break;
                    }
                    if(!messageResult.Value.Any()) {
                        break;
                    }
                    messages.AddRange(messageResult.Value
                        .Where(msg => !_cache.SetOrUpdate(msg.MessageId, _cacheTimer))
                        .Select(x => new Item(x))
                    );
                }
                if(messages.None()) {
                    result.Return();
                    yield break;
                }
                try {
                    _callback(messages);
                } catch(Exception e) {
                    _log.Warn(
                        string.Format("dispatching bulk messages from '{0}' threw '{1}': {2}",
                            _queuename,
                            e,
                            e.Message
                        ),
                        e
                    );
                }
                foreach(var toDelete in messages.Where(x => x.IsDeleted)) {
                    Result<AwsSqsResponse> deleteResult;
                    yield return deleteResult = _client.Delete(toDelete.Message, new Result<AwsSqsResponse>()).Catch();
                    if(deleteResult.HasException) {
                        LogError(deleteResult.Exception, string.Format("deleting message '{0}'", toDelete.Message.MessageId));
                    } else {
                        _cache.SetOrUpdate(toDelete.Message.MessageId, _cacheTimer);
                    }
                }
                result.Return();
            }

            private void LogError(Exception e, string prefix) {
                var awsException = e as AwsSqsRequestException;
                if(awsException != null && awsException.IsSqsError) {
                    _log.WarnFormat("{0} resulted in AWS error {1}/{2}: {3}",
                        prefix,
                        awsException.Error.Code,
                        awsException.Error.Type,
                        awsException.Error.Message
                    );
                    return;
                }
                _log.Warn(string.Format("{0} resulted in non-AWS exception: {1}", prefix, e.Message), e);
            }

            public void Dispose() {
                _isDisposed = true;
                _pollTimer.Cancel();
            }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly IAwsSqsClient _client;
        private readonly TaskTimerFactory _timerFactory;
        private readonly int _maxMessagesPerPoll;
        private readonly List<Listener> _listeners = new List<Listener>();

        //--- Constructors ---
        public SqsPollClient(IAwsSqsClient client, TaskTimerFactory timerFactory, int maxMessagesPerPoll = 1000) {
            if(client == null) {
                throw new ArgumentNullException("client");
            }
            if(timerFactory == null) {
                throw new ArgumentNullException("timerFactory");
            }
            if(maxMessagesPerPoll < 1) {
                throw new ArgumentException("Cannot specify a max less than 1","maxMessagesPerPoll");
            }
            _client = client;
            _timerFactory = timerFactory;
            _maxMessagesPerPoll = maxMessagesPerPoll;
        }

        //--- Methods ---
        public void ListenMany(string queuename, TimeSpan pollInterval, Action<IEnumerable<Item>> callback) {
            _listeners.Add(new Listener(queuename, callback, _client, _timerFactory, pollInterval, _maxMessagesPerPoll));
        }

        public void Listen(string queuename, TimeSpan pollInterval, Action<AwsSqsMessage> callback) {
            _listeners.Add(new Listener(queuename, items => {
                foreach(var item in items) {
                    try {
                        callback(item.Message);
                        item.Delete();
                    } catch(Exception e) {
                        _log.Warn(
                            string.Format("dispatching message {0} from '{1}' threw '{2}': {3}",
                                item.Message,
                                queuename,
                                e,
                                e.Message
                            ),
                            e
                        );
                    }
                }
            }, _client, _timerFactory, pollInterval, _maxMessagesPerPoll));
        }



        public void Dispose() {
            foreach(var listener in _listeners) {
                listener.Dispose();
            }
            _listeners.Clear();
        }
    }
}