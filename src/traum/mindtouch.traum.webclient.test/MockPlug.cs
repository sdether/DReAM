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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindTouch.Traum.Webclient.Test.Mock;

namespace MindTouch.Traum.Webclient.Test {

    /// <summary>
    /// Provides a mocking framework for intercepting <see cref="Plug"/> calls.
    /// </summary>
    /// <remarks>
    /// Meant to be used to test services without having to set up dependent remote endpoints the service relies on for proper execution.
    /// <see cref="MockPlug"/> provides 2 different mechanisms for mocking an endpoint:
    /// <list type="bullet">
    /// <item>
    /// <see cref="MockPlug"/> endpoints, which can match requests based on content and supply a reply. These endpoints are order independent and
    /// can be set up to verifiable.
    /// </item>
    /// <item>
    /// <see cref="MockInvokeDelegate"/> endpoints which redirect an intercepted Uri (and child paths) to a delegate to be handled as the
    /// desired by the delegate implementor.
    /// </item>
    /// </list>
    /// </remarks>
    public class MockPlug : IMockPlug {

        //--- Types ---
        internal interface IMockInvokee {

            //--- Methods ---
            Task<DreamMessage> Invoke(Plug plug, string verb, XUri uri, DreamMessage request);

            //--- Properties ---
            int EndPointScore { get; }
            XUri Uri { get; }
        }

        internal class MockInvokee : IMockInvokee {

            //--- Fields ---
            private readonly XUri _uri;
            private readonly MockInvokeDelegate _callback;
            private readonly int _endpointScore;

            //--- Constructors ---
            public MockInvokee(XUri uri, MockInvokeDelegate callback, int endpointScore) {
                _uri = uri;
                _callback = callback;
                _endpointScore = endpointScore;
            }

            //--- Properties ---
            public int EndPointScore { get { return _endpointScore; } }
            public XUri Uri { get { return _uri; } }

            //--- Methods ---
            public Task<DreamMessage> Invoke(Plug plug, string verb, XUri uri, DreamMessage request) {
                return _callback(plug, verb, uri, request);
            }
        }


        //--- Delegates ---

        /// <summary>
        /// Delegate for registering a callback on Uri/Child Uri interception via <see cref="MockPlug.Register(XUri,Test.MockPlug.MockInvokeDelegate)"/>.
        /// </summary>
        /// <param name="plug">Invoking plug instance.</param>
        /// <param name="verb">Request verb.</param>
        /// <param name="uri">Request uri.</param>
        /// <param name="request">Request message.</param>
        public delegate Task<DreamMessage> MockInvokeDelegate(Plug plug, string verb, XUri uri, DreamMessage request);

        //--- Class Fields ---
        private static readonly Dictionary<string, List<MockPlug>> _mocks = new Dictionary<string, List<MockPlug>>();
        private static readonly Logger.ILog _log = Logger.CreateLog();
        private static int _setupcounter = 0;

        //--- Class Properties ---

        /// <summary>
        /// The default base Uri that will return a <see cref="DreamMessage.Ok(MindTouch.Xml.XDoc)"/> for any request. Should be used as no-op endpoint.
        /// </summary>
        public static readonly XUri DefaultUri = new XUri(MockEndpoint.DEFAULT);

        //--- Class Methods ---

        /// <summary>
        /// Register a callback to intercept any calls to a uri and its child paths.
        /// </summary>
        /// <param name="uri">Base Uri to intercept.</param>
        /// <param name="mock">Interception callback.</param>
        public static void Register(XUri uri, MockInvokeDelegate mock) {
            Register(uri, mock, int.MaxValue);
        }

