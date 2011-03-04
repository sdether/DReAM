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
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Principal;

using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream {
    using Yield = IEnumerator<IYield>;

    /// <summary>
    /// Common Http verbs.
    /// </summary>
    public static class Verb {

        //--- Constants ---

        /// <summary>
        /// POST verb.
        /// </summary>
        public const string POST = "POST";

        /// <summary>
        /// PUT verb.
        /// </summary>
        public const string PUT = "PUT";

        /// <summary>
        /// GET verb.
        /// </summary>
        public const string GET = "GET";

        /// <summary>
        /// DELETE verb.
        /// </summary>
        public const string DELETE = "DELETE";

        /// <summary>
        /// HEAD verb.
        /// </summary>
        public const string HEAD = "HEAD";

        /// <summary>
        /// OPTIONS verb.
        /// </summary>
        public const string OPTIONS = "OPTIONS";
    }

    /// <summary>
    /// Common URI schemes
    /// </summary>
    public static class Scheme {

        //--- Constants ---

        /// <summary>
        /// Hyper-text transport protocol.
        /// </summary>
        public const string HTTP = "http";

        /// <summary>
        /// Secure Hyper-text transport protocol.
        /// </summary>
        public const string HTTPS = "https";

        /// <summary>
        /// eXtensible Resource Identifier.
        /// </summary>
        public const string XRI = "xri";

        /// <summary>
        /// Dream Host internal transport.
        /// </summary>
        public const string LOCAL = "local";
    }

    /// <summary>
    /// Dream specific input query parameters.
    /// </summary>
    public static class DreamInParam {

        //--- Constants ---

        /// <summary>
        /// Request message format
        /// </summary>
        public const string FORMAT = "dream.in.format";

        /// <summary>
        /// Request originating host.
        /// </summary>
        public const string HOST = "dream.in.host";

        /// <summary>
        /// Request originating path root.
        /// </summary>
        public const string ROOT = "dream.in.root";

        /// <summary>
        /// Request originating verb.
        /// </summary>
        public const string VERB = "dream.in.verb";

        /// <summary>
        /// Request origin.
        /// </summary>
        public const string ORIGIN = "dream.in.origin";

        /// <summary>
        /// Request originating scheme.
        /// </summary>
        public const string SCHEME = "dream.in.scheme";

        /// <summary>
        /// Original request uri.
        /// </summary>
        public const string URI = "dream.in.uri";
    }

    /// <summary>
    /// Dream specific output query parameters.
    /// </summary>
    public static class DreamOutParam {

        //--- Constants ---

        /// <summary>
        /// Expected response message format.
        /// </summary>
        public const string FORMAT = "dream.out.format";

        /// <summary>
        /// Response prefix.
        /// </summary>
        public const string PREFIX = "dream.out.pre";

        /// <summary>
        /// Response postix.
        /// </summary>
        public const string POSTFIX = "dream.out.post";

        /// <summary>
        /// Response callback.
        /// </summary>
        public const string CALLBACK = "dream.out.callback";

        /// <summary>
        /// Response selector.
        /// </summary>
        public const string SELECT = "dream.out.select";

        /// <summary>
        /// Response chunk.
        /// </summary>
        public const string CHUNK = "dream.out.chunk";

        /// <summary>
        /// Response type.
        /// </summary>
        public const string TYPE = "dream.out.type";

        /// <summary>
        /// Response 'Save As' directive.
        /// </summary>
        public const string SAVEAS = "dream.out.saveas";
    }

    /// <summary>
    /// Encapsulation for Http Content-Disposition header.
    /// </summary>
    public class ContentDisposition {

        //--- Class Fields
        private static readonly Regex MIME_ENCODE_REGEX = new Regex("(Firefox|Chrome)");
        private static readonly Regex URL_ENCODE_REGEX = new Regex("(MSIE)");

        //--- Fields ---

        /// <summary>
        /// Inline content.
        /// </summary>
        public bool Inline;

        /// <summary>
        /// Content creation date.
        /// </summary>
        public DateTime? CreationDate;

        /// <summary>
        /// Content modification date.
        /// </summary>
        public DateTime? ModificationDate;

        /// <summary>
        /// Date content was read.
        /// </summary>
        public DateTime? ReadDate;

        /// <summary>
        /// Local filename for content.
        /// </summary>
        public string FileName;

        /// <summary>
        /// Content file size (if available).
        /// </summary>
        public long? Size;

        /// <summary>
        /// Content target user agent.
        /// </summary>
        public string UserAgent;

        //--- Construtors ---

        /// <summary>
        /// Create a new content disposition.
        /// </summary>
        public ContentDisposition() { }

        /// <summary>
        /// Create a new content dispisition from a Content-Disposition header string.
        /// </summary>
        /// <param name="value"></param>
        public ContentDisposition(string value) {
            Dictionary<string, string> values = HttpUtil.ParseNameValuePairs(value);
            if(values.ContainsKey("#1")) {
                string type = values["#1"];
                this.Inline = StringUtil.EqualsInvariant(type, "inline");
            }
            if(values.ContainsKey("creation-date")) {
                DateTime date;
                if(DateTimeUtil.TryParseInvariant(values["creation-date"], out date)) {
                    this.CreationDate = date.ToUniversalTime();
                }
            }
            if(values.ContainsKey("modification-date")) {
                DateTime date;
                if(DateTimeUtil.TryParseInvariant(values["modification-date"], out date)) {
                    this.ModificationDate = date.ToUniversalTime();
                }
            }
            if(values.ContainsKey("read-date")) {
                DateTime date;
                if(DateTimeUtil.TryParseInvariant(values["read-date"], out date)) {
                    this.ReadDate = date.ToUniversalTime();
                }
            }
            if(values.ContainsKey("filename")) {
                this.FileName = values["filename"];
            }
            if(values.ContainsKey("size")) {
                long size;
                if(long.TryParse(values["size"], out size)) {
                    this.Size = size;
                }
            }
        }

        /// <summary>
        /// Create a new content disposition.
        /// </summary>
        /// <param name="inline">Inline the content.</param>
        /// <param name="created">Creation date.</param>
        /// <param name="modified">Modification date.</param>
        /// <param name="read">Read date.</param>
        /// <param name="filename">Content filename.</param>
        /// <param name="size">Content size.</param>
        public ContentDisposition(bool inline, DateTime? created, DateTime? modified, DateTime? read, string filename, long? size) {
            this.Inline = inline;
            this.CreationDate = created;
            this.ModificationDate = modified;
            this.ReadDate = read;
            this.FileName = filename;
            this.Size = size;
        }

        /// <summary>
        /// Create a new content disposition.
        /// </summary>
        /// <param name="inline">Inline the content.</param>
        /// <param name="created">Creation date.</param>
        /// <param name="modified">Modification date.</param>
        /// <param name="read">Read date.</param>
        /// <param name="filename">Content filename.</param>
        /// <param name="size">File size.</param>
        /// <param name="userAgent">Target user agent.</param>
        public ContentDisposition(bool inline, DateTime? created, DateTime? modified, DateTime? read, string filename, long? size, string userAgent) {
            this.Inline = inline;
            this.CreationDate = created;
            this.ModificationDate = modified;
            this.ReadDate = read;
            this.FileName = filename;
            this.Size = size;
            this.UserAgent = userAgent;
        }

        //--- Methods ---

        /// <summary>
        /// Convert to Content-Disposition header string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            StringBuilder result = new StringBuilder();
            if(Inline) {
                result.Append("inline");
            } else {
                result.Append("attachment");
            }
            if(CreationDate != null) {
                result.Append("; creation-date=\"").Append(CreationDate.Value.ToUniversalTime().ToString("r")).Append("\"");
            }
            if(ModificationDate != null) {
                result.Append("; modification-date=\"").Append(ModificationDate.Value.ToUniversalTime().ToString("r")).Append("\"");
            }
            if(!string.IsNullOrEmpty(FileName)) {
                bool gotFilename = false;
                if(!string.IsNullOrEmpty(UserAgent)) {
                    if(URL_ENCODE_REGEX.IsMatch(UserAgent)) {
                        
                        // Filename is uri encoded to support non ascii characters.
                        // + is replaced with %20 for IE otherwise it saves names containing spaces with plusses.
                        result.Append("; filename=\"").Append(XUri.Encode(FileName).Replace("+", "%20")).Append("\"");
                        gotFilename = true;
                    } else if(MIME_ENCODE_REGEX.IsMatch(UserAgent)) {
                        result.Append("; filename=\"=?UTF-8?B?").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(FileName))).Append("?=\"");
                        gotFilename = true;
                    }
                }
                if(!gotFilename) {
                    result.Append("; filename=\"").Append(Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(FileName))).Append("\"");
                }
            }
            if(Size != null) {
                result.Append("; size=").Append(Size.Value);
            }
            return result.ToString();
        }
    }
}
