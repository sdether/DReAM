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
using System.Text;

namespace MindTouch {

    /// <summary>
    /// Encapsulates a content mime type.
    /// </summary>
    public class MimeType {

        //--- Constants ---
        private const string PARAM_CHARSET = "charset";
        private const string PARAM_QUALITY = "q";

        //--- Class Fields ---

        // (bug 7232) changed ISO-8859-1 to US-ASCII as per RFC-2045/2046.

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance that can match any mime type.
        /// </summary>
        /// <remarks>Signature: <b>*/*</b></remarks>
        public static readonly MimeType ANY = new MimeType("*/*");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance that can match any text mime type.
        /// </summary>
        /// <remarks>Signature: <b>text/*</b></remarks>
        public static readonly MimeType ANY_TEXT = new MimeType("text/*");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for Atom content.
        /// </summary>
        /// <remarks>Signature: <b>application/atom+xml; UTF8</b></remarks>
        public static readonly MimeType ATOM = new MimeType("application/atom+xml", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for binary content.
        /// </summary>
        /// <remarks>Signature: <b>application/octet-stream</b></remarks>
        public static readonly MimeType BINARY = new MimeType("application/octet-stream");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for BMP Images.
        /// </summary>
        /// <remarks>Signature: <b>image/bmp</b></remarks>
        public static readonly MimeType BMP = new MimeType("image/bmp");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for CSS.
        /// </summary>
        /// <remarks>Signature: <b>text/css; ASCII</b></remarks>
        public static readonly MimeType CSS = new MimeType("text/css", Encoding.ASCII);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for dream exception xml.
        /// </summary>
        /// <remarks>Signature: <b>application/x-dream-exception+xml; UTF-8</b></remarks>
        public static readonly MimeType DREAM_EXCEPTION = new MimeType("application/x-dream-exception+xml", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for wwww form urlencoded content.
        /// </summary>
        /// <remarks>Signature: <b>application/x-www-form-urlencoded; UTF-8</b></remarks>
        public static readonly MimeType FORM_URLENCODED = new MimeType("application/x-www-form-urlencoded", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for GIF images.
        /// </summary>
        /// <remarks>Signature: <b>image/gif</b></remarks>
        public static readonly MimeType GIF = new MimeType("image/gif");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for Html content.
        /// </summary>
        /// <remarks>Signature: <b>text/html; UTF-8</b></remarks>
        public static readonly MimeType HTML = new MimeType("text/html", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for Http Applications.
        /// </summary>
        /// <remarks>Signature: <b>application/http; UTF-8</b></remarks>
        public static readonly MimeType HTTP = new MimeType("application/http", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for JPEG images.
        /// </summary>
        /// <remarks>Signature: <b>image/jpeg</b></remarks>
        public static readonly MimeType JPEG = new MimeType("image/jpeg");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for javascript source code.
        /// </summary>
        /// <remarks>Signature: <b>text/javascript; ASCII</b></remarks>
        public static readonly MimeType JS = new MimeType("text/javascript", Encoding.ASCII);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for javascript object notation content.
        /// </summary>
        /// <remarks>Signature: <b>application/json; UTF-8</b></remarks>
        public static readonly MimeType JSON = new MimeType("application/json", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for multipart/mixed content.
        /// </summary>
        /// <remarks>Signature: <b>multipart/mixed</b></remarks>
        public static readonly MimeType MULTIPART_MIXED = new MimeType("multipart/mixed");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance indidcating no content.
        /// </summary>
        /// <remarks>Signature: <b>-/-</b></remarks>
        public static readonly MimeType NOTHING = new MimeType("-/-");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for Adobe PDF.
        /// </summary>
        /// <remarks>Signature: <b>application/pdf</b></remarks>
        public static readonly MimeType PDF = new MimeType("application/pdf");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for PHP source code.
        /// </summary>
        /// <remarks>Signature: <b>application/php; UTF-8</b></remarks>
        public static readonly MimeType PHP = new MimeType("application/php", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for PNG images.
        /// </summary>
        /// <remarks>Signature: <b>image/png</b></remarks>
        public static readonly MimeType PNG = new MimeType("image/png");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for RDF content.
        /// </summary>
        /// <remarks>Signature: <b>application/rdf+xml; UTF-8</b></remarks>
        public static readonly MimeType RDF = new MimeType("application/rdf+xml", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for Relax-NG schema content.
        /// </summary>
        /// <remarks>Signature: <b>application/relax-ng-compact-syntax; UTF-8</b></remarks>
        public static readonly MimeType RELAXNG = new MimeType("application/relax-ng-compact-syntax", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for SVG images.
        /// </summary>
        /// <remarks>Signature: <b>image/svg+xml; UTF-8</b></remarks>
        public static readonly MimeType SVG = new MimeType("image/svg+xml", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for plain text content.
        /// </summary>
        /// <remarks>Signature: <b>text/plain; ASCII</b></remarks>
        public static readonly MimeType TEXT = new MimeType("text/plain", Encoding.ASCII);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for UTF-8 encoded text content.
        /// </summary>
        /// <remarks>Signature: <b>text/plain; UTF-8</b></remarks>
        public static readonly MimeType TEXT_UTF8 = new MimeType("text/plain", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for ASCII-encoded Xml content.
        /// </summary>
        /// <remarks>Signature: <b>text/xml; ASCII</b></remarks>
        public static readonly MimeType TEXT_XML = new MimeType("text/xml", Encoding.ASCII);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for TIFF images.
        /// </summary>
        /// <remarks>Signature: <b>image/tiff</b></remarks>
        public static readonly MimeType TIFF = new MimeType("image/tiff");

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for VCal calendar files.
        /// </summary>
        /// <remarks>Signature: <b>text/calendar; ASCII</b></remarks>
        public static readonly MimeType VCAL = new MimeType("text/calendar", Encoding.ASCII);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for VCard files.
        /// </summary>
        /// <remarks>Signature: <b>application/x-versit; UTF-8</b></remarks>
        public static readonly MimeType VERSIT = new MimeType("application/x-versit", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for XHtml content.
        /// </summary>
        /// <remarks>Signature: <b>application/xhtml+xml; UTF-8</b></remarks>
        public static readonly MimeType XHTML = new MimeType("application/xhtml+xml", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for Xml content.
        /// </summary>
        /// <remarks>Signature: <b>application/xml; UTF-8</b></remarks>
        public static readonly MimeType XML = new MimeType("application/xml", Encoding.UTF8);

        /// <summary>
        /// Creates a <see cref="MimeType"/> instance for Xrds content.
        /// </summary>
        /// <remarks>Signature: <b>application/xrds+xml; UTF-8</b></remarks>
        public static readonly MimeType XRDS = new MimeType("application/xrds+xml", Encoding.UTF8);

        // TODO (steveb): we need to make the collection read-only as well
        private static readonly Dictionary<string, string> _emptyParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        //--- Class Methods ---

        /// <summary>
        /// Create a new Mimetype instance with a manually specified content-type.
        /// </summary>
        /// <param name="contentTypeWithParameters">Content type string.</param>
        /// <returns>New mime type instance.</returns>
        public static MimeType New(string contentTypeWithParameters) {
            MimeType result;
            TryParse(contentTypeWithParameters, out result);
            return result;
        }

        /// <summary>
        /// Get the best matching Mimetype given a list of mime-type options and accepted mime-types.
        /// </summary>
        /// <param name="have">Array of possible mime-types.</param>
        /// <param name="accept">Array of mime-types accepted.</param>
        /// <returns>Best match mime-type instance.</returns>
        public static MimeType MatchBestContentType(MimeType[] have, MimeType[] accept) {

            // match each available content-type against accepted content-types
            MimeType result = null;
            float best = 0.0f;
            foreach(MimeType haveType in have) {
                foreach(MimeType acceptType in accept) {

                    // check if current combination yields a better result
                    float current = acceptType.Quality;
                    if((current > best) && haveType.Match(acceptType)) {
                        best = current;
                        result = haveType;
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// Parse an array of accepted mime-types from an 'Accept' header.
        /// </summary>
        /// <param name="accept">'Accept' header value.</param>
        /// <returns>Array of accepted mime-types.</returns>
        public static MimeType[] ParseAcceptHeader(string accept) {
            List<MimeType> result = new List<MimeType>();
            if(!string.IsNullOrEmpty(accept)) {
                foreach(string entry in accept.Split(',')) {
                    MimeType type;
                    if(TryParse(entry, out type)) {
                        result.Add(type);
                    }
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Render an 'Accept' header value from an array of mime-types.
        /// </summary>
        /// <param name="types">Array of mime-types.</param>
        /// <returns>'Accept' header value string.</returns>
        public static string RenderAcceptHeader(params MimeType[] types) {
            StringBuilder result = new StringBuilder();
            foreach(MimeType type in types) {
                if(result.Length > 0) {
                    result.Append(", ");
                }
                result.Append(type);
            }
            return result.ToString();
        }

        /// <summary>
        /// Derive a mime-type from a file extension.
        /// </summary>
        /// <param name="filename">Filename to get extension from.</param>
        /// <returns>Best match mime-type.</returns>
        public static MimeType FromFileExtension(string filename) {
            MimeType result = BINARY;
            switch(Path.GetExtension(filename).ToLowerInvariant()) {

                // document formats
                case ".html":
                case ".htm":
                    result = HTML;
                    break;
                case ".txt":
                    result = TEXT;
                    break;
                case ".xml":
                case ".xhtml":
                case ".xsl":
                case ".xsd":
                case ".xslt":
                    result = XML;
                    break;
                case ".css":
                    result = CSS;
                    break;
                case ".js":
                    result = JS;
                    break;
                case ".pdf":
                    result = PDF;
                    break;

                // image formats
                case ".gif":
                    result = GIF;
                    break;
                case ".jpg":
                case ".jpeg":
                    result = JPEG;
                    break;
                case ".png":
                    result = PNG;
                    break;
                case ".svg":
                    result = SVG;
                    break;
                case ".tif":
                case ".tiff":
                    result = TIFF;
                    break;
                case ".bmp":
                    result = BMP;
                    break;
            }
            return result;
        }

        /// <summary>
        /// Try to parse a mime-type from a content-type string.
        /// </summary>
        /// <param name="contentTypeWithParameters">Content type string.</param>
        /// <param name="result">Mime-type instance return value.</param>
        /// <returns><see langword="True"/> if a mime-type could be parsed.</returns>
        public static bool TryParse(string contentTypeWithParameters, out MimeType result) {
            string type;
            string subtype;
            Dictionary<string, string> parameters;
            if(!TryParse(contentTypeWithParameters, out type, out subtype, out parameters)) {
                result = null;
                return false;
            }
            result = new MimeType(type, subtype, parameters);
            return true;
        }

        /// <summary>
        /// Try to parse a mime-type components from a content-type string.
        /// </summary>
        /// <param name="contentTypeWithParameters">Content type string.</param>
        /// <param name="type">Main type return value.</param>
        /// <param name="subtype">Sub-type return value.</param>
        /// <param name="parameters">Dictionary of mime-type parameters.</param>
        /// <returns></returns>
        protected static bool TryParse(string contentTypeWithParameters, out string type, out string subtype, out Dictionary<string, string> parameters) {
            type = null;
            subtype = null;
            parameters = null;
            if(string.IsNullOrEmpty(contentTypeWithParameters)) {
                return false;
            }

            // parse types
            string[] parts = contentTypeWithParameters.Split(';');
            string[] typeParts = parts[0].Split(new char[] { '/' }, 2);
            if(typeParts.Length != 2) {
                return false;
            }
            type = typeParts[0].Trim().ToLowerInvariant();
            subtype = typeParts[1].Trim().ToLowerInvariant();

            // parse parameters
            for(int i = 1; i < parts.Length; ++i) {
                string[] assign = parts[i].Split(new char[] { '=' }, 2);
                if(assign.Length == 2) {
                    if(parameters == null) {
                        parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    parameters[assign[0].Trim()] = assign[1].Trim();
                }
            }
            return true;
        }

        //--- Fields ---
        private string _mainType;
        private string _subType;
        private Dictionary<string, string> _parameters;
        private string _text;

        //--- Constructors ---

        /// <summary>
        /// Create a new mime-type from an exact-match content-type string.
        /// </summary>
        /// <param name="contentTypeWithParameters">Content type string.</param>
        public MimeType(string contentTypeWithParameters) {
            if(!TryParse(contentTypeWithParameters, out _mainType, out _subType, out _parameters)) {
                throw new ArgumentNullException("contentTypeWithParameters");
            }
        }

        /// <summary>
        /// Create a new mime-type from an exact-match content-type string.
        /// </summary>
        /// <param name="contentTypeWithParameters">Content type string.</param>
        /// <param name="charset">Encoding to set for mime-type.</param>
        /// <param name="quality">Match quality.</param>
        public MimeType(string contentTypeWithParameters, Encoding charset, float quality) {
            if(!TryParse(contentTypeWithParameters, out _mainType, out _subType, out _parameters)) {
                throw new ArgumentNullException("contentTypeWithParameters");
            }
            if(_parameters == null) {
                _parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            _parameters[PARAM_CHARSET] = charset.WebName;
            _parameters[PARAM_QUALITY] = quality.ToString();
        }

        /// <summary>
        /// Create a new mime-type from an exact-match content-type string.
        /// </summary>
        /// <param name="contentTypeWithParameters">Content type string.</param>
        /// <param name="quality">Match quality.</param>
        public MimeType(string contentTypeWithParameters, float quality) {
            if(!TryParse(contentTypeWithParameters, out _mainType, out _subType, out _parameters)) {
                throw new ArgumentNullException("contentTypeWithParameters");
            }
            if(_parameters == null) {
                _parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            _parameters[PARAM_QUALITY] = quality.ToString();
        }

        /// <summary>
        /// Create a new mime-type from an exact-match content-type string.
        /// </summary>
        /// <param name="contentTypeWithParameters">Content type string.</param>
        /// <param name="charset">Encoding to set for mime-type.</param>
        public MimeType(string contentTypeWithParameters, Encoding charset) {
            if(!TryParse(contentTypeWithParameters, out _mainType, out _subType, out _parameters)) {
                throw new ArgumentNullException("contentTypeWithParameters");
            }
            if(_parameters == null) {
                _parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            _parameters[PARAM_CHARSET] = charset.WebName;
        }

        private MimeType(string type, string subtype, Dictionary<string, string> parameters) {
            _mainType = type;
            _subType = subtype;
            _parameters = parameters;
        }

        //--- Properties ---

        /// <summary>
        /// Main type.
        /// </summary>
        public string MainType { get { return _mainType; } }

        /// <summary>
        /// Sub type.
        /// </summary>
        public string SubType { get { return _subType; } }

        /// <summary>
        /// Full type (without parameters).
        /// </summary>
        public string FullType { get { return _mainType + "/" + _subType; } }

        /// <summary>
        /// Type parameters.
        /// </summary>
        public Dictionary<string, string> Parameters { get { return _parameters ?? _emptyParameters; } }

        /// <summary>
        /// Match quality value.
        /// </summary>
        public float Quality { get { return float.Parse(GetParameter(PARAM_QUALITY) ?? "1"); } }

        /// <summary>
        /// Type Encoding.
        /// </summary>
        public Encoding CharSet {
            get {
                string charset = GetParameter(PARAM_CHARSET);
                if(charset != null) {
                    return Encoding.GetEncoding(charset.Trim('"'));
                } else if(StringUtil.EqualsInvariant(MainType, "text")) {
                    return Encoding.ASCII;
                } else {
                    return Encoding.UTF8;
                }
            }
        }

        /// <summary>
        /// <see langword="True"/> if content represented by the mime-type is Xml.
        /// </summary>
        public bool IsXml {
            get {
                return StringUtil.EqualsInvariant(SubType, "xml") || StringUtil.EndsWithInvariant(SubType, "+xml");
            }
        }

        //--- Methods ---

        /// <summary>
        /// Try to match instance against another instance.
        /// </summary>
        /// <param name="other">Other mime-type.</param>
        /// <returns><see langword="True"/> if the two types match.</returns>
        public bool Match(MimeType other) {

            // match main type
            if((MainType != "*") && (other.MainType != "*") && (MainType != other.MainType)) {
                return false;
            }

            // match subtype
            if((SubType != "*") && (other.SubType != "*") && (SubType != other.SubType)) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get a named parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns>Parameter value or null.</returns>
        public string GetParameter(string name) {
            string result;
            Parameters.TryGetValue(name, out result);
            return result;
        }

        /// <summary>
        /// Render mime-type as a string representation including parameters.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            if(_text == null) {
                StringBuilder result = new StringBuilder();
                result.Append(FullType);
                foreach(KeyValuePair<string, string> param in Parameters) {
                    result.AppendFormat("; {0}={1}", param.Key, param.Value);
                }
                _text = result.ToString();
            }
            return _text;
        }
    }
}
