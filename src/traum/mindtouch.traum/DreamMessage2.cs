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
using System.Text;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Web;

namespace MindTouch.Traum {

    /// <summary>
    /// Provides the Dream encapsulations of Http request and response objects.
    /// </summary>
    public class DreamMessage2 {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        /// <summary>
        /// New Message with HTTP status: Ok (200).
        /// </summary>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 Ok() {
            return new DreamMessage2(DreamStatus.Ok);
        }

        /// <summary>
        /// New Message with HTTP status: Ok (200).
        /// </summary>
        /// <param name="contentType">Content Mime-Type.</param>
        /// <param name="text">Message body.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 Ok(MimeType contentType, string text) {
            return new DreamMessage2(DreamStatus.Ok, null, contentType, text);
        }

        /// <summary>
        /// New Message with HTTP status: Ok (200).
        /// </summary>
        /// <param name="values">Name/value pair body.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 Ok(KeyValuePair<string, string>[] values) {
            return new DreamMessage2(DreamStatus.Ok, null, MimeType.FORM_URLENCODED, XUri.RenderParams(values) ?? string.Empty);
        }

        /// <summary>
        /// New Message with HTTP status: Ok (200).
        /// </summary>
        /// <param name="contentType">Content Mime-Type.</param>
        /// <param name="content">Message body.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 Ok(MimeType contentType, byte[] content) {
            return new DreamMessage2(DreamStatus.Ok, null, contentType, content);
        }

        /// <summary>
        /// New Message with HTTP status: Ok (200).
        /// </summary>
        /// <param name="contentType">Content Mime-Type.</param>
        /// <param name="contentLength">Content length.</param>
        /// <param name="content">Message body.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 Ok(MimeType contentType, long contentLength, Stream content) {
            return new DreamMessage2(DreamStatus.Ok, null, contentType, contentLength, content);
        }

        /// <summary>
        /// New Message with HTTP status: Not Modified (304).
        /// </summary>
        /// <returns>New DreamMessage.</returns>
        public static DreamMessage2 NotModified() {
            return new DreamMessage2(DreamStatus.NotModified);
        }

        /// <summary>
        /// New Message with HTTP status: Not Found (404).
        /// </summary>
        /// <param name="reason">Reason.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 NotFound(string reason) {
            _log.DebugFormat("Response: Not Found - {0}", reason);
            return new DreamMessage2(DreamStatus.NotFound, null, MimeType.TEXT, reason);
        }

        /// <summary>
        /// New Message with HTTP status: Bad Request (400).
        /// </summary>
        /// <param name="reason">Reason.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 BadRequest(string reason) {
            _log.DebugFormat("Response: Bad Request - {0}", reason);
            return new DreamMessage2(DreamStatus.BadRequest, null, MimeType.TEXT, reason);
        }

        /// <summary>
        /// New Message with HTTP status: Not Implemented (501).
        /// </summary>
        /// <param name="reason">Reason.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 NotImplemented(string reason) {
            _log.DebugFormat("Response: Not Implemented - {0}", reason);
            return new DreamMessage2(DreamStatus.NotImplemented, null, MimeType.TEXT, reason);
        }

        /// <summary>
        /// New Message with HTTP status: Conflict (409).
        /// </summary>
        /// <param name="reason">Reason.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 Conflict(string reason) {
            _log.DebugFormat("Response: Conflict - {0}", reason);
            return new DreamMessage2(DreamStatus.Conflict, null, MimeType.TEXT, reason);
        }

        /// <summary>
        /// New Message with HTTP status: Found (302)
        /// </summary>
        /// <param name="uri">Redirect target.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 Redirect(XUri uri) {
            var result = new DreamMessage2(DreamStatus.Found);
            result.Headers.Location = uri;
            return result;
        }

        /// <summary>
        /// New Message with HTTP status: Unauthorized (401)
        /// </summary>
        /// <param name="accessRealm">Access Realm.</param>
        /// <param name="reason">Reason.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 AccessDenied(string accessRealm, string reason) {
            _log.DebugFormat("Response: Unauthorized - {0}", reason);
            var result = new DreamMessage2(DreamStatus.Unauthorized, null, MimeType.TEXT, reason);
            result.Headers.Authenticate = string.Format("Basic realm=\"{0}\"", accessRealm);
            return result;
        }

        /// <summary>
        /// New Message with HTTP status: LicenseRequired (402)
        /// </summary>
        /// <param name="reason">Reason.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 LicenseRequired(string reason) {
            _log.DebugFormat("Response: LicenseRequired - {0}", reason);
            return new DreamMessage2(DreamStatus.LicenseRequired, null, MimeType.TEXT, reason);
        }

        /// <summary>
        /// New Message with HTTP status: Forbidden (403)
        /// </summary>
        /// <param name="reason">Reason.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 Forbidden(string reason) {
            _log.DebugFormat("Response: Forbidden - {0}", reason);
            return new DreamMessage2(DreamStatus.Forbidden, null, MimeType.TEXT, reason);
        }

        /// <summary>
        /// New Message with HTTP status: Method Not Allowed (405)
        /// </summary>
        /// <param name="allowedMethods">Array of allowed request Verbs.</param>
        /// <param name="reason">Reason.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 MethodNotAllowed(string[] allowedMethods, string reason) {
            _log.DebugFormat("Response: MethodNotAllowed - {0}", reason);
            var result = new DreamMessage2(DreamStatus.MethodNotAllowed, null, MimeType.TEXT, reason);
            result.Headers.Allow = string.Join(",", allowedMethods);
            return result;
        }

        /// <summary>
        /// New Message with HTTP status: Internal Error (500)
        /// </summary>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 InternalError() {
            _log.DebugMethodCall("Response: Internal Error");
            return new DreamMessage2(DreamStatus.InternalError);
        }

        /// <summary>
        /// New Message with HTTP status: Internal Error (500)
        /// </summary>
        /// <param name="text">Error message.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 InternalError(string reason) {
            _log.DebugMethodCall("Response: Internal Error", reason);
            return new DreamMessage2(DreamStatus.InternalError, null, MimeType.TEXT, reason);
        }

        /// <summary>
        /// New Message with HTTP status: Internal Error (500)
        /// </summary>
        /// <param name="exception">Error.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 InternalError(Exception exception) {
            // TODO (arnec): need a better story for exceptions in messages
            _log.DebugMethodCall("Response: Internal Error", exception.Message);
            return new DreamMessage2(DreamStatus.InternalError, exception);
        }

        /// <summary>
        /// New Message with HTTP status: Request Failed (11)
        /// </summary>
        /// <param name="exception">Error.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 RequestFailed(Exception exception) {
            _log.DebugMethodCall("Response: Request Failed", exception.Message);
            return new DreamMessage2(DreamStatus.RequestFailed, exception);
        }

        /// <summary>
        /// New Message with HTTP status: Internal Error (500)
        /// </summary>
        /// <param name="e">Exception responsible for internal error.</param>
        /// <returns>New DreamMessage2.</returns>
        //public static DreamMessage2 InternalError(Exception e) {
        //    _log.DebugExceptionMethodCall(e, "Response: Internal Error");
        //    return new DreamMessage2(DreamStatus.InternalError, null, MimeType.DREAM_EXCEPTION, (e != null) ? new XException2(e) : XDoc.Empty);
        //}

        /// <summary>
        /// Create a message from a file.
        /// </summary>
        /// <param name="filename">Path to file.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 FromFile(string filename) {
            return FromFile(filename, false);
        }

        /// <summary>
        /// Create a message from a file.
        /// </summary>
        /// <param name="filename">Path to file.</param>
        /// <param name="omitFileContents">If <see langword="True"/> the contents of the file are omitted.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 FromFile(string filename, bool omitFileContents) {
            return FromFile(filename, null, null, omitFileContents);
        }

        /// <summary>
        /// Create a message from a file.
        /// </summary>
        /// <param name="filename">Path to file.</param>
        /// <param name="contentType">Mime-Type of message.</param>
        /// <param name="displayName">File name to emit.</param>
        /// <param name="omitFileContents">If <see langword="True"/> the contents of the file are omitted.</param>
        /// <returns>New DreamMessage2.</returns>
        public static DreamMessage2 FromFile(string filename, MimeType contentType, string displayName, bool omitFileContents) {
            if(contentType == null) {
                contentType = MimeType.FromFileExtension(filename);
            }
            DreamMessage2 result;
            if(omitFileContents) {
                result = new DreamMessage2(DreamStatus.Ok, null, contentType, new FileInfo(filename).Length, Stream.Null);
            } else {
                FileStream stream = File.OpenRead(filename);
                result = new DreamMessage2(DreamStatus.Ok, null, contentType, stream.Length, stream);
            }
            if((displayName != null) && !StringUtil.EqualsInvariantIgnoreCase(Path.GetFileName(filename), displayName)) {
                result.Headers.ContentDisposition = new ContentDisposition(true, File.GetLastWriteTimeUtc(filename), null, null, displayName, result.ContentLength);
            }
            return result;
        }

        /// <summary>
        /// Get a status string from a DreamMessage2 or null, or null, if the message is null.
        /// </summary>
        /// <param name="message">A DreamMessage2 instance or null.</param>
        /// <returns>The <see cref="Status"/> as an information string message if a non-null message was provide, or null otherwise.</returns>
        public static string GetStatusStringOrNull(DreamMessage2 message) {
            if(message != null) {
                return string.Format("HTTP Status: {0}({1})", message.Status, (int)message.Status);
            }
            return null;
        }

        //--- Fields ---

        /// <summary>
        /// Http Status of message.
        /// </summary>
        public readonly DreamStatus Status;

        /// <summary>
        /// Message Http header collection.
        /// </summary>
        public readonly DreamHeaders Headers;

        private byte[] _bytes;
        private Stream _stream;
        private bool _streamOpen;
        //--- Constructors ---

        /// <summary>
        /// Create a new message.
        /// </summary>
        /// <param name="status">Http status.</param>
        /// <param name="headers">Header collection.</param>
        public DreamMessage2(DreamStatus status) {
            this.Status = status;
            this.Headers = new DreamHeaders();
            _bytes = new byte[0];
        }

        /// <summary>
        /// Create a new message.
        /// </summary>
        /// <param name="status">Http status.</param>
        /// <param name="headers">Header collection.</param>
        public DreamMessage2(DreamStatus status, DreamHeaders headers) {
            this.Status = status;
            this.Headers = new DreamHeaders(headers);
            _bytes = new byte[0];
        }

        /// <summary>
        /// Create a new message.
        /// </summary>
        /// <param name="status">Http status.</param>
        /// <param name="exception">Error.</param>
        public DreamMessage2(DreamStatus status, Exception exception) {
            this.Status = status;
            this.Headers = new DreamHeaders();
            _bytes = new byte[0];
        }

        /// <summary>
        /// Create a new message.
        /// </summary>
        /// <param name="status">Http status.</param>
        /// <param name="headers">Header collection.</param>
        /// <param name="contentType">Content Mime-Type</param>
        /// <param name="contentLength">Content byte langth</param>
        /// <param name="stream">Stream to uas as the source for the message's content.</param>
        public DreamMessage2(DreamStatus status, DreamHeaders headers, MimeType contentType, long contentLength, Stream stream) {
            this.Status = status;
            this.Headers = new DreamHeaders(headers);
            if(contentLength != -1) {
                this.Headers.ContentLength = contentLength;
            }
            this.Headers.ContentType = contentType ?? MimeType.BINARY;

            // set stream
            _stream = stream ?? Stream.Null;
            _streamOpen = _stream is MemoryStream;
        }

        /// <summary>
        /// Create a new message.
        /// </summary>
        /// <param name="status">Http status.</param>
        /// <param name="headers">Header collection.</param>
        /// <param name="contentType">Content Mime-Type.</param>
        /// <param name="bytes">Message body.</param>
        public DreamMessage2(DreamStatus status, DreamHeaders headers, MimeType contentType, byte[] bytes) {
            if(bytes == null) {
                throw new ArgumentNullException("bytes");
            }
            this.Status = status;
            this.Headers = new DreamHeaders(headers);
            this.Headers.ContentLength = bytes.LongLength;
            this.Headers.ContentType = contentType ?? MimeType.BINARY;

            // set bytes
            _bytes = bytes;
        }

        /// <summary>
        /// Create a new message.
        /// </summary>
        /// <param name="status">Http status.</param>
        /// <param name="headers">Header collection.</param>
        /// <param name="contentType">Content Mime-Type.</param>
        /// <param name="text">Message body.</param>
        public DreamMessage2(DreamStatus status, DreamHeaders headers, MimeType contentType, string text)
            : this(status, headers, contentType, contentType.CharSet.GetBytes(text)) { }

        //--- Properties ---

        /// <summary>
        /// <see langword="True"/> if the Status indicates a successful response.
        /// </summary>
        /// <remarks>Requests are always marked as successful. Only responses use the status to convey information.</remarks>
        public bool IsSuccessful { get { return (Status >= DreamStatus.Ok) && (Status < DreamStatus.MultipleChoices); } }

        /// <summary>
        /// Message Content Mime-Type.
        /// </summary>
        public MimeType ContentType { get { return Headers.ContentType ?? MimeType.BINARY; } }

        /// <summary>
        /// Message contains cookies.
        /// </summary>
        public bool HasCookies { get { return Headers.HasCookies; } }

        /// <summary>
        /// Cookies.
        /// </summary>
        public List<DreamCookie> Cookies { get { return Headers.Cookies; } }

        /// <summary>
        /// Content Disposition Header.
        /// </summary>
        public ContentDisposition ContentDisposition { get { return Headers.ContentDisposition; } }

        /// <summary>
        /// <see langword="True"/> if the underlying content stream is closed.
        /// </summary>
        public bool IsClosed { get { return (_stream == null) && (_bytes == null); } }

        /// <summary>
        /// Total number of bytes in message.
        /// </summary>
        public long ContentLength {
            get {
                long? result = Headers.ContentLength;
                if(result != null) {
                    return result.Value;
                }
                if(IsClosed) {
                    return 0;
                } else if(_bytes != null) {
                    return _bytes.LongLength;
                } else if(_stream.IsStreamMemorized()) {
                    return _stream.Length;
                }
                return -1;
            }
        }

        /// <summary>
        /// Can this message be clone?
        /// </summary>
        /// <remarks>In general only false for closed messages and messages with non-memorized streams.</remarks>
        public bool IsCloneable {
            get {
                return !IsClosed && (_stream == null || _stream == Stream.Null || _stream.IsStreamMemorized());
            }
        }

        //--- Methods ---

        /// <summary>
        /// Get the message body as a Stream.
        /// </summary>
        /// <returns>Content Stream.</returns>
        public Stream ToStream() {
            MakeStream();
            return _stream;
        }

        /// <summary>
        /// Convert the message body into a byte array.
        /// </summary>
        /// <remarks>This method is potentially thread-blocking. Please avoid using it if possible.</remarks>
        /// <returns>Array of bytes.</returns>
        public byte[] ToBytes() {
            MakeBytes();
            return _bytes;
        }

        /// <summary>
        /// Convert the message body to plain text.
        /// </summary>
        /// <returns>Content text.</returns>
        public string ToText() {
            return ContentType.CharSet.GetString(ToBytes());
        }

        /// <summary>
        /// Convert the message body to a text reader.
        /// </summary>
        /// <returns>New text reader instance.</returns>
        public TextReader ToTextReader() {
            return new StreamReader(ToStream(), ContentType.CharSet);
        }

        /// <summary>
        /// Set Caching headers.
        /// </summary>
        /// <param name="timestamp">Last modified timestamp.</param>
        public void SetCacheMustRevalidate(DateTime timestamp) {
            Headers.CacheControl = "must-revalidate,private";
            Headers.Vary = "Accept-Encoding";
            Headers.LastModified = timestamp;
            Headers.ETag = timestamp.ToUniversalTime().ToString("r");
        }

        /// <summary>
        /// Check if the cache needs ot be re-validated
        /// </summary>
        /// <param name="timestamp">Last modified timestamp.</param>
        /// <returns><see langword="True"/> if the cache needs to be re-validated.</returns>
        public bool CheckCacheRevalidation(DateTime timestamp) {
            DateTime rounded = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Kind);

            // check if an 'If-Modified-Since' header is present
            DateTime ifModSince = Headers.IfModifiedSince ?? DateTime.MinValue;
            if(rounded <= ifModSince) {
                return true;
            }

            // check if an 'ETag' header is present
            string ifNoneMatch = Headers.IfNoneMatch;
            if(!string.IsNullOrEmpty(ifNoneMatch)) {
                if(timestamp.ToUniversalTime().ToString("r") == ifNoneMatch) {
                    return true;
                }
            }

            // either there was not validation check or the cached copy is out-of-date
            return false;
        }

        /// <summary>
        /// Clone the current message.
        /// </summary>
        /// <returns>A new message instance.</returns>
        public DreamMessage2 Clone() {
            byte[] bytes = ToBytes();
            var result = new DreamMessage2(Status, Headers, ContentType, bytes);

            // length may differ for HEAD requests
            if(bytes.LongLength != ContentLength) {
                result.Headers.ContentLength = bytes.LongLength;
            }
            if(HasCookies) {
                result.Cookies.AddRange(Cookies);
            }
            return result;
        }

        /// <summary>
        /// Close any underlying stream on the message.
        /// </summary>
        public void Close() {
            if(_stream != null) {
                _stream.Close();
                _streamOpen = false;
            }
            _stream = null;
            _bytes = null;
        }

        /// <summary>
        /// Memorize the content stream.
        /// </summary>
        /// <param name="timeout">The synchronization handle to return.</param>
        /// <returns>Synchronization handle for memorization completion.</returns>
        public Task<DreamMessage2> Memorize(TimeSpan timeout) {
            return Memorize(-1, timeout);
        }

        /// <summary>
        /// Memorize the content stream.
        /// </summary>
        /// <param name="max">Maximum number of bytes to memorize.</param>
        /// <param name="timeout">Async timeout.</param>
        /// <returns>Synchronization handle for memorization completion.</returns>
        public Task<DreamMessage2> Memorize(int max, TimeSpan timeout) {

            // check if we need to call Memorize_Helper()
            if((_stream == null) || _stream.IsStreamMemorized()) {

                // message already contains a document or byte array or a memory stream
                // we don't need to memorize those
                return this.AsCompletedTask();
            }
            if(max < 0) {
                max = int.MaxValue - 1;
            }

            // check if we already know that the stream will not fit
            var length = (int)ContentLength;
            if(length > max) {

                // mark stream as closed
                _stream.Close();
                _stream = null;
                _streamOpen = false;

                // throw size exceeded exception
                return new InternalBufferOverflowException("message body exceeded max size").AsFaultedTask<DreamMessage2>();
            }
            if(length < 0) {
                length = int.MaxValue;
            }

            // NOTE: the content-length and body length may differ (e.g. HEAD verb)

            // copy contents asynchronously
            var completion = new TaskCompletionSource<DreamMessage2>();
            StreamMemorizer.Memorize(_stream, Math.Min(length, max + 1))
                .ContinueWith(t => {
                    if(t.Result.Length > max) {
                        completion.SetException(new InternalBufferOverflowException("message body exceeded max size"));
                        return;
                    }
                    _stream = t.Result;
                    _stream.Close();
                    _stream = null;
                    _streamOpen = false;
                    completion.SetResult(null);
                });
            return completion.Task;
        }

        private void MakeStream() {
            if(IsClosed) {
                throw new InvalidOperationException("message has already been closed");
            }
            if(_stream == null) {
                _stream = new MemoryStream(_bytes, 0, _bytes.Length, true, true);
                _streamOpen = false;

                // NOTE: the content-length and body length may differ (e.g. HEAD verb)

                // update content-length if it isn't set yet
                if(Headers.ContentLength == null) {
                    Headers.ContentLength = _stream.Length;
                }
            }
        }

        private void MakeBytes() {
            if(IsClosed) {
                throw new InvalidOperationException("message has already been closed");
            }
            if(_bytes == null) {
                if(_stream is MemoryStream) {
                    _bytes = ((MemoryStream)_stream).ToArray();
                    _stream = null;
                    _streamOpen = false;
                } else {

                    // NOTE: the content-length and body length may differ (e.g. HEAD verb)

                    try {
                        var buffer = new MemoryStream();
                        _stream.CopyTo(buffer, (int)ContentLength);
                        _bytes = buffer.ToArray();
                    } finally {
                        _stream.Close();
                        _stream = null;
                        _streamOpen = false;
                    }
                }
            }
        }
    }
}