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
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Web;
using MindTouch.Xml;

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

        //--- Types ---
        internal interface IPlugInterceptor {
            DreamMessage2 PreProcess(string verb, XUri uri, XUri normalizedUri, DreamMessage2 message, DreamCookieJar cookies);
            DreamMessage2 PostProcess(string verb, XUri uri, XUri normalizedUri, DreamMessage2 message, DreamCookieJar cookies);
        }

        internal interface ICookieJarSource {
            DreamCookieJar CookieJar { get; }
        }

        //--- Constants ---

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
        private static IPlugInterceptor _interceptor;
        private static ICookieJarSource _cookieJarSource;

        //--- Class Constructors ---
        static Plug2() {

            // let's find all IPlugEndpoint derived, concrete classes
            foreach(Type type in typeof(Plug2).Assembly.GetTypes()) {
                if(!typeof(IPlugEndpoint2).IsAssignableFrom(type) || !type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition) {
                    continue;
                }
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if(ctor != null) {
                    AddEndpoint((IPlugEndpoint2)ctor.Invoke(null));
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
                return new Plug2(new XUri(uri), timeout, null, null, null, null, null);
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
                return new Plug2(new XUri(uri), timeout, null, null, null, null, null);
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
                return new Plug2(uri, timeout, null, null, null, null, null);
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

        internal static void SetInterceptor(IPlugInterceptor interceptor) {
            _interceptor = interceptor;
        }

        internal static void SetCookieJarSource(ICookieJarSource cookieJarSource) {
            _cookieJarSource = cookieJarSource;
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
        public Plug2(XUri uri, TimeSpan timeout, DreamHeaders headers, List<PlugHandler2> preHandlers, List<PlugHandler2> postHandlers, ICredentials credentials, DreamCookieJar cookieJarOverride) {
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
                return _cookieJarOverride
                    ?? ((_cookieJarSource != null) ? _cookieJarSource.CookieJar : null)
                    ?? GlobalCookies;
            }
        }

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
            return new Plug2(Uri.At(segments), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a path/query/fragement appended to its Uri.
        /// </summary>
        /// <param name="path">Path/Query/fragment string.</param>
        /// <returns>New instance.</returns>
        public Plug2 AtPath(string path) {
            return new Plug2(Uri.AtPath(path), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, string value) {
            return new Plug2(Uri.With(key, value), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, bool value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, int value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, long value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, decimal value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, double value) {
            return new Plug2(Uri.With(key, value.ToString()), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a query key/value pair added.
        /// </summary>
        /// <param name="key">Query key.</param>
        /// <param name="value">Query value.</param>
        /// <returns>New instance.</returns>
        public Plug2 With(string key, DateTime value) {
            return new Plug2(Uri.With(key, value.ToUniversalTime().ToString("R")), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with additional query parameters.
        /// </summary>
        /// <param name="args">Array of query key/value pairs.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithParams(KeyValuePair<string, string>[] args) {
            return new Plug2(Uri.WithParams(args), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with the provided querystring added.
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithQuery(string query) {
            return new Plug2(Uri.WithQuery(query), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with parameters from another uri added.
        /// </summary>
        /// <param name="uri">Uri to extract parameters from.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithParamsFrom(XUri uri) {
            return new Plug2(Uri.WithParamsFrom(uri), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with the given credentials
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="password">Password.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithCredentials(string user, string password) {
            return new Plug2(Uri.WithCredentials(user, password), Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with the given credentials
        /// </summary>
        /// <param name="credentials">Credential instance.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithCredentials(ICredentials credentials) {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with credentials removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutCredentials() {
            return new Plug2(Uri.WithoutCredentials(), Timeout, _headers, _preHandlers, _postHandlers, null, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with an override cookie jar.
        /// </summary>
        /// <param name="cookieJar">Cookie jar to use.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithCookieJar(DreamCookieJar cookieJar) {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, cookieJar);
        }

        /// <summary>
        /// Create a copy of the instance with any override cookie jar removed.
        /// </summary>
        /// <remarks>Will fall back on service or global jar.</remarks>
        /// <returns>New instance.</returns>
        public Plug2 WithoutCookieJar() {
            return new Plug2(Uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, null);
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
            DreamHeaders newHeaders = new DreamHeaders(_headers);
            newHeaders.Add(name, value);
            return new Plug2(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a header collection added.
        /// </summary>
        /// <param name="headers">Header collection</param>
        /// <returns>New instance.</returns>
        public Plug2 WithHeaders(DreamHeaders headers) {
            if(headers != null) {
                DreamHeaders newHeaders = new DreamHeaders(_headers);
                newHeaders.AddRange(headers);
                return new Plug2(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
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
            return new Plug2(Uri, Timeout, newHeaders, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with all headers removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutHeaders() {
            return new Plug2(Uri, Timeout, null, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a pre-invocation handler added.
        /// </summary>
        /// <param name="preHandlers">Pre-invocation handler.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithPreHandler(params PlugHandler2[] preHandlers) {
            List<PlugHandler2> list = (_preHandlers != null) ? new List<PlugHandler2>(_preHandlers) : new List<PlugHandler2>();
            list.AddRange(preHandlers);
            return new Plug2(Uri, Timeout, _headers, list, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a post-invocation handler added.
        /// </summary>
        /// <param name="postHandlers">Post-invocation handler.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithPostHandler(params PlugHandler2[] postHandlers) {
            List<PlugHandler2> list = new List<PlugHandler2>(postHandlers);
            if(_postHandlers != null) {
                list.AddRange(_postHandlers);
            }
            return new Plug2(Uri, Timeout, _headers, _preHandlers, list, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with all handlers removed.
        /// </summary>
        /// <returns>New instance.</returns>
        public Plug2 WithoutHandlers() {
            return new Plug2(Uri, Timeout, _headers, null, null, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Create a copy of the instance with a new timeout.
        /// </summary>
        /// <param name="timeout">Invocation timeout.</param>
        /// <returns>New instance.</returns>
        public Plug2 WithTimeout(TimeSpan timeout) {
            return new Plug2(Uri, timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride);
        }

        /// <summary>
        /// Provide a string representation of the Uri of the instance.
        /// </summary>
        /// <returns>Uri string.</returns>
        public override string ToString() {
            return Uri.ToString();
        }

        #region --- Blocking Methods ---

        /// <summary>
        /// Blocking version of <see cref="Post(TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
        public DreamMessage2 Post() {
            return WaitAndConfirm(Invoke(Verb.POST, DreamMessage2.Ok(XDoc.Empty), TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Post(MindTouch.Xml.XDoc,TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="doc"></param>
        /// <returns></returns>
        public DreamMessage2 Post(XDoc doc) {
            return WaitAndConfirm(Invoke(Verb.POST, DreamMessage2.Ok(doc), TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Post(DreamMessage2,TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
        public DreamMessage2 Post(DreamMessage2 message) {
            return WaitAndConfirm(Invoke(Verb.POST, message, TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="PostAsForm(TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
        public DreamMessage2 PostAsForm() {
            DreamMessage2 message = DreamMessage2.Ok(Uri.Params);
            XUri uri = Uri.WithoutParams();
            return WaitAndConfirm(new Plug2(uri, Timeout, _headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride).Invoke(Verb.POST, message, TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Put(MindTouch.Xml.XDoc,TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="doc"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage2 Put(XDoc doc) {
            return WaitAndConfirm(Invoke(Verb.PUT, DreamMessage2.Ok(doc), TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Put(MindTouch.Traum.DreamMessage2,TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage2 Put(DreamMessage2 message) {
            return WaitAndConfirm(Invoke(Verb.PUT, message, TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Get(TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage2 Get() {
            return WaitAndConfirm(Invoke(Verb.GET, DreamMessage2.Ok(), TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Get(MindTouch.Traum.DreamMessage2,TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage2 Get(DreamMessage2 message) {
            return WaitAndConfirm(Invoke(Verb.GET, message, TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Head(TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage2 Head() {
            return WaitAndConfirm(Invoke(Verb.HEAD, DreamMessage2.Ok(), TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Options(TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage2 Options() {
            return WaitAndConfirm(Invoke(Verb.OPTIONS, DreamMessage2.Ok(), TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Delete(TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage2 Delete() {
            return WaitAndConfirm(Invoke(Verb.DELETE, DreamMessage2.Ok(), TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Delete(MindTouch.Xml.XDoc,TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="doc"></param>
        /// <returns></returns>
#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        public DreamMessage2 Delete(XDoc doc) {
            return WaitAndConfirm(Invoke(Verb.DELETE, DreamMessage2.Ok(doc), TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Delete(DreamMessage2,TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
        public DreamMessage2 Delete(DreamMessage2 message) {
            return WaitAndConfirm(Invoke(Verb.DELETE, message, TimeSpan.MaxValue));
        }

        /// <summary>
        /// Blocking version of <see cref="Invoke(string,DreamMessage2,TimeSpan)"/>
        /// </summary>
        /// <remarks>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </remarks>
        /// <param name="verb"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public DreamMessage2 Invoke(string verb, DreamMessage2 message) {
            return WaitAndConfirm(Invoke(verb, message, TimeSpan.MaxValue));
        }
        #endregion

        #region --- Iterative Methods ---

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
        /// <param name="doc">Document to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Post(XDoc doc, TimeSpan timeout) {
            return Invoke(Verb.POST, DreamMessage2.Ok(doc), timeout);
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
            return new Plug2(uri, Timeout, Headers, _preHandlers, _postHandlers, Credentials, _cookieJarOverride).Invoke(Verb.POST, message, timeout);
        }

        /// <summary>
        /// Invoke the plug with the <see cref="Verb.PUT"/> verb.
        /// </summary>
        /// <param name="doc">Document to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Put(XDoc doc, TimeSpan timeout) {
            return Invoke(Verb.PUT, DreamMessage2.Ok(doc), timeout);
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
        /// <param name="doc">Document to send.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public Task<DreamMessage2> Delete(XDoc doc, TimeSpan timeout) {
            return Invoke(Verb.DELETE, DreamMessage2.Ok(doc), timeout);
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

            // Note (arnec): Plug never throws, so we remove the timeout from the result (if it has one), 
            // and pass it into our coroutine manually.
            //var timeout = result.Timeout;
            //if(timeout != TimeSpan.MaxValue) {
            //    result.Timeout = TimeSpan.MaxValue;
            //}
            return Invoke_Helper(verb, request, timeout);
        }

        private async Task<DreamMessage2> Invoke_Helper(string verb, DreamMessage2 request, TimeSpan timeout) {
            var hasTimeout = timeout != TimeSpan.MaxValue;
            var requestTimer = Stopwatch.StartNew();
            var message = await InvokeEx(verb, request, timeout);
            requestTimer.Stop();
            if(hasTimeout) {
                timeout = timeout - requestTimer.Elapsed;
            }
            try {
                await message.Memorize(timeout);
            } catch(TimeoutException e) {
                return new DreamMessage2(DreamStatus.ResponseDataTransferTimeout, null, new XException2(e));
            } catch(Exception e) {
                return new DreamMessage2(DreamStatus.ResponseFailed, null, new XException2(e));
            }
            return message;
        }

        /// <summary>
        /// Invoke the plug, but leave the stream unread so that the returned <see cref="DreamMessage"/> can be streamed.
        /// </summary>
        /// <param name="verb">Request verb.</param>
        /// <param name="request">Request message.</param>
        /// <param name="timeout">The timeout for this asynchronous call.</param>
        /// <returns>Synchronization handle.</returns>
        public async Task<DreamMessage2> InvokeEx(string verb, DreamMessage2 request, TimeSpan timeout) {
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
                return new DreamMessage2(DreamStatus.NoEndpointFound, null, XDoc.Empty);
            }

            // add matching cookies from service or from global cookie jar
            DreamCookieJar cookies = CookieJar;

            // prepare request
            try {

                // check if custom pre-processing handlers are registered
                if(cookies != null) {
                    lock(cookies) {
                        request.Cookies.AddRange(cookies.Fetch(Uri));
                    }
                }

                // transfer plug headers
                request.Headers.AddRange(_headers);

                if(_preHandlers != null) {
                    foreach(PlugHandler2 handler in _preHandlers) {
                        request = handler(verb, Uri, normalizedUri, request) ?? new DreamMessage2(DreamStatus.RequestIsNull, null, XDoc.Empty);
                        if(request.Status != DreamStatus.Ok) {
                            return request;
                        }
                    }
                }
            } catch(Exception e) {
                return new DreamMessage2(DreamStatus.RequestFailed, null, new XException2(e));
            }

            // Note (arnec): Plug never throws, so we usurp the passed result if it has a timeout
            // setting the result timeout on inner result manually
            //var outerTimeout = result.Timeout;
            //if(outerTimeout != TimeSpan.MaxValue) {
            //    result.Timeout = TimeSpan.MaxValue;
            //}

            // if the governing result has a shorter timeout than the plug, it superceeds the plug timeout
            //var timeout = outerTimeout < Timeout ? outerTimeout : Timeout;

            DreamMessage2 response = null;
            try {
                response = await match.Invoke(this, verb, normalizedUri, request, timeout);

            } catch(Exception e) {
                // an exception occurred somewhere during processing (not expected, but it could happen)
                request.Close();
                var status = DreamStatus.RequestFailed;
                if(e is TimeoutException) {
                    status = DreamStatus.RequestConnectionTimeout;
                }
                return new DreamMessage2(status, null, new XException2(e));

            }
            try {
                if(response.IsSuccessful && response.HasCookies) {

                    // add matching cookies to service or to global cookie jar
                    if(cookies != null) {
                        lock(cookies) {
                            if(!Uri.Scheme.EqualsInvariant("local") && normalizedUri.Scheme.EqualsInvariant("local")) {

                                // need to translate cookies as they leave the dreamcontext
                                cookies.Update(DreamCookie.ConvertToPublic(response.Cookies), Uri);
                            } else {
                                cookies.Update(response.Cookies, Uri);
                            }
                        }
                    }
                }

                // check if custom post-processing handlers are registered
                if(_postHandlers != null) {
                    foreach(PlugHandler2 handler in _postHandlers) {
                        response = handler(verb, Uri, normalizedUri, response) ?? new DreamMessage2(DreamStatus.ResponseIsNull, null, XDoc.Empty);
                    }
                }
            } catch(Exception e) {
                return new DreamMessage2(DreamStatus.ResponseFailed, null, new XException2(e));
            }
            return response;
        }
        #endregion
    }

    public interface IPlugPrePostProcessor { }
}
