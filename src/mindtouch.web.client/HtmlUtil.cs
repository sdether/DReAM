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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Sgml;

namespace MindTouch.Dream {
    public static class HtmlUtil {
        
        //--- Class Fields ---
        private static Dictionary<string, Entity> _literals;
        private static Dictionary<string, string> _entities;
        private static Regex _specialSymbolRegEx = new Regex("[&<>\x22\u0080-\uFFFF]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Regex _htmlEntitiesRegEx = new Regex("&(?<value>#(x[a-f0-9]+|[0-9]+)|[a-z0-9]+);", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
 
        //--- Class Properties ---
        private static Dictionary<string, Sgml.Entity> LiteralNameLookup {
            get {
                if(_literals == null) {
                    Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader();
                    sgmlReader.DocType = "HTML";
                    _literals = sgmlReader.Dtd.GetEntitiesLiteralNameLookup();
                }
                return _literals;
            }
        }

        private static Dictionary<string, string> EntityNameLookup {
            get {
                if(_entities == null) {
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    foreach(KeyValuePair<string, Sgml.Entity> entry in LiteralNameLookup) {
                        result[entry.Value.Name] = entry.Key;
                    }
                    _entities = result;
                }
                return _entities;
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Encode any html entities in a string.
        /// </summary>
        /// <param name="text">String to encode.</param>
        /// <param name="encoding">Text encoding to use.</param>
        /// <param name="useEntityNames">If <see langword="True"/>, encodes html entity using entity name rather than numeric entity code.</param>
        /// <returns>Encoded string.</returns>
        public static string EncodeHtmlEntities(this string text, Encoding encoding, bool useEntityNames) {
            return _specialSymbolRegEx.Replace(text, delegate(Match m) {
                string v = m.Groups[0].Value;
                switch(v) {
                case "&":
                    return "&amp;";
                case "<":
                    return "&lt;";
                case ">":
                    return "&gt;";
                case "\"":
                    return "&quot;";
                }

                // default case
                Sgml.Entity e;
                if(useEntityNames && LiteralNameLookup.TryGetValue(v, out e)) {
                    return "&" + e.Name + ";";
                }
                return (encoding == Encoding.ASCII) ? "&#" + (int)v[0] + ";" : v;
            }, int.MaxValue);
        }

        /// <summary>
        /// Encode any html entities in a string.
        /// </summary>
        /// <param name="text">String to encode.</param>
        /// <returns>Encoded string.</returns>
        public static string EncodeHtmlEntities(this string text) {
            return EncodeHtmlEntities(text, Encoding.UTF8, true);
        }

        /// <summary>
        /// Encode any html entities in a string.
        /// </summary>
        /// <param name="text">String to encode.</param>
        /// <param name="encoding">Text encoding to use.</param>
        /// <returns>Encoded string.</returns>
        public static string EncodeHtmlEntities(this string text, Encoding encoding) {
            return EncodeHtmlEntities(text, encoding, true);
        }

        /// <summary>
        /// Decode Html entities.
        /// </summary>
        /// <param name="text">Html encoded string.</param>
        /// <returns>Decoded string.</returns>
        public static string DecodeHtmlEntities(this string text) {
            return _htmlEntitiesRegEx.Replace(text, delegate(Match m) {
                string v = m.Groups["value"].Value;
                if(v[0] == '#') {
                    if(char.ToLowerInvariant(v[1]) == 'x') {
                        string value = v.Substring(2);
                        return ((char)int.Parse(value, NumberStyles.HexNumber)).ToString();
                    } else {
                        string value = v.Substring(1);
                        return ((char)int.Parse(value)).ToString();
                    }
                } else {
                    string value;
                    if(EntityNameLookup.TryGetValue(v, out value)) {
                        return value;
                    }
                    return m.Groups[0].Value;
                }
            }, int.MaxValue);
        }

    }
}
