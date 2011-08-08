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

namespace MindTouch.Traum {

    /// <summary>
    /// Provides the common base exception for Dream specific exceptions.
    /// </summary>
    public class DreamException : Exception {

        //--- Constructors ---

        /// <summary>
        /// Create new instance.
        /// </summary>
        public DreamException() { }

        /// <summary>
        /// Create new instance with a message about the error that cause the exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DreamException(string message) : base(message) { }

        /// <summary>
        /// Create new instance with the exception and a message about the cause of this error.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public DreamException(string message, Exception innerException) : base(message, innerException) { }
    }


    /// <summary>
    /// Provides a common exception base for exceptions that should stop request processing from proceeding.
    /// </summary>
    public class DreamRequestFatalException : DreamException {

        //--- Constructors ---

        /// <summary>
        /// Create new instance.
        /// </summary>
        public DreamRequestFatalException() { }

        /// <summary>
        /// Create new instance with a message about the error that cause the exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DreamRequestFatalException(string message) : base(message) { }

        /// <summary>
        /// Create new instance with the exception and a message about the cause of this error.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public DreamRequestFatalException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Provides an exception that is thrown by blocking <see cref="Plug2"/> calls when the response <see cref="DreamMessage2"/> indicates an unsuccessful request.
    /// </summary>
    public class DreamResponseException : DreamException {

        //--- Fields ---

        /// <summary>
        /// The response that caused this exception.
        /// </summary>
        public readonly DreamMessage2 Response;

        //--- Constructors ---

        /// <summary>
        /// Create new instance for an unsuccessful response.
        /// </summary>
        /// <param name="response">Unsuccessful response message.</param>
        public DreamResponseException(DreamMessage2 response) : base(DreamMessage2.GetStatusStringOrNull(response)) {
            this.Response = response;
        }

        /// <summary>
        /// Create new instance for an unsuccessful response.
        /// </summary>
        /// <param name="response">Unsuccessful response message.</param>
        /// <param name="message">A message about why the response failed.</param>
        public DreamResponseException(DreamMessage2 response, string message) : base(message) {
            this.Response = response;
        }

        /// <summary>
        /// Create a new instance for an unsuccessful response.
        /// </summary>
        /// <param name="response">Unsuccessful response message.</param>
        /// <param name="message">A message about why the response failed.</param>
        /// <param name="innerException">The exception that caused the message to be unsuccessful.</param>
        public DreamResponseException(DreamMessage2 response, string message, Exception innerException) : base(message, innerException) {
            this.Response = response;
        }

        //--- Methods ---

        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString() {
            if(Response != null) {
                return base.ToString() + " " + DreamMessage2.GetStatusStringOrNull(Response);
            }
            return base.ToString();
        }
    }

    /// <summary>
    /// Provides an exception to be thrown when a request is aborted because of an error.
    /// </summary>
    public class DreamAbortException : DreamException {

        //--- Fields ---

        /// <summary>
        /// Message describing the reason for the aborted request.
        /// </summary>
        public readonly DreamMessage2 Response;

        //--- Constructors ---

        /// <summary>
        /// Create new instance with a message describing the reason for the aborted request.
        /// </summary>
        /// <param name="response">Message describing the reason for the aborted request.</param>
        public DreamAbortException(DreamMessage2 response) : base(DreamMessage2.GetStatusStringOrNull(response)) {
            this.Response = response;
        }

        /// <summary>
        /// Create new instance with a message describing the reason for the aborted request.
        /// </summary>
        /// <param name="response">Message describing the reason for the aborted request.</param>
        /// <param name="message">Additional text message about the problem.</param>
        public DreamAbortException(DreamMessage2 response, string message) : base(message) {
            this.Response = response;
        }

        /// <summary>
        /// Create new instance with a message describing the reason for the aborted request.
        /// </summary>
        /// <param name="response">Message describing the reason for the aborted request.</param>
        /// <param name="message">Additional text message about the problem.</param>
        /// <param name="innerException">The exception that caused the message to be unsuccessful.</param>
        public DreamAbortException(DreamMessage2 response, string message, Exception innerException) : base(message, innerException) {
            this.Response = response;
        }
    }

    /// <summary>
    /// Provides a <see cref="DreamAbortException"/> with a <see cref="DreamStatus.InternalError"/> message.
    /// </summary>
    public class DreamInternalErrorException : DreamAbortException {

        //--- Constructors ---

        /// <summary>
        /// Create a new instance for an <see cref="DreamStatus.InternalError"/> failure.
        /// </summary>
        public DreamInternalErrorException() : base(DreamMessage2.InternalError()) { }

        /// <summary>
        /// Create a new instance for an <see cref="DreamStatus.InternalError"/> failure.
        /// </summary>
        /// <param name="message">Text message to use for <see cref="Exception.Message"/> and the internal <see cref="DreamMessage2"/>.</param>
        public DreamInternalErrorException(string message) : base(DreamMessage2.InternalError(message), message) { }

        /// <summary>
        /// Create a new instance for a <see cref="DreamStatus.InternalError"/> condition.
        /// </summary>
        /// <param name="innerException">The exception that cause the internal error for the request.</param>
        public DreamInternalErrorException(Exception innerException) : base(DreamMessage2.InternalError(innerException), innerException.Message) { }
    }

    /// <summary>
    /// Provides a <see cref="DreamAbortException"/> with a <see cref="DreamStatus.BadRequest"/> message.
    /// </summary>
    public class DreamBadRequestException : DreamAbortException {

        //--- Constructors ---

        /// <summary>
        /// Create a new instance for a <see cref="DreamStatus.BadRequest"/> condition.
        /// </summary>
        /// <param name="message">Text message to use for <see cref="Exception.Message"/> and the internal <see cref="DreamMessage2"/>.</param>
        public DreamBadRequestException(string message) : base(DreamMessage2.BadRequest(message), message) { }
    }

    /// <summary>
    /// Provides a <see cref="DreamAbortException"/> with a <see cref="DreamStatus.Forbidden"/> message.
    /// </summary>
    public class DreamForbiddenException : DreamAbortException {

        //--- Constructors ---

        /// <summary>
        /// Create a new instance for a <see cref="DreamStatus.Forbidden"/> condition.
        /// </summary>
        /// <param name="message">Text message to use for <see cref="Exception.Message"/> and the internal <see cref="DreamMessage2"/>.</param>
        public DreamForbiddenException(string message) : base(DreamMessage2.Forbidden(message), message) { }
    }

    /// <summary>
    /// Provides a <see cref="DreamAbortException"/> with a <see cref="DreamStatus.NotFound"/> message.
    /// </summary>
    public class DreamNotFoundException : DreamAbortException {

        //--- Constructors ---

        /// <summary>
        /// Create a new instance for a <see cref="DreamStatus.NotFound"/> condition.
        /// </summary>
        /// <param name="message">Text message to use for <see cref="Exception.Message"/> and the internal <see cref="DreamMessage2"/>.</param>
        public DreamNotFoundException(string message) : base(DreamMessage2.NotFound(message), message) { }
    }
}
