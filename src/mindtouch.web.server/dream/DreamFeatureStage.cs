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
using System.IO;
using System.Linq;
using System.Reflection;
using MindTouch.dream;
using MindTouch.Extensions;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream {
    using Yield = IEnumerator<IYield>;
    using DreamFeatureAdapter = Func<DreamContext, DreamMessage, Result<DreamMessage>, object>;

    /// <summary>
    /// Provides a single stage in a <see cref="DreamFeature"/> processing chain.
    /// </summary>
    public sealed class DreamFeatureStage {

        //--- Constants ---
        private const string UNNAMED = "--unnamed--";

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---

        /// <summary>
        /// Stage name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Stage access level.
        /// </summary>
        public readonly DreamAccess Access;

        private readonly CoroutineHandler<DreamContext, DreamMessage, Result<DreamMessage>> _handler;
        private readonly MethodInfo _method;
        private readonly List<DreamFeatureAdapter> _plan;
        private readonly IDreamService _service;

        //--- Constructors ---

        /// <summary>
        /// Creates a new stage instance.
        /// </summary>
        /// <param name="name">Stage name.</param>
        /// <param name="handler">Stage handler.</param>
        /// <param name="access">Stage access level.</param>
        public DreamFeatureStage(string name, CoroutineHandler<DreamContext, DreamMessage, Result<DreamMessage>> handler, DreamAccess access) {
            if(handler == null) {
                throw new ArgumentNullException("handler");
            }
            this.Name = name ?? UNNAMED;
            this.Access = access;
            _handler = handler;
        }

        /// <summary>
        /// Creates a new stage instance.
        /// </summary>
        /// <param name="service">Service instance to which the stage belongs to.</param>
        /// <param name="method">Method definintion for stage handler.</param>
        /// <param name="access">Stage access level.</param>
        public DreamFeatureStage(IDreamService service, MethodInfo method, DreamAccess access) {
            if(service == null) {
                throw new ArgumentNullException("service");
            }
            if(method == null) {
                throw new ArgumentNullException("method");
            }
            this.Name = service.GetType().FullName + "!" + method.Name;
            this.Access = access;
            _method = method;
            _service = service;

            // determine what kind of method we were given
            var parameters = method.GetParameters();
            if((method.ReturnType == typeof(Yield)) && (parameters.Length == 3) && (parameters[0].ParameterType == typeof(DreamContext)) && (parameters[1].ParameterType == typeof(DreamMessage)) && (parameters[2].ParameterType == typeof(Result<DreamMessage>))) {

                // classical coroutine feature handler
                _handler = (CoroutineHandler<DreamContext, DreamMessage, Result<DreamMessage>>)Delegate.CreateDelegate(typeof(CoroutineHandler<DreamContext, DreamMessage, Result<DreamMessage>>), service, method);
            } else {

                // validate method return type
                if(method.ReturnType != typeof(Yield) && method.ReturnType != typeof(DreamMessage)) {
                    throw new InvalidCastException(string.Format("feature handler '{0}' has return type {1}, but should be either DreamMessage or IEnumerator<IYield>", method.Name, method.ReturnType));
                }

                // create an execution plan for fetching the necessary parameters to invoke the method
                _plan = new List<DreamFeatureAdapter>();
                foreach(var param in method.GetParameters()) {
                    var attributes = param.GetCustomAttributes(false);
                    QueryAttribute queryParam = (QueryAttribute)attributes.FirstOrDefault(i => i is QueryAttribute);
                    PathAttribute pathParam = (PathAttribute)attributes.FirstOrDefault(i => i is PathAttribute);
                    HeaderAttribute header = (HeaderAttribute)attributes.FirstOrDefault(i => i is HeaderAttribute);
                    CookieAttribute cookie = (CookieAttribute)attributes.FirstOrDefault(i => i is CookieAttribute);

                    // check attribute-based parameters
                    if(queryParam != null) {
                        Assert(method, param, typeof(string), typeof(string[]));

                        // check if a single or a list of query parameters are requested
                        if(param.ParameterType == typeof(string)) {
                            _plan.Add(MakeContextParamGetter(queryParam.Name ?? param.Name));
                        } else if(param.ParameterType == typeof(string[])) {
                            _plan.Add(MakeContextParamListGetter(queryParam.Name ?? param.Name));
                        } else {
                            throw new ShouldNeverHappenException();
                        }
                    } else if(pathParam != null) {
                        Assert(method, param, typeof(string));
                        _plan.Add(MakeContextParamGetter(pathParam.Name ?? param.Name));
                    } else if(cookie != null) {
                        Assert(method, param, typeof(string), typeof(DreamCookie));

                        // check which cookie type is requested
                        if(param.ParameterType == typeof(string)) {
                            _plan.Add(MakeRequestCookieValueGetter(cookie.Name ?? param.Name));
                        } else if(param.ParameterType == typeof(DreamCookie)) {
                            _plan.Add(MakeRequestCookieGetter(cookie.Name ?? param.Name));                            
                        } else {
                            throw new ShouldNeverHappenException();
                        }
                    } else if(header != null) {
                        Assert(method, param, typeof(string));
                        _plan.Add(MakeRequestHeaderGetter(header.Name ?? param.Name));
                    } else {

                        // check name-based parameters
                        if(param.Name.EqualsInvariant("verb")) {
                            Assert(method, param, typeof(string));
                            _plan.Add(GetContextVerb);
                        } else if(param.Name.EqualsInvariant("path")) {
                            Assert(method, param, typeof(string[]));
                            _plan.Add(GetContextFeatureSubpath);
                        } else if(param.Name.EqualsInvariant("uri")) {
                            Assert(method, param, typeof(XUri));
                            _plan.Add(GetContextUri);
                        } else if(param.Name.EqualsInvariant("body")) {
                            Assert(method, param, typeof(XDoc), typeof(string), typeof(Stream), typeof(byte[]));

                            // check which body type is requested
                            if(param.ParameterType == typeof(XDoc)) {
                                _plan.Add(GetRequestAsDocument);
                            } else if(param.ParameterType == typeof(string)) {
                                _plan.Add(GetRequestAsText);
                            } else if(param.ParameterType == typeof(Stream)) {
                                _plan.Add(GetRequestAsStream);
                            } else if(param.ParameterType == typeof(byte[])) {
                                _plan.Add(GetRequestAsBytes);
                            } else {
                                throw new ShouldNeverHappenException();
                            }
                        } else {
                            Assert(method, param, typeof(DreamContext), typeof(DreamMessage), typeof(Result<DreamMessage>), typeof(DreamCookie), typeof(string), typeof(string[]));

                            // check type-based parameters
                            if(param.ParameterType == typeof(DreamContext)) {
                                _plan.Add(GetContext);
                            } else if(param.ParameterType == typeof(DreamMessage)) {
                                _plan.Add(GetRequest);
                            } else if(param.ParameterType == typeof(Result<DreamMessage>)) {
                                _plan.Add(GetResponse);
                            } else if(param.ParameterType == typeof(DreamCookie)) {
                                _plan.Add(MakeRequestCookieGetter(param.Name));
                            } else if(param.ParameterType == typeof(string)) {
                                _plan.Add(MakeContextParamGetter(param.Name));
                            } else if(param.ParameterType == typeof(string[])) {
                                _plan.Add(MakeContextParamListGetter(param.Name));
                            } else {
                                throw new ShouldNeverHappenException();
                            }
                        }
                    }
                }
            }
        }

        //--- Methods ---

        /// <summary>
        /// Invoke the stage method.
        /// </summary>
        /// <param name="context"><see cref="DreamContext"/> for invocation.</param>
        /// <param name="request"><see cref="DreamMessage"/> for invocation.</param>
        /// <param name="response"><see cref="Result{DreamMessage}"/> for invocations.</param>
        public void Invoke(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(_handler != null) {
                Coroutine.Invoke(_handler, context, request, response);
            } else {
                try {

                    // build parameter list
                    object[] arguments = new object[_plan.Count];
                    for(int i = 0; i < _plan.Count; ++i) {
                        arguments[i] = _plan[i](context, request, response);
                    }

                    // invoke method
                    if(_method.ReturnType == typeof(Yield)) {

                        // invoke method as coroutine
                        new Coroutine(_method, response).Invoke(() => (Yield)_method.InvokeWithRethrow(_service, arguments));
                    } else {
                        response.Return((DreamMessage)_method.InvokeWithRethrow(_service, arguments));
                    }
                } catch(Exception e) {
                    response.Throw(e);
                }
            }
        }

        private static void Assert(MethodInfo method, ParameterInfo param, params Type[] types) {
            if(types.Length == 0) {
                return;
            }
            if(types.Length == 1) {
                if(param.ParameterType != types[0]) {
                    throw new InvalidCastException(string.Format("feature handler '{0}' parameter '{1}' has type {3}, but should be {2}", method.Name, param.Name, types[0], param.ParameterType));
                }
            } else {
                foreach(var type in types) {
                    if(type == param.ParameterType) {
                        return;
                    }
                }
                var allowedTypes = string.Join(", ", (from type in types select type.FullName).ToArray());
                throw new InvalidCastException(string.Format("feature handler '{0}' parameter '{1}' has type {3}, but should be one of {2}", method.Name, param.Name, allowedTypes, param.ParameterType));
            }
        }

        private static object GetRequest(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return request;
        }

        private static object GetRequestAsText(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return request.ToText();
        }

        private static object GetRequestAsDocument(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return request.ToDocument();
        }

        private static object GetRequestAsStream(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return request.ToStream();
        }

        private static object GetRequestAsBytes(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return request.ToBytes();
        }

        private static object GetContext(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return context;
        }

        private static object GetContextVerb(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return context.Verb;
        }

        private static object GetContextUri(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return context.Uri;
        }

        private static object GetContextFeatureSubpath(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return context.Uri.GetRelativePathTo(context.Feature.ServiceUri).Split('/');
        }

        private static object GetResponse(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return response;
        }

        private static DreamFeatureAdapter MakeContextParamGetter(string name) {
            return (context, request, response) => context.GetParam(name, null);
        }

        private static DreamFeatureAdapter MakeContextParamListGetter(string name) {
            return (context, request, response) => context.GetParams(name);
        }

        private static DreamFeatureAdapter MakeRequestHeaderGetter(string name) {
            return (context, request, response) => request.Headers[name];
        }

        private static DreamFeatureAdapter MakeRequestCookieGetter(string name) {
            return (context, request, response) => (from cookie in request.Headers.Cookies where cookie.Name.EqualsInvariant(name) select cookie).FirstOrDefault();
        }

        private static DreamFeatureAdapter MakeRequestCookieValueGetter(string name) {
            return (context, request, response) => (from cookie in request.Headers.Cookies where cookie.Name.EqualsInvariant(name) select cookie.Value).FirstOrDefault();
        }
    }
}