using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MindTouch.Traum.Webclient {
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
                this.Inline = type.EqualsInvariant("inline");
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
            var result = new StringBuilder();
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