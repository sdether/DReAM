/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2010 MindTouch, Inc.
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
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.AmazonS3 {
    /// <summary>
    /// Amazon S3 Client configuration
    /// </summary>
    public class AmazonS3ClientConfig {

        //--- Class Fields ---
        private static readonly TimeSpan DEFAULT_TIMEOUT = 30.Seconds();

        //--- Constructors ---
        
        /// <summary>
        /// Create a new configuration instance
        /// </summary>
        public AmazonS3ClientConfig() {
            WithEndpoint(AmazonS3Endpoint.Default);
            Timeout = DEFAULT_TIMEOUT;
            RootPath = "/";
            Delimiter = "/";
        }

        //--- Properties ---

        /// <summary>
        /// Base uri for Amazon (usually set via <see cref="WithEndpoint"/>, default: http://s3.amazonaws.com).
        /// </summary>
        public XUri S3BaseUri { get; set; }

        /// <summary>
        /// Private Key.
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Public Key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Amazon S3 bucket.
        /// </summary>
        public string Bucket { get; set; }

        /// <summary>
        /// Root Path inside Bucket (can be null, default: "/").
        /// </summary>
        public string RootPath { get; set; }

        /// <summary>
        /// Path delimiter (default: "/").
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// Client call timeout.
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Optional Location constraint (usually set via <see cref="WithEndpoint"/>, default: null).
        /// </summary>
        public string LocationConstraint { get; set; }

        //--- Methods ---
        public void WithEndpoint(AmazonS3Endpoint endpoint) {
            S3BaseUri = endpoint.Uri;
            LocationConstraint = endpoint.LocationConstraint;
        }
    }
}