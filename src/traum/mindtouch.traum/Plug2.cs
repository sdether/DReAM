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
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace MindTouch.Traum {
    /// <summary>
    /// Provides a contract for intercepting and modifying <see cref="Plug2"/> requests and responses in the invocation pipeline.
    /// </summary>
    /// <param name="verb">Verb of the intercepted invocation.</param>
    /// <param name="uri">Uri of the intercepted invocation.</param>
    /// <param name="normalizedUri">Normalized version of the uri of the intercepted invocation.</param>
    /// <param name="message">Message of the intercepted invocation.</param>
    /// <returns>The message to return as the result of the interception.</returns>
    public delegate DreamMessage2 PlugHandler2(string verb, XUri uri, XUri normalizedUri, DreamMessage2 message);

    /// <summary>
    /// Provides a fluent, immutable interface for building request/response invocation  against a resource. Mostly used as an interface
    /// for making Http requests, but can be extended for any resource that can provide request/response semantics.
    /// </summary>
    public class Plug2 {

        //--- Constants ---

        /// <summary>
        /// Default number of redirects plug uses when no value is specified.
        /// </summary>
        public const ushort DEFAULT_MAX_AUTO_REDIRECTS = 50;

        /// <summary>
        /// Base score normal priorty <see cref="IPlugEndpoint2"/> implementations should use to signal a successful match.
        /// </summary>
        public const int BASE_ENDPOINT_SCORE = int.MaxValue / 2;

        /// <summary>
        /// Default timeout of 60 seconds for <see cref="Plug2"/> invocations.
        /// </summary>
        public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(60);

        //--- Class Fields ---

        /// <summary>
        /// Default, shared cookie jar for all plugs.
        /// </summary>
        public static DreamCookieJar GlobalCookies = new DreamCookieJar();

        private static log4net.ILog _log = LogUtils.CreateLog();
        private static readonly List<IPlugEndpoint2> _endpoints = new List<IPlugEndpoint2>();

        //--- Class Constructors ---
        static Plug2() {

            // let's find all IPlugEndpoint derived, concrete classes
            foreach(Type type in typeof(Plug2).Assembly.GetTypes()) {
                if(typeof(IPlugEndpoint2).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition) {
                    ConstructorInfo ctor = type.GetConstructor(System.Type.EmptyTypes);
                    if(ctor != null) {
                        AddEndpoint((IPlugEndpoint2)ctor.Invoke(null));
                    }
                }
            }
        }

        //--- Class Operators ---

        /// <summary>
        /// Implicit conversion operator for casting a <see cref="Plug2"/> to a <see cref="XUri"/>.
        /// </summary>
        /// <param name="plug">Plug instance to convert.</param>
        /// <returns>New uri instance.</returns>
        public static implicit operator XUri(Plug2 plug) {
            return (plug != null) ? plug.Uri : null;
        }

        //--- Class Methods ---

        /// <summary>
        /// Create a new <see cref="Plug2"/> instance from a uri string.
        /// </summary>
        /// <param name="uri">Uri string.</param>
        /// <returns>New plug instance.</returns>
        public static Plug2 New(string uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug2"/> instance from a uri string.
        /// </summary>
        /// <param name="uri">Uri string.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug2 New(string uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug2(new XUri(uri), timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Create a new <see cref="Plug2"/> instance from a <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <returns>New plug instance.</returns>
        public static Plug2 New(Uri uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug2"/> instance from a <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug2 New(Uri uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug2(new XUri(uri), timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Create a new <see cref="Plug2"/> instance from a <see cref="XUri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <returns>New plug instance.</returns>
        public static Plug2 New(XUri uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug2"/> instance from a <see cref="XUri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug2 New(XUri uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug2(uri, timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Manually add a plug endpoint for handling invocations.
        /// </summary>
        /// <param name="endpoint">Factory instance to add.</param>
        public static void AddEndpoint(IPlugEndpoint2 endpoint) {
            lock(_endpoints) {
                _endpoints.Add(endpoint);
            }
        }

        /// <summary>
        /// Manually remove a plug endpoint from the handler pool.
        /// </summary>
        /// <param name="endpoint">Factory instance to remove.</param>
        public static void RemoveEndpoint(IPlugEndpoint2 endpoint) {
            lock(_endpoints) {
                _endpoints.Remove(endpoint);
            }
        }

        /// <summary>
        /// Blocks on a Plug synchronization handle to wait for it ti complete and confirm that it's a non-error response.
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="task">Plug synchronization handle.</param>
        /// <returns>Successful reponse message.</returns>
        public static DreamMessage2 WaitAndConfirm(Task<DreamMessage2> task) {

            // NOTE (steveb): we don't need to set a time-out since 'Memorize()' already guarantees eventual termination

            task.Result.Memorize(TimeSpan.MaxValue).Wait();
            DreamMessage2 message = task.Result;
            if(!message.IsSuccessful) {
                throw new DreamResponseException(message);
            }
            return message;
        }

        private static DreamMessage2 PreProcess(string verb, XUri uri, XUri normalizedUri, DreamHeaders headers, DreamCookieJar cookies, DreamMessage2 message) {
            if(cookies != null) {
                lock(cookies) {
                    message.Cookies.AddRange(cookies.Fetch(uri));
                }
            }

            // transfer plug headers
            message.Headers.AddRange(headers);
            return message;
        }

        private static DreamMessage2 PostProcess(string verb, XUri uri, XUri normalizedUri, DreamHeaders headers, DreamCookieJar cookies, DreamMessage2 message) {

            // check if we received cookies
            if(message.HasCookies) {

                // add matching cookies to service or to global cookie jar
                if(cookies != null) {
                    lock(cookies) {
                        if(!uri.Scheme.EqualsInvariant("local") && normalizedUri.Scheme.EqualsInvariant("local")) {

                            // need to translate cookies as they leave the dreamcontext
                            cookies.Update(DreamCookie.ConvertToPublic(message.Cookies), uri);
                        } else {
                            cookies.Update(message.Cookies, uri);
                        }
                    }
                }
            }
            return message;
        }

        private static int FindPlugEndpoint(XUri uri, out IPlugEndpoint2 match, out XUri normalizedUri) {
            match = null;
            normalizedUri = null;

            // determine which plug factory has the best match
            int maxScore = 0;
            lock(_endpoints) {

                // loop over all plug factories to determine best transport mechanism
                foreach(var factory in _endpoints) {
                    XUri newNormalizedUri;
                    int score = factory.GetScoreWithNormalizedUri(uri, out newNormalizedUri);
                    if(score > maxScore) {
                        maxScore = score;
                        normalizedUri = newNormalizedUri;
                        match = factory;
                    }
                }
            }
            return maxScore;
        }

        //--- Fields ---

        /// <summary>
        /// Uri of the instance.
        /// </summary>
        public readonly XUri Uri;

        /// <summary>
        /// Timeout for invocation.
        /// </summary>
        public readonly TimeSpan Timeout;

        /// <summary>
        /// If not null, the creditials to use for the invocation.
        /// </summary>
        public readonly ICredentials Credentials;

        // BUGBUGBUG (steveb): _headers needs to be read-only
        private readonly DreamHeaders _headers;

        // BUGBUGBUG (steveb): _preHandlers, _postHandlers need to be read-only
        private readonly List<PlugHandler2> _preHandlers;
        private readonly List<PlugHandler2> _postHandlers;

        private readonly DreamCookieJar _cookieJarOverride = null;
        private readonly ushort _maxAutoRedirects = 0;

        //--- Constructors ---

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="uri">Uri to the resource to make the request against.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <param name="headers">Header collection for request.</param>
        /// <param name="preHandlers">Optional pre-invocation handlers.</param>
        /// <param name="postHandlers">Optional post-invocation handlers.</param>
        /// <param name="credentials">Optional request credentials.</param>
        /// <param name="cookieJarOverride">Optional cookie jar to override global jar shared by <see cref="Plug2"/> instances.</param>
        /// <param name="maxAutoRedirects">Maximum number of redirects to follow, 0 if non redirects should be followed.</param>
        public Plug2(XUri uri, TimeSpan timeout, DreamHeaders headers, List<PlugHandler2> preHandlers, List<PlugHandler2> postHandlers, ICredentials credentials, DreamCookieJar cookieJarOverride, ushort maxAutoRedirects) {
            if(uri == null) {
                throw new ArgumentNullException("uri");
            }
            this.Uri = uri;
            this.Timeout = timeout;
            this.Credentials = credentials;
            _headers = headers;
            _preHandlers = preHandlers;
            _postHandlers = postHandlers;
            _cookieJarOverride = cookieJarOverride;
            _maxAutoRedirects = maxAutoRedirects;
        }

        //--- Properties ---

        /// <summary>
        /// Request header collection.
        /// </summary>
        public DreamHeaders Headers { get { return _headers; } }

        /// <summary>
        /// Pre-invocation handlers.
        /// </summary>
        public PlugHandler2[] PreHandlers { get { return (_preHandlers != null) ? _preHandlers.ToArray() : null; } }

        /// <summary>
        /// Post-invocation handlers.
        /// </summary>
        public PlugHandler2[] PostHandlers { get { return (_postHandlers != null) ? _postHandlers.ToArray() : null; } }

        /// <summary>
        /// Cookie jar for the request.
        /// </summary>
        public DreamCookieJar CookieJar {
            get {
                // Note (arnec): In order for the override to not block the environment, we always run this logic to get at the
                // plug's cookie jar rather than assigning the resulting value to _cookieJarOverride
                return _cookieJarOverride ?? GlobalCookies;
            }
        }

        /// <summary>
        /// True if this plug will automatically follow redirects (301,302 &amp; 307).
        /// </summary>
        public bool AutoRedirect { get { return _maxAutoRedirects > 0; } }

        /// <summary>
        /// Maximum number of redirect to follow before giving up.
        /// </summary>
        public ushort MaxAutoRedirects { get { return _maxAutoRedirects; } }

        //--- Methods ---

        /// <summary>
        /// Create a copy of the instance with new path segments appended to its Uri.
        /// </summary>
        /// <param name="segments">Segements to add.</param>
        /// <returns>New instance.</returns>
        public Plug2 At(params string[] segments) {
            if(segments.Length == 0) {
                return this;
            }
            return new Plug2(Uri.At(segments), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a path/query/fragement appended to its Uri.
        /// </summary>
        /// <param name="path">Path/Query/fragment string.</param>
        /// <returns>New instance.</returns>
        public Plug2 AtPath(string path) {
            return new Plug2(Uri.AtPath(path), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, string value) {
            return new Plug2(Uri.With(key, value), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, bool value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, int value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, long value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, decimal value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, double value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, DateTime value) {
            return new Plug2(Uri.With(key, value.ToUniversalTime().ToString("R")), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with additional query parameters.
        /// </summary>
        /// <param name="args">Array of query key/value pairs.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithParams(KeyValuePair<string, string>[] args) {
            return new Plug2(Uri.WithParams(args), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with the provided querystring added.
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithQuery(string query) {
            return new Plug2(Uri.WithQuery(query), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with parameters from another uri added.
        /// </summary>
        /// <param name="uri">Uri to extract parameters from.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithParamsFrom(XUri uri) {
            return new Plug2(Uri.WithParamsFrom(uri), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with the given credentials
        /// </summary>
        /// <remarks>
        /// Using the user/password signature will always try to send a basic auth header. If negotiation of auth method is desired
        /// (i.e. digest auth may be an option), use <see cref="WithCredentials(System.Net.ICredentials)"/> instead.
        /// </remarks>
        /// <param name="user">User.</param>
        /// <param name="password">Password.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithCredentials(string user, string password) {
            return new Plug2(Uri.WithCredentials(user, password), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with the given credentials
        /// </summary>
        /// <param name="credentials">Credential instance.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithCredentials(ICredentials credentials) {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with credentials removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutCredentials() {
            return new Plug2(Uri.WithoutCredentials(), Timeout, _headers, _preHandlers, _postHandlers, null, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with an override cookie jar.
        /// </summary>
        /// <param name="cookieJar">Cookie jar to use.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithCookieJar(DreamCookieJar cookieJar) {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, cookieJar, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with any override cookie jar removed.
        /// </summary>
        /// <remarks>Will fall back on <see cref="DreamContext"/> or global jar.</remarks>
        /// <returns>New instance.</returns>
        public Plug2 WithoutCookieJar() {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, null, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn on auto redirect behavior with the <see cref="DEFAULT_MAX_AUTO_REDIRECTS"/> number of redirects to follow.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithAutoRedirects() {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, DEFAULT_MAX_AUTO_REDIRECTS);
        }

        /// <summary>
        /// Turn on auto redirect behavior with the specified number of redirects.
        /// </summary>
        /// <param name="maxRedirects">Maximum number of redirects to follow before giving up.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithAutoRedirects(ushort maxRedirects) {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, maxRedirects);
        }

        /// <summary>
        /// Turn off auto-redirect behavior.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutAutoRedirects() {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, 0);
        }
        /// <summary>
        /// Create a copy of the instance with a header added.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithHeader(string name, string value) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            var newHeaders = new DreamHeaders(_headers) { { name, value } };
            return new Plug2(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a header collection added.
        /// </summary>
        /// <param name="headers">Header collection</param>
        /// <returns>New instance.</returns>
        public Plug2 WithHeaders(DreamHeaders headers) {
            if(headers != null) {
                var newHeaders = new DreamHeaders(_headers);
                newHeaders.AddRange(headers);
                return new Plug2(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
            }
            return this;
        }

        /// <summary>
        /// Create a copy of the instance with a header removed.
        /// </summary>
        /// <param name="name">Name of the header to remove.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithoutHeader(string name) {
            DreamHeaders newHeaders = null;
            if(_headers != null) {
                newHeaders = new DreamHeaders(_headers);
                newHeaders.Remove(name);
                if(newHeaders.Count == 0) {
                    newHeaders = null;
                }
            }
            return new Plug2(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with all headers removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutHeaders() {
            return new Plug2(Uri, Timeout, null, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a pre-invocation handler added.
        /// </summary>
        /// <param name="preHandlers">Pre-invocation handler.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithPreHandler(params PlugHandler2[] preHandlers) {
            List<PlugHandler2> list = (_preHandlers != null) ? new List<PlugHandler2>(_preHandlers) : new List<PlugHandler2>();
            list.AddRange(preHandlers);
            return new Plug2(Uri, Timeout, _headers, list, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a post-invocation handler added.
        /// </summary>
        /// <param name="postHandlers">Post-invocation handler.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithPostHandler(params PlugHandler2[] postHandlers) {
            var list = new List<PlugHandler2>(postHandlers);
            if(_postHandlers != null) {
                list.AddRange(_postHandlers);
            }
            return new Plug2(Uri, Timeout, _headers, _preHandlers, list, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with all handlers removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutHandlers() {
            return new Plug2(Uri, Timeout, _headers, null, null, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a new timeout.
        /// </summary>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithTimeout(TimeSpan timeout) {
            return new Plug2(Uri, timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a trailing slash.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithTrailingSlash() {
            return new Plug2(Uri.WithTrailingSlash(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance without a trailing slash.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutTrailingSlash() {
            return new Plug2(Uri.WithoutTrailingSlash(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn on double-encoding of segments when the Plug's <see cref="Uri"/> is converted to a <see cref="System.Uri"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithSegmentDoubleEncoding() {
            return new Plug2(Uri.WithSegmentDoubleEncoding(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn off double-encoding of segments when the Plug's <see cref="Uri"/> is converted to a <see cref="System.Uri"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutSegmentDoubleEncoding() {
            return new Plug2(Uri.WithoutSegmentDoubleEncoding(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Provide a string representation of the Uri of the instance.
        /// </summary>
        /// <returns>Uri string.</returns>
        public override string ToString() {
            return Uri.ToString();
        }

        #region --- Iterative Methods ---

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb and an empty message.
        /// </summary>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Post() {
            return Invoke(Verb.POST, DreamMessage2.Ok(), DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb and an empty message.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Post(TimeSpan timeout) {
            return Invoke(Verb.POST, DreamMessage2.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> PostAsync(DreamMessage2 message) {
            return Invoke(Verb.POST, message, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Post(DreamMessage2 message, TimeSpan timeout) {
            return Invoke(Verb.POST, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb with <see cref="Verb.GET"/> query arguments converted as form post body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> PostAsForm(TimeSpan timeout) {
            DreamMessage2 message = DreamMessage2.Ok(Uri.Params);
            XUri uri = Uri.WithoutParams();
            return new Plug2(uri, Timeout, Headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects).Invoke(Verb.POST, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.PUT"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Put(DreamMessage2 message, TimeSpan timeout) {
            return Invoke(Verb.PUT, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb and no message body.
        /// </summary>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> GetAsync() {
            return Invoke(Verb.GET, DreamMessage2.Ok(), DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb and no message body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Get(TimeSpan timeout) {
            return Invoke(Verb.GET, DreamMessage2.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Get(DreamMessage2 message, TimeSpan timeout) {
            return Invoke(Verb.GET, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.HEAD"/> verb and no message body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Head(TimeSpan timeout) {
            return Invoke(Verb.HEAD, DreamMessage2.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.OPTIONS"/> verb and no message body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Options(TimeSpan timeout) {
            return Invoke(Verb.OPTIONS, DreamMessage2.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.DELETE"/> verb and no message body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Delete(TimeSpan timeout) {
            return Invoke(Verb.DELETE, DreamMessage2.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.DELETE"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Delete(DreamMessage2 message, TimeSpan timeout) {
            return Invoke(Verb.DELETE, message, timeout);
        }

        /// <summary>
        /// Invoke the plug.
        /// </summary>
        /// <param name="verb">Request verb.</param>
        /// <param name="request">Request message.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Invoke(string verb, DreamMessage2 request, TimeSpan timeout) {
            var hasTimeout = timeout != TimeSpan.MaxValue;
            var requestTimer = Stopwatch.StartNew();
            var completion = new TaskCompletionSource<DreamMessage2>();
            InvokeEx(verb, request, timeout)
                .ContinueWith(t1 => {
                    requestTimer.Stop();
                    if(hasTimeout) {
                        timeout = timeout - requestTimer.Elapsed;
                    }
                    if(t1.IsFaulted) {
                        completion.SetException(t1.Exception);
                    } else {
                        t1.Result.Memorize(timeout)
                            .ContinueWith(t2 => {
                                if(t2.IsFaulted) {
                                    completion.SetException(t2.Exception);
                                } else {
                                    completion.SetResult(t2.Result);
                                }
                            });
                    }
                });
            return completion.Task;
        }

        /// <summary>
        /// Invoke the plug, but leave the stream unread so that the returned <see cref="DreamMessage2"/> can be streamed.
        /// </summary>
        /// <param name="verb">Request verb.</param>
        /// <param name="request">Request message.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> InvokeEx(string verb, DreamMessage2 request, TimeSpan timeout) {
            if(verb == null) {
                throw new ArgumentNullException("verb");
            }
            if(request == null) {
                throw new ArgumentNullException("request");
            }
            if(request.Status != DreamStatus.Ok) {
                throw new ArgumentException("request status must be 200 (Ok)");
            }

            // determine which factory has the best match
            IPlugEndpoint2 match;
            XUri normalizedUri;
            FindPlugEndpoint(Uri, out match, out normalizedUri);

            // check if we found a match
            if(match == null) {
                request.Close();
                return new DreamMessage2(DreamStatus.NoEndpointFound).AsCompletedTask();
            }

            // add matching cookies from service or from global cookie jar
            DreamCookieJar cookies = CookieJar;

            // prepare request
            try {
                request = PreProcess(verb, Uri, normalizedUri, _headers, cookies, request);

                // check if custom pre-processing handlers are registered
                if(_preHandlers != null) {
                    foreach(PlugHandler2 handler in _preHandlers) {
                        request = handler(verb, Uri, normalizedUri, request) ?? new DreamMessage2(DreamStatus.RequestIsNull);
                        if(request.Status != DreamStatus.Ok) {
                            return request.AsCompletedTask();
                        }
                    }
                }
            } catch(Exception e) {
                request.Close();
                return DreamMessage2.RequestFailed(e).AsCompletedTask();
            }

            // Note (arnec): Plug never throws, so we usurp the passed result if it has a timeout
            // setting the result timeout on inner result manually
            //var outerTimeout = result.Timeout;
            //if(outerTimeout != TimeSpan.MaxValue) {
            //    result.Timeout = TimeSpan.MaxValue;
            //}

            // if the governing result has a shorter timeout than the plug, it superceeds the plug timeout
            //var timeout = outerTimeout < Timeout ? outerTimeout : Timeout;
            var completion = new TaskCompletionSource<DreamMessage2>();
            match.Invoke(this, verb, normalizedUri, request, timeout).ContinueWith(invokeTask => {
                if(invokeTask.IsFaulted) {
                    // an exception occurred somewhere during processing (not expected, but it could happen)
                    request.Close();

                    var status = DreamStatus.RequestFailed;
                    var e = invokeTask.UnwrapFault();
                    if(e is TimeoutException) {
                        status = DreamStatus.RequestConnectionTimeout;
                    }
                    completion.SetResult(new DreamMessage2(status, e));
                    return;
                }
                var response = invokeTask.Result;
                try {
                    var message = PostProcess(verb, Uri, normalizedUri, _headers, cookies, response);

                    // check if custom post-processing handlers are registered
                    if((message.Status == DreamStatus.MovedPermanently ||
                        message.Status == DreamStatus.Found ||
                        message.Status == DreamStatus.TemporaryRedirect) &&
                       AutoRedirect &&
                       request.IsCloneable
                    ) {
                        var redirectPlug = new Plug2(message.Headers.Location, Timeout, Headers, null, null, null, CookieJar, (ushort)(MaxAutoRedirects - 1));
                        var redirectMessage = request.Clone();
                        request.Close();
                        // await
                        redirectPlug.InvokeEx(verb, redirectMessage, Timeout).ContinueWith(redirectTask => completion.SetResult(redirectTask.Result));
                    } else {
                        request.Close();
                        if(_postHandlers != null) {
                            foreach(PlugHandler2 handler in _postHandlers) {
                                response = handler(verb, Uri, normalizedUri, response) ?? new DreamMessage2(DreamStatus.ResponseIsNull);
                            }
                        }
                    }
                } catch(Exception e) {
                    request.Close();
                    completion.SetResult(new DreamMessage2(DreamStatus.ResponseFailed, e));
                }
            });


            return completion.Task;
        }
        #endregion
    }
}
