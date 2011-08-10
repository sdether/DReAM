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
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MindTouch.Traum.Webclient {

    /// <summary>
    /// Static utility class containing extension and helper methods for Web and Http related tasks.
    /// </summary>
    internal static class HttpUtil {

        // NOTE (steveb): cookie parsing based on RFC2109 (http://rfc.net/rfc2109.html)

        //--- Class Fields ---
        private static readonly MethodInfo _addHeaderMethod = typeof(WebHeaderCollection).GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Regex _rangeRegex = new Regex(@"((?<rangeSpecifier>[^\s]+)\s*(=|\s))?\s*(?<from>\d+)(-(?<to>\d+))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Extension Methods ---

        /// <summary>
        /// Add a header to a web request.
        /// </summary>
        /// <param name="request">Target web request.</param>
        /// <param name="key">Header Key.</param>
        /// <param name="value">Header Value.</param>
        public static void AddHeader(this HttpWebRequest request, string key, string value) {
            if(string.Compare(key, DreamHeaders.ACCEPT, true) == 0) {
                request.Accept = value;
            } else if(string.Compare(key, DreamHeaders.CONNECTION, true) == 0) {

                // ignored: set automatically
                //request.Connection = value;
            } else if(string.Compare(key, DreamHeaders.CONTENT_LENGTH, true) == 0) {
                request.ContentLength = long.Parse(value);
            } else if(string.Compare(key, DreamHeaders.CONTENT_TYPE, true) == 0) {
                request.ContentType = value;
            } else if(string.Compare(key, DreamHeaders.EXPECT, true) == 0) {

                // ignored: set automatically
                // request.Expect = value;
            } else if(string.Compare(key, DreamHeaders.DATE, true) == 0) {

                // ignored: set automatically
            } else if(string.Compare(key, DreamHeaders.HOST, true) == 0) {

                // ignored: set automatically
            } else if(string.Compare(key, DreamHeaders.IF_MODIFIED_SINCE, true) == 0) {
                request.IfModifiedSince = DateTimeUtil.ParseInvariant(value);
            } else if(string.Compare(key, DreamHeaders.RANGE, true) == 0) {

                // read range-specifier, with range (e.g. "bytes=500-999")
                Match m = _rangeRegex.Match(value);
                if(m.Success) {
                    int from = int.Parse(m.Groups["from"].Value);
                    int to = m.Groups["to"].Success ? int.Parse(m.Groups["to"].Value) : -1;
                    string rangeSpecifier = m.Groups["rangeSpecifier"].Success ? m.Groups["rangeSpecifier"].Value : null;
                    if((rangeSpecifier != null) && (to >= 0)) {
                        request.AddRange(rangeSpecifier, from, to);
                    } else if(rangeSpecifier != null) {
                        request.AddRange(rangeSpecifier, from);
                    } else if(to >= 0) {
                        request.AddRange(from, to);
                    } else {
                        request.AddRange(from);
                    }
                }
            } else if(string.Compare(key, DreamHeaders.REFERER, true) == 0) {
                request.Referer = value;
            } else if(string.Compare(key, DreamHeaders.PROXY_CONNECTION, true) == 0) {

                // TODO (steveb): not implemented
#if DEBUG
                throw new NotImplementedException("missing code");
#endif
            } else if(string.Compare(key, DreamHeaders.TRANSFER_ENCODING, true) == 0) {

                // TODO (steveb): not implemented
#if DEBUG
                throw new NotImplementedException("missing code");
#endif
            } else if(string.Compare(key, DreamHeaders.USER_AGENT, true) == 0) {
                request.UserAgent = value;
            } else {
                request.Headers.Add(key, value);
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Parse all name value pairs from a header string.
        /// </summary>
        /// <param name="header">Header to be parsed.</param>
        /// <returns>Dictionary of header name value pairs.</returns>
        public static Dictionary<string, string> ParseNameValuePairs(string header) {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int index = 0;
            string name;
            string value;
            int count = 1;
            while(ParseNameValue(out name, out value, header, ref index, false)) {
                if(value == null) {
                    result["#" + count.ToString()] = name;
                    ++count;
                }
                result[name] = value;
            }
            return result;
        }

        private static bool ParseNameValue(out string name, out string value, string text, ref int index, bool useCommaAsSeparator) {
            name = null;
            value = null;
            SkipWhitespace(text, ref index);
            if(!ParseWord(out name, text, ref index)) {
                return false;
            }
            SkipWhitespace(text, ref index);
            if(MatchToken("=", text, ref index)) {
                int useCommaAsSeparatorStartingAtOffset = (useCommaAsSeparator ? 0 : int.MaxValue);

                // NOTE (steveb): 'expires' can contain commas, but cannot be quoted; so we need skip some characters when we find it before we allows commas again
                if(useCommaAsSeparator && name.EqualsInvariantIgnoreCase("expires")) {
                    useCommaAsSeparatorStartingAtOffset = 6;
                }

                if(!ParseValue(out value, text, ref index, useCommaAsSeparatorStartingAtOffset)) {
                    return false;
                }
            }
            SkipWhitespace(text, ref index);
            if(useCommaAsSeparator) {
                SkipSemiColon(text, ref index);
            } else {
                SkipDelimiter(text, ref index);
            }
            return true;
        }

        private static bool SkipWhitespace(string text, ref int index) {

            // skip whitespace
            while((index < text.Length) && char.IsWhiteSpace(text[index])) {
                ++index;
            }
            return true;
        }

        private static bool SkipSemiColon(string text, ref int index) {

            // skip whitespace
            if((index < text.Length) && (text[index] == ';')) {
                ++index;
            }
            return true;
        }

        private static bool SkipDelimiter(string text, ref int index) {

            // skip whitespace
            if((index < text.Length) && ((text[index] == ',') || (text[index] == ';'))) {
                ++index;
            }
            return true;
        }

        private static bool MatchToken(string token, string text, ref int index) {
            if(string.Compare(text, index, token, 0, token.Length) == 0) {
                index += token.Length;
                return true;
            }
            return false;
        }

        private static bool ParseWord(out string word, string text, ref int index) {
            word = null;
            if(index >= text.Length) {
                return false;
            }

            // check if we're parsing a quoted string
            if(text[index] == '"') {
                ++index;
                int last;
                for(last = index; (last < text.Length) && (text[last] != '"'); ++last) {

                    // check if the next character is escaped
                    if(text[last] == '\\') {
                        ++last;
                        if(last == text.Length) {
                            break;
                        }
                    }
                }
                if(last == text.Length) {
                    word = text.Substring(index);
                    index = last;
                } else {
                    word = text.Substring(index, last - index);
                    index = last + 1;
                }
                word = word.UnescapeString();
                return true;
            } else {

                // parse an alphanumeric token
                int last;
                for(last = index; (last < text.Length) && IsTokenChar(text[last]); ++last) { }
                if(last == index) {
                    return false;
                }
                word = text.Substring(index, last - index);
                index = last;
                return true;
            }
        }

        private static bool ParseValue(out string word, string text, ref int index, int useCommaAsSeparatorStartingAtOffset) {
            word = null;
            if(index >= text.Length) {
                return false;
            }

            // check if we're parsing a quoted string
            if(text[index] == '"') {
                ++index;
                int last;
                for(last = index; (last < text.Length) && (text[last] != '"'); ++last) {

                    // check if the next character is escaped
                    if(text[last] == '\\') {
                        ++last;
                        if(last == text.Length) {
                            break;
                        }
                    }
                }
                if(last == text.Length) {
                    word = text.Substring(index);
                    index = last;
                } else {
                    word = text.Substring(index, last - index);
                    index = last + 1;
                }
                word = word.UnescapeString();
                return true;
            } else {

                // parse an alphanumeric token
                int last;
                for(last = index; (last < text.Length) && (text[last] != ';' && (((last - index) <= useCommaAsSeparatorStartingAtOffset) || (text[last] != ','))); ++last) { }

                word = text.Substring(index, last - index).Trim();
                index = last;
                return true;
            }
        }

        private static bool IsTokenChar(char c) {
            return ((c >= 'A') && (c <= 'Z')) || ((c >= 'a') && (c <= 'z')) ||
                ((c > 32) && (c < 127) && (c != '(') && (c != ')') && (c != '<') &&
                (c != '<') && (c != '>') && (c != '@') && (c != ',') && (c != ';') &&
                (c != ':') && (c != '\\') && (c != '"') && (c != '/') && (c != '[') &&
                (c != ']') && (c != '?') && (c != '=') && (c != '{') && (c != '}'));
        }

        private static void UnsafeAddHeader(WebHeaderCollection collection, string key, string value) {
            _addHeaderMethod.Invoke(collection, new object[] { key, value });
        }
    }
}