        /// <summary>
        /// Register a callback to intercept any calls to a uri and its child paths.
        /// </summary>
        /// <param name="uri">Base Uri to intercept.</param>
        /// <param name="mock">Interception callback.</param>
        /// <param name="endpointScore">The score to return to <see cref="IPlugEndpoint.GetScoreWithNormalizedUri"/> for this uri.</param>
        public static void Register(XUri uri, MockInvokeDelegate mock, int endpointScore) {
            MockEndpoint.Instance.Register(new MockInvokee(uri, mock, endpointScore));
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(string baseUri) {
            return Setup(new XUri(baseUri));
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <param name="name">Debug name for setup</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(string baseUri, string name) {
            return Setup(new XUri(baseUri), name);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(XUri baseUri) {
            _setupcounter++;
            return Setup(baseUri, "Setup#" + _setupcounter, int.MaxValue);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// Note: endPointScore is only set on the first set for a specific baseUri. Subsequent values are ignored.
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <param name="endPointScore">The score to return to <see cref="IPlugEndpoint.GetScoreWithNormalizedUri"/> for this uri.</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(XUri baseUri, int endPointScore) {
            _setupcounter++;
            return Setup(baseUri, "Setup#" + _setupcounter, endPointScore);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <param name="name">Debug name for setup</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(XUri baseUri, string name) {
            return Setup(baseUri, name, int.MaxValue);
        }

        /// <summary>
        /// Setup a new <see cref="MockPlug"/> interceptor candidate for a uri and its child paths.
        /// </summary>
        /// <remarks>
        /// Note: endPointScore is only set on the first set for a specific baseUri. Subsequent values are ignored.
        /// </remarks>
        /// <param name="baseUri">Base Uri to intercept.</param>
        /// <param name="name">Debug name for setup</param>
        /// <param name="endPointScore">The score to return to <see cref="IPlugEndpoint.GetScoreWithNormalizedUri"/> for this uri.</param>
        /// <returns>A new interceptor instance that may intercept the uri, depending on its additional matching parameters.</returns>
        public static IMockPlug Setup(XUri baseUri, string name, int endPointScore) {
            List<MockPlug> mocks;
            var key = baseUri.SchemeHostPortPath;
            lock(_mocks) {
                if(!_mocks.TryGetValue(key, out mocks)) {
                    mocks = new List<MockPlug>();
                    MockInvokeDelegate callback = (plug, verb, uri, request) => {
                        _log.DebugFormat("checking setups for match on {0}:{1}", verb, uri);
                        MockPlug bestMatch = null;
                        var matchScore = 0;
                        foreach(var match in mocks) {
                            var score = match.GetMatchScore(verb, uri, request);
                            if(score > matchScore) {
                                bestMatch = match;
                                matchScore = score;
                            }
                        }
                        if(bestMatch == null) {
                            _log.Debug("no match");
                            return DreamMessage.Ok().AsCompletedTask();
                        } else {
                            _log.DebugFormat("[{0}] matched", bestMatch.Name);
                            return bestMatch.Invoke(verb, uri, request).AsCompletedTask();
                        }
                    };
                    MockEndpoint.Instance.Register(new MockInvokee(baseUri, callback, endPointScore));
                    MockEndpoint.Instance.AllDeregistered += Instance_AllDeregistered;
                    _mocks.Add(key, mocks);
                }
            }
            var mock = new MockPlug(baseUri, name);
            mocks.Add(mock);
            return mock;
        }

        static void Instance_AllDeregistered(object sender, EventArgs e) {
            lock(_mocks) {
                _mocks.Clear();
            }
        }

        /// <summary>
        /// Verify all <see cref="MockPlug"/> instances created with <see cref="Setup(XUri)"/> since the last <see cref="DeregisterAll"/> call.
        /// </summary>
        /// <remarks>
        /// Uses a 10 second timeout.
        /// </remarks>
        public static void VerifyAll() {
            VerifyAll(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Verify all <see cref="MockPlug"/> instances created with <see cref="Setup(XUri)"/> since the last <see cref="DeregisterAll"/> call.
        /// </summary>
        /// <param name="timeout">Time to wait for all expectations to be met.</param>
        public static void VerifyAll(TimeSpan timeout) {
            var verifiable = (from mocks in _mocks.Values
                              from mock in mocks
                              where mock.IsVerifiable
                              select mock);
            foreach(var mock in verifiable) {
                var stopwatch = Stopwatch.StartNew();
                ((IMockPlug)mock).Verify(timeout);
                stopwatch.Stop();
                timeout = timeout.Subtract(stopwatch.Elapsed);
                if(timeout.TotalMilliseconds < 0) {
                    timeout = TimeSpan.Zero;
                }
            }
        }

        /// <summary>
        /// Deregister all interceptors for a specific base uri
        /// </summary>
        /// <remarks>
        /// This will not deregister an interceptor that was registered specifically for a uri that is a child path of the provided uri.
        /// </remarks>
        /// <param name="uri">Base Uri to intercept.</param>
        public static void Deregister(XUri uri) {
            MockEndpoint.Instance.Deregister(uri);
        }

        /// <summary>
        /// Deregister all interceptors.
        /// </summary>
        public static void DeregisterAll() {
            MockEndpoint.Instance.DeregisterAll();
            _setupcounter = 0;
        }

        //--- Fields ---

        /// <summary>
        /// Name for the Mock Plug for debug logging purposes.
        /// </summary>
        public readonly string Name;

        private readonly AutoResetEvent _called = new AutoResetEvent(false);
        private readonly List<Tuple<string, Predicate<string>>> _queryMatchers = new List<Tuple<string, Predicate<string>>>();
        private readonly List<Tuple<string, Predicate<string>>> _headerMatchers = new List<Tuple<string, Predicate<string>>>();
        private readonly DreamHeaders _headers = new DreamHeaders();
        private readonly DreamHeaders _responseHeaders = new DreamHeaders();
        private XUri _uri;
        private string _verb = "*";
        private string _request;
        private Func<DreamMessage, bool> _requestCallback;
        private DreamMessage _response;
        private Func<MockPlugInvocation, DreamMessage> _responseCallback;
        private int _times;
        private Times _verifiable;
        private bool _matchTrailingSlashes;

        //--- Constructors ---
        private MockPlug(XUri uri, string name) {
            _uri = uri;
            Name = name;
        }

        //--- Properties ---
        /// <summary>
        /// Used by <see cref="VerifyAll()"/> to determine whether instance should be included in verification.
        /// </summary>
        public bool IsVerifiable { get { return _verifiable != null; } }

        //--- Methods ---
        private int GetMatchScore(string verb, XUri uri, DreamMessage request) {
            var score = 0;
            if(verb.EqualsInvariantIgnoreCase(_verb)) {
                score = 1;
            } else if(_verb != "*") {
                return 0;
            }
            var path = _matchTrailingSlashes ? _uri.Path : _uri.WithoutTrailingSlash().Path;
            var incomingPath = _matchTrailingSlashes ? uri.Path : uri.WithoutTrailingSlash().Path;
            if(!incomingPath.EqualsInvariantIgnoreCase(path)) {
                return 0;
            }
            score++;
            if(_uri.Params != null) {
                foreach(var param in _uri.Params) {
                    var v = uri.GetParam(param.Key);
                    if(v == null || !v.EndsWithInvariantIgnoreCase(param.Value)) {
                        return 0;
                    }
                    score++;
                }
            }
            foreach(var matcher in _queryMatchers) {
                var v = uri.GetParam(matcher.Item1);
                if(v == null || !matcher.Item2(v)) {
                    return 0;
                }
                score++;
            }
            foreach(var matcher in _headerMatchers) {
                var v = request.Headers[matcher.Item1];
                if(string.IsNullOrEmpty(v) || !matcher.Item2(v)) {
                    return 0;
                }
                score++;
            }
            foreach(var header in _headers) {
                var v = request.Headers[header.Key];
                if(string.IsNullOrEmpty(v) || !v.EqualsInvariant(header.Value)) {
                    return 0;
                }
                score++;
            }
            if(_requestCallback != null) {
                if(!_requestCallback(request)) {
                    return 0;
                }
            } else if(_request != null && (_request != request.ToText())) {
                return 0;
            }
            score++;
            return score;
        }

        private DreamMessage Invoke(string verb, XUri uri, DreamMessage request) {
            _times++;
            if(_responseCallback != null) {
                var response = _responseCallback(new MockPlugInvocation(verb, uri, request, _responseHeaders));
                _response = response;
            }
            if(_response == null) {
                _response = DreamMessage.Ok();
            }
            _response.Headers.AddRange(_responseHeaders);
            _called.Set();
            _log.DebugFormat("invoked {0}:{1}", verb, uri);
            return _response;
        }

        #region Implementation of IMockPlug
        IMockPlug IMockPlug.Verb(string verb) {
            _verb = verb;
            return this;
        }

        IMockPlug IMockPlug.At(string[] path) {
            _uri = _uri.At(path);
            return this;
        }

        IMockPlug IMockPlug.With(string key, string value) {
            _uri = _uri.With(key, value);
            return this;
        }

        IMockPlug IMockPlug.With(string key, Predicate<string> valueCallback) {
            _queryMatchers.Add(new Tuple<string, Predicate<string>>(key, valueCallback));
            return this;
        }

        IMockPlug IMockPlug.WithTrailingSlash() {
            _uri = _uri.WithTrailingSlash();
            _matchTrailingSlashes = true;
            return this;
        }

        IMockPlug IMockPlug.WithoutTrailingSlash() {
            _uri = _uri.WithoutTrailingSlash();
            _matchTrailingSlashes = true;
            return this;
        }

        IMockPlug IMockPlug.WithBody(string request) {
            _request = request;
            _requestCallback = null;
            return this;
        }

        IMockPlug IMockPlug.WithMessage(Func<DreamMessage, bool> requestCallback) {
            _requestCallback = requestCallback;
            _request = null;
            return this;
        }

        IMockPlug IMockPlug.WithHeader(string key, string value) {
            _headers[key] = value;
            return this;
        }

        IMockPlug IMockPlug.WithHeader(string key, Predicate<string> valueCallback) {
            _headerMatchers.Add(new Tuple<string, Predicate<string>>(key, valueCallback));
            return this;
        }

        IMockPlug IMockPlug.Returns(DreamMessage response) {
            _response = response;
            return this;
        }

        IMockPlug IMockPlug.Returns(Func<MockPlugInvocation, DreamMessage> response) {
            _responseCallback = response;
            return this;
        }

        IMockPlug IMockPlug.Returns(string response) {
            var status = _response == null ? DreamStatus.Ok : _response.Status;
            var headers = _response == null ? null : _response.Headers;
            _response = new DreamMessage(status, headers, MimeType.TEXT, response);
            return this;
        }

        IMockPlug IMockPlug.WithResponseHeader(string key, string value) {
            _responseHeaders[key] = value;
            return this;
        }

        void IMockPlug.Verify() {
            if(_verifiable == null) {
                return;
            }
            ((IMockPlug)this).Verify(TimeSpan.FromSeconds(5), _verifiable);
        }

        void IMockPlug.Verify(Times times) {
            ((IMockPlug)this).Verify(TimeSpan.FromSeconds(5), times);
        }

        IMockPlug IMockPlug.ExpectAtLeastOneCall() {
            _verifiable = Times.AtLeastOnce();
            return this;
        }

        IMockPlug IMockPlug.ExpectCalls(Times called) {
            _verifiable = called;
            return this;
        }

        void IMockPlug.Verify(TimeSpan timeout) {
            if(_verifiable == null) {
                return;
            }
            ((IMockPlug)this).Verify(timeout, _verifiable);
        }

        void IMockPlug.Verify(TimeSpan timeout, Times times) {
            while(true) {
                var verified = times.Verify(_times, timeout);
                if(verified == Times.Result.Ok) {
                    _log.DebugFormat("satisfied {0}", _uri);
                    return;
                }
                if(verified == Times.Result.TooMany) {
                    break;
                }

                // check if we have any time left to wait
                if(timeout.TotalMilliseconds < 0) {
                    break;
                }
                _log.DebugFormat("waiting on {0}:{1} with {2:0.00}ms left in timeout", _verb, _uri, timeout.TotalMilliseconds);
                var stopwatch = Stopwatch.StartNew();
                if(!_called.WaitOne(timeout)) {
                    break;
                }
                timeout = timeout.Subtract(stopwatch.Elapsed);
            }
            throw new MockPlugException(string.Format("[{0}] {1}:{2} was called {3} times before timeout.", Name, _verb, _uri, _times));
        }
        #endregion
    }
}