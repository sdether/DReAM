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

namespace MindTouch.Traum.Webclient {
    /// <summary>
    /// Provides a contract for intercepting and modifying <see cref="Plug"/> requests and responses in the invocation pipeline.
    /// </summary>
    /// <param name="verb">Verb of the intercepted invocation.</param>
    /// <param name="uri">Uri of the intercepted invocation.</param>
    /// <param name="normalizedUri">Normalized version of the uri of the intercepted invocation.</param>
    /// <param name="message">Message of the intercepted invocation.</param>
    /// <returns>The message to return as the result of the interception.</returns>
    public delegate DreamMessage PlugHandler2(string verb, XUri uri, XUri normalizedUri, DreamMessage message);

    /// <summary>
    /// Provides a fluent, immutable interface for building request/response invocation  against a resource. Mostly used as an interface
    /// for making Http requests, but can be extended for any resource that can provide request/response semantics.
    /// </summary>
    public class Plug {

        //--- Constants ---

        /// <summary>
        /// Default number of redirects plug uses when no value is specified.
        /// </summary>
        public const ushort DEFAULT_MAX_AUTO_REDIRECTS = 50;

        /// <summary>
        /// Base score normal priorty <see cref="IPlugEndpoint"/> implementations should use to signal a successful match.
        /// </summary>
        public const int BASE_ENDPOINT_SCORE = int.MaxValue / 2;

        /// <summary>
        /// Default timeout of 60 seconds for <see cref="Plug"/> invocations.
        /// </summary>
        public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(60);

        //--- Class Fields ---

        /// <summary>
        /// Default, shared cookie jar for all plugs.
        /// </summary>
        public static DreamCookieJar GlobalCookies = new DreamCookieJar();

        private static Logger.ILog _log = Logger.CreateLog();
        private static readonly List<IPlugEndpoint> _endpoints = new List<IPlugEndpoint>();

        //--- Class Constructors ---
        static Plug() {

            // let's find all IPlugEndpoint derived, concrete classes
            foreach(Type type in typeof(Plug).Assembly.GetTypes()) {
                if(typeof(IPlugEndpoint).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition) {
                    ConstructorInfo ctor = type.GetConstructor(System.Type.EmptyTypes);
                    if(ctor != null) {
                        AddEndpoint((IPlugEndpoint)ctor.Invoke(null));
                    }
                }
            }
        }

        //--- Class Operators ---

        /// <summary>
        /// Implicit conversion operator for casting a <see cref="Plug"/> to a <see cref="XUri"/>.
        /// </summary>
        /// <param name="plug">Plug instance to convert.</param>
        /// <returns>New uri instance.</returns>
        public static implicit operator XUri(Plug plug) {
            return (plug != null) ? plug.Uri : null;
        }

