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

namespace MindTouch.Dream.Web.Server {

    /// <summary>
    /// Provides an exception thrown by <see cref="DreamContext"/> when an invalid access is made by <see cref="DreamContext.Current"/> or <see cref="DreamContext.AttachToCurrentTaskEnv"/>.
    /// </summary>
    public class DreamContextAccessException : DreamRequestFatalException {

        //--- Constructors ---

        /// <summary>
        /// Create new instance.
        /// </summary>
        public DreamContextAccessException() { }

        /// <summary>
        /// Create new instance with a message about the error that cause the exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DreamContextAccessException(string message) : base(message) { }

        /// <summary>
        /// Create new instance with the exception and a message about the cause of this error.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public DreamContextAccessException(string message, Exception innerException) : base(message, innerException) { }
    }
}
