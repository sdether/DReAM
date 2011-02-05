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
using System.Linq;
using MindTouch.Xml;

namespace MindTouch.Dream.Web.Client {

    /// <summary>
    /// Provides Dream specific extension methods for <see cref="XUri"/>.
    /// </summary>
    public static class XUriEx {

        //--- Types ---
        internal interface IUriTranslator {
            XUri AsPublicUri(XUri uri);
            XUri AsLocalUri(XUri uri);
            XUri AsServerUri(XUri uri);
        }
        
        //--- Class Fields
        internal static IUriTranslator UriTranslator;

        //--- Extension Methods ---

        /// <summary>
        /// Get a typed parameter from the Uri.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="uri">Input Uri.</param>
        /// <param name="key">Parameter key.</param>
        /// <param name="def">Default value to return in case parameter does not exist.</param>
        /// <returns>Parameter value or default.</returns>
        public static T GetParam<T>(this XUri uri, string key, T def) {
            return GetParam(uri, key, 0, def);
        }

        /// <summary>
        /// Get a typed parameter from the Uri.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="uri">Input Uri.</param>
        /// <param name="key">Parameter key.</param>
        /// <param name="index">Parameter index.</param>
        /// <param name="def">Default value to return in case parameter does not exist.</param>
        /// <returns>Parameter value or default.</returns>
        public static T GetParam<T>(this XUri uri, string key, int index, T def) {
            string value = uri.GetParam(key, index, null);
            if(!string.IsNullOrEmpty(value)) {
                return (T)SysUtil.ChangeType(value, typeof(T));
            }
            return def;
        }

        public static XUri AsLocalUri(this XUri uri) {
            return UriTranslator == null ? uri : UriTranslator.AsLocalUri(uri);
        }

        public static XUri AsPublicUri(this XUri uri) {
            return UriTranslator == null ? uri : UriTranslator.AsPublicUri(uri);
        }

        public static XUri AsServerUri(this XUri uri) {
            return UriTranslator == null ? uri : UriTranslator.AsServerUri(uri);
        }
    }
}