        //--- Class Methods ---

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a uri string.
        /// </summary>
        /// <param name="uri">Uri string.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(string uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a uri string.
        /// </summary>
        /// <param name="uri">Uri string.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(string uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug(new XUri(uri), timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(Uri uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(Uri uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug(new XUri(uri), timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a <see cref="XUri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(XUri uri) {
            return New(uri, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Create a new <see cref="Plug"/> instance from a <see cref="XUri"/>.
        /// </summary>
        /// <param name="uri">Uri instance.</param>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New plug instance.</returns>
        public static Plug New(XUri uri, TimeSpan timeout) {
            if(uri != null) {
                return new Plug(uri, timeout, null, null, null, null, null, DEFAULT_MAX_AUTO_REDIRECTS);
            }
            return null;
        }

        /// <summary>
        /// Manually add a plug endpoint for handling invocations.
        /// </summary>
        /// <param name="endpoint">Factory instance to add.</param>
        public static void AddEndpoint(IPlugEndpoint endpoint) {
            lock(_endpoints) {
                _endpoints.Add(endpoint);
            }
        }

        /// <summary>
        /// Manually remove a plug endpoint from the handler pool.
        /// </summary>
        /// <param name="endpoint">Factory instance to remove.</param>
        public static void RemoveEndpoint(IPlugEndpoint endpoint) {
            lock(_endpoints) {
                _endpoints.Remove(endpoint);
            }
        }

        private static DreamMessage PreProcess(string verb, XUri uri, XUri normalizedUri, DreamHeaders headers, DreamCookieJar cookies, DreamMessage message) {
            if(cookies != null) {
                lock(cookies) {
                    message.Cookies.AddRange(cookies.Fetch(uri));
                }
            }

            // transfer plug headers
            message.Headers.AddRange(headers);
            return message;
        }

        private static DreamMessage PostProcess(string verb, XUri uri, XUri normalizedUri, DreamHeaders headers, DreamCookieJar cookies, DreamMessage message) {

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

        private static int FindPlugEndpoint(XUri uri, out IPlugEndpoint match, out XUri normalizedUri) {
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
        /// <param name="cookieJarOverride">Optional cookie jar to override global jar shared by <see cref="Plug"/> instances.</param>
        /// <param name="maxAutoRedirects">Maximum number of redirects to follow, 0 if non redirects should be followed.</param>
        public Plug(XUri uri, TimeSpan timeout, DreamHeaders headers, List<PlugHandler2> preHandlers, List<PlugHandler2> postHandlers, ICredentials credentials, DreamCookieJar cookieJarOverride, ushort maxAutoRedirects) {
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
        public Plug At(params string[] segments) {
            if(segments.Length == 0) {
                return this;
            }
            return new Plug(Uri.At(segments), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a path/query/fragement appended to its Uri.
        /// </summary>
        /// <param name="path">Path/Query/fragment string.</param>
        /// <returns>New instance.</returns>
        public Plug AtPath(string path) {
            return new Plug(Uri.AtPath(path), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, string value) {
            return new Plug(Uri.With(key, value), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, bool value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, int value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, long value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, decimal value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, double value) {
            return new Plug(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug With(string key, DateTime value) {
            return new Plug(Uri.With(key, value.ToUniversalTime().ToString("R")), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with additional query parameters.
        /// </summary>
        /// <param name="args">Array of query key/value pairs.</param>
        /// <returns>New instance.</returns>
        public Plug WithParams(KeyValuePair<string, string>[] args) {
            return new Plug(Uri.WithParams(args), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with the provided querystring added.
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <returns>New instance.</returns>
        public Plug WithQuery(string query) {
            return new Plug(Uri.WithQuery(query), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with parameters from another uri added.
        /// </summary>
        /// <param name="uri">Uri to extract parameters from.</param>
        /// <returns>New instance.</returns>
        public Plug WithParamsFrom(XUri uri) {
            return new Plug(Uri.WithParamsFrom(uri), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
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
        public Plug WithCredentials(string user, string password) {
            return new Plug(Uri.WithCredentials(user, password), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with the given credentials
        /// </summary>
        /// <param name="credentials">Credential instance.</param>
        /// <returns>New instance.</returns>
        public Plug WithCredentials(ICredentials credentials) {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with credentials removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutCredentials() {
            return new Plug(Uri.WithoutCredentials(), Timeout, _headers, _preHandlers, _postHandlers, null, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with an override cookie jar.
        /// </summary>
        /// <param name="cookieJar">Cookie jar to use.</param>
        /// <returns>New instance.</returns>
        public Plug WithCookieJar(DreamCookieJar cookieJar) {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, cookieJar, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with any override cookie jar removed.
        /// </summary>
        /// <remarks>Will fall back on <see cref="DreamContext"/> or global jar.</remarks>
        /// <returns>New instance.</returns>
        public Plug WithoutCookieJar() {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, null, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn on auto redirect behavior with the <see cref="DEFAULT_MAX_AUTO_REDIRECTS"/> number of redirects to follow.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithAutoRedirects() {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, DEFAULT_MAX_AUTO_REDIRECTS);
        }

        /// <summary>
        /// Turn on auto redirect behavior with the specified number of redirects.
        /// </summary>
        /// <param name="maxRedirects">Maximum number of redirects to follow before giving up.</param>
        /// <returns>New instance.</returns>
        public Plug WithAutoRedirects(ushort maxRedirects) {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, maxRedirects);
        }

        /// <summary>
        /// Turn off auto-redirect behavior.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutAutoRedirects() {
            return new Plug(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, 0);
        }
        /// <summary>
        /// Create a copy of the instance with a header added.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns>New instance.</returns>
        public Plug WithHeader(string name, string value) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            var newHeaders = new DreamHeaders(_headers) { { name, value } };
            return new Plug(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a header collection added.
        /// </summary>
        /// <param name="headers">Header collection</param>
        /// <returns>New instance.</returns>
        public Plug WithHeaders(DreamHeaders headers) {
            if(headers != null) {
                var newHeaders = new DreamHeaders(_headers);
                newHeaders.AddRange(headers);
                return new Plug(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
            }
            return this;
        }

        /// <summary>
        /// Create a copy of the instance with a header removed.
        /// </summary>
        /// <param name="name">Name of the header to remove.</param>
        /// <returns>New instance.</returns>
        public Plug WithoutHeader(string name) {
            DreamHeaders newHeaders = null;
            if(_headers != null) {
                newHeaders = new DreamHeaders(_headers);
                newHeaders.Remove(name);
                if(newHeaders.Count == 0) {
                    newHeaders = null;
                }
            }
            return new Plug(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with all headers removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutHeaders() {
            return new Plug(Uri, Timeout, null, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a pre-invocation handler added.
        /// </summary>
        /// <param name="preHandlers">Pre-invocation handler.</param>
        /// <returns>New instance.</returns>
        public Plug WithPreHandler(params PlugHandler2[] preHandlers) {
            List<PlugHandler2> list = (_preHandlers != null) ? new List<PlugHandler2>(_preHandlers) : new List<PlugHandler2>();
            list.AddRange(preHandlers);
            return new Plug(Uri, Timeout, _headers, list, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a post-invocation handler added.
        /// </summary>
        /// <param name="postHandlers">Post-invocation handler.</param>
        /// <returns>New instance.</returns>
        public Plug WithPostHandler(params PlugHandler2[] postHandlers) {
            var list = new List<PlugHandler2>(postHandlers);
            if(_postHandlers != null) {
                list.AddRange(_postHandlers);
            }
            return new Plug(Uri, Timeout, _headers, _preHandlers, list, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with all handlers removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutHandlers() {
            return new Plug(Uri, Timeout, _headers, null, null, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a new timeout.
        /// </summary>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New instance.</returns>
        public Plug WithTimeout(TimeSpan timeout) {
            return new Plug(Uri, timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance with a trailing slash.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithTrailingSlash() {
            return new Plug(Uri.WithTrailingSlash(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Create a copy of the instance without a trailing slash.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutTrailingSlash() {
            return new Plug(Uri.WithoutTrailingSlash(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn on double-encoding of segments when the Plug's <see cref="Uri"/> is converted to a <see cref="System.Uri"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithSegmentDoubleEncoding() {
            return new Plug(Uri.WithSegmentDoubleEncoding(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
        }

        /// <summary>
        /// Turn off double-encoding of segments when the Plug's <see cref="Uri"/> is converted to a <see cref="System.Uri"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug WithoutSegmentDoubleEncoding() {
            return new Plug(Uri.WithoutSegmentDoubleEncoding(), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects);
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
        public Task<DreamMessage> Post() {
            return Invoke(Verb.POST, DreamMessage.Ok(), DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb and an empty message.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Post(TimeSpan timeout) {
            return Invoke(Verb.POST, DreamMessage.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Post(DreamMessage message) {
            return Invoke(Verb.POST, message, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Post(string message) {
            return Invoke(Verb.POST, DreamMessage.Ok(MimeType.TEXT_UTF8, message), DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Post(DreamMessage message, TimeSpan timeout) {
            return Invoke(Verb.POST, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Post(string message, TimeSpan timeout) {
            return Invoke(Verb.POST, DreamMessage.Ok(MimeType.TEXT_UTF8, message), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.POST"/> verb with <see cref="Verb.GET"/> query arguments converted as form post body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> PostAsForm(TimeSpan timeout) {
            DreamMessage message = DreamMessage.Ok(Uri.Params);
            XUri uri = Uri.WithoutParams();
            return new Plug(uri, Timeout, Headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride, MaxAutoRedirects).Invoke(Verb.POST, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.PUT"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Put(DreamMessage message, TimeSpan timeout) {
            return Invoke(Verb.PUT, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb and no message body.
        /// </summary>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Get() {
            return Invoke(Verb.GET, DreamMessage.Ok(), DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb and no message body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Get(TimeSpan timeout) {
            return Invoke(Verb.GET, DreamMessage.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.GET"/> verb.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Get(DreamMessage message, TimeSpan timeout) {
            return Invoke(Verb.GET, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.HEAD"/> verb and no message body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Head(TimeSpan timeout) {
            return Invoke(Verb.HEAD, DreamMessage.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.OPTIONS"/> verb and no message body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Options(TimeSpan timeout) {
            return Invoke(Verb.OPTIONS, DreamMessage.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.DELETE"/> verb and no message body.
        /// </summary>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Delete(TimeSpan timeout) {
            return Invoke(Verb.DELETE, DreamMessage.Ok(), timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.DELETE"/> verb.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Delete(DreamMessage message, TimeSpan timeout) {
            return Invoke(Verb.DELETE, message, timeout);
        }

        /// <summary>
        /// Invoke the plug.
        /// </summary>
        /// <param name="verb">Request verb.</param>
        /// <param name="request">Request message.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> Invoke(string verb, DreamMessage request, TimeSpan timeout) {
            var hasTimeout = timeout != TimeSpan.MaxValue;
            var requestTimer = Stopwatch.StartNew();
            var completion = new TaskCompletionSource<DreamMessage>();
            InvokeEx(verb, request, timeout)
                .ContinueWith(t1 => {
                    requestTimer.Stop();
                    if(hasTimeout) {
                        timeout = timeout - requestTimer.Elapsed;
                    }
                    if(t1.IsFaulted) {
                        completion.SetException(t1.Exception);
                    } else {
                        _log.Debug("memorizing message");
                        t1.Result.Memorize(timeout)
                            .ContinueWith(t2 => {
                                _log.Debug("message memorized");
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
        /// Invoke the plug, but leave the stream unread so that the returned <see cref="DreamMessage"/> can be streamed.
        /// </summary>
        /// <param name="verb">Request verb.</param>
        /// <param name="request">Request message.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage> InvokeEx(string verb, DreamMessage request, TimeSpan timeout) {
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
            IPlugEndpoint match;
            XUri normalizedUri;
            FindPlugEndpoint(Uri, out match, out normalizedUri);

            // check if we found a match
            if(match == null) {
                request.Close();
                return new DreamMessage(DreamStatus.NoEndpointFound).AsCompletedTask();
            }

            // add matching cookies from service or from global cookie jar
            DreamCookieJar cookies = CookieJar;

            // prepare request
            try {
                request = PreProcess(verb, Uri, normalizedUri, _headers, cookies, request);

                // check if custom pre-processing handlers are registered
                if(_preHandlers != null) {
                    foreach(PlugHandler2 handler in _preHandlers) {
                        request = handler(verb, Uri, normalizedUri, request) ?? new DreamMessage(DreamStatus.RequestIsNull);
                        if(request.Status != DreamStatus.Ok) {
                            return request.AsCompletedTask();
                        }
                    }
                }
            } catch(Exception e) {
                request.Close();
                return DreamMessage.RequestFailed(e).AsCompletedTask();
            }

            // Note (arnec): Plug never throws, so we usurp the passed result if it has a timeout
            // setting the result timeout on inner result manually
            //var outerTimeout = result.Timeout;
            //if(outerTimeout != TimeSpan.MaxValue) {
            //    result.Timeout = TimeSpan.MaxValue;
            //}

            // if the governing result has a shorter timeout than the plug, it superceeds the plug timeout
            //var timeout = outerTimeout < Timeout ? outerTimeout : Timeout;
            var completion = new TaskCompletionSource<DreamMessage>();
            match.Invoke(this, verb, normalizedUri, request, timeout).ContinueWith(invokeTask => {
                _log.Debug("plug handler completed");
                if(invokeTask.IsFaulted) {
                    // an exception occurred somewhere during processing (not expected, but it could happen)
                    request.Close();

                    var status = DreamStatus.RequestFailed;
                    var e = invokeTask.UnwrapFault();
                    if(e is TimeoutException) {
                        status = DreamStatus.RequestConnectionTimeout;
                    }
                    completion.SetResult(new DreamMessage(status, e));
                    return;
                }
                var response = invokeTask.Result;
                try {
                    var message = PostProcess(verb, Uri, normalizedUri, _headers, cookies, response);
                    if((message.Status == DreamStatus.MovedPermanently ||
                        message.Status == DreamStatus.Found ||
                        message.Status == DreamStatus.TemporaryRedirect) &&
                       AutoRedirect &&
                       request.IsCloneable
                    ) {
                        var redirectPlug = new Plug(message.Headers.Location, Timeout, Headers, null, null, null, CookieJar, (ushort)(MaxAutoRedirects - 1));
                        var redirectMessage = request.Clone();
                        request.Close();
                        redirectPlug.InvokeEx(verb, redirectMessage, Timeout).ContinueWith(redirectTask => completion.SetResult(redirectTask.Result));
                    } else {
                        request.Close();
                        if(_postHandlers != null) {
                            foreach(PlugHandler2 handler in _postHandlers) {
                                response = handler(verb, Uri, normalizedUri, response) ?? new DreamMessage(DreamStatus.ResponseIsNull);
                            }
                        }
                        completion.SetResult(response);
                    }
                } catch(Exception e) {
                    request.Close();
                    completion.SetResult(new DreamMessage(DreamStatus.ResponseFailed, e));
                }
            });


            return completion.Task;
        }
        #endregion
    }
}
