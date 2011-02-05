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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using log4net;

namespace MindTouch.IO {

    /// <summary>
    /// A set of static and extension methods to simplify common stream opreations.
    /// </summary>
    public static class StreamUtil {

        //--- Constants ---

        /// <summary>
        /// Common size for internal byte buffer used for Stream operations
        /// </summary>
        public const int BUFFER_SIZE = 16 * 1024;

        //--- Class Fields ---
        private static readonly Random Randomizer = new Random();
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Extension Methods ---

        /// <summary>
        /// Write a string to <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/></param>
        /// <param name="encoding">Encoding to use to convert the string to bytes</param>
        /// <param name="text">Regular string or composite format string to write to the <see cref="Stream"/></param>
        /// <param name="args">An System.Object array containing zero or more objects to format.</param>
        public static void Write(this Stream stream, Encoding encoding, string text, params object[] args) {
            const int bufferSize = BUFFER_SIZE / sizeof(char);
            if(text.Length > bufferSize) {

                // to avoid a allocating a byte array of greater than 64k, we chunk our string writing here
                if(args.Length != 0) {
                    text = string.Format(text, args);
                }
                var length = text.Length;
                var idx = 0;
                var buffer = new char[bufferSize];
                while(true) {
                    var size = Math.Min(bufferSize, length - idx);
                    if(size == 0) {
                        break;
                    }
                    text.CopyTo(idx, buffer, 0, size);
                    stream.Write(encoding.GetBytes(buffer, 0, size));
                    idx += size;
                }
            } else {
                if(args.Length == 0) {
                    stream.Write(encoding.GetBytes(text));
                } else {
                    stream.Write(encoding.GetBytes(string.Format(text, args)));
                }
            }
        }

        /// <summary>
        /// Write an entire buffer to a <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/></param>
        /// <param name="buffer">An array of bytes to write to the <see cref="Stream"/></param>
        public static void Write(this Stream stream, byte[] buffer) {
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Determine whether a <see cref="Stream"/> contents are in memory
        /// </summary>
        /// <param name="stream">Target <see cref="Stream"/></param>
        /// <returns><see langword="true"/> If the <see cref="Stream"/> contents are in memory</returns>
        public static bool IsStreamMemorized(this Stream stream) {
            return (stream is ChunkedMemoryStream) || (stream is MemoryStream);
        }

        /// <summary>
        /// Synchronous copying of one stream to another.
        /// </summary>
        /// <param name="source">Source <see cref="Stream"/>.</param>
        /// <param name="target">Target <see cref="Stream"/>.</param>
        /// <param name="length">Number of bytes to copy from source to target.</param>
        /// <returns>Actual bytes copied.</returns>
        public static long CopyTo(this Stream source, Stream target, long length) {
            var bufferLength = length >= 0 ? length : long.MaxValue;
            var buffer = new byte[Math.Min(bufferLength, BUFFER_SIZE)];
            long total = 0;
            while(length != 0) {
                var count = (length >= 0) ? Math.Min(length, buffer.LongLength) : buffer.LongLength;
                count = source.Read(buffer, 0, (int)count);
                if(count == 0) {
                    break;
                }
                target.Write(buffer, 0, (int)count);
                total += count;
                length -= count;
            }
            return total;
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        public static void CopyToFile(this Stream stream, string filename, long length) {
            FileStream file = null;
            try {
                using(file = File.Create(filename)) {
                    CopyTo(stream, file, length);
                }
            } catch {

                // check if we created a file, if so delete it
                if(file != null) {
                    try {
                        File.Delete(filename);
                    } catch { }
                }
                throw;
            } finally {

                // make sure the source stream is closed
                try {
                    stream.Close();
                } catch { }
            }
        }

        /// <summary>
        /// Compute the MD5 hash.
        /// </summary>
        /// <param name="stream">Stream to hash.</param>
        /// <returns>MD5 hash.</returns>
        public static byte[] ComputeHash(this Stream stream) {
            return MD5.Create().ComputeHash(stream);
        }

        /// <summary>
        /// Compute the MD5 hash string.
        /// </summary>
        /// <param name="stream">Stream to hash.</param>
        /// <returns>MD5 hash string.</returns>
        public static string ComputeHashString(this Stream stream) {
            return StringUtil.HexStringFromBytes(ComputeHash(stream));
        }

#if WARN_ON_SYNC
        [Obsolete("This method is thread-blocking.  Please avoid using it if possible.")]
#endif
        /// <summary>
        /// WARNING: This method is thread-blocking.  Please avoid using it if possible.
        /// </summary>
        public static byte[] ReadBytes(this Stream source, long length) {
            var result = new MemoryStream();
            CopyTo(source, result, length);
            return result.ToArray();
        }

        //--- Class Methods ---


        /// <summary>
        /// Try to open a file for exclusive read/write access
        /// </summary>
        /// <param name="filename">Path to file</param>
        /// <returns>A <see cref="Stream"/> for the opened file, or <see langword="null"/> on failure to open the file.</returns>
        public static Stream FileOpenExclusive(string filename) {
            for(int attempts = 0; attempts < 10; ++attempts) {
                try {
                    return File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                } catch(IOException e) {
                    _log.TraceExceptionMethodCall(e, "FileOpenExclusive", filename, attempts);
                } catch(UnauthorizedAccessException e) {
                    _log.TraceExceptionMethodCall(e, "FileOpenExclusive", filename, attempts);
                }
                Thread.Sleep((attempts + 1) * Randomizer.Next(100));
            }
            return null;
        }

        /// <summary>
        /// Create a pipe
        /// </summary>
        /// <param name="writer">The writer endpoint of the pipe</param>
        /// <param name="reader">The reader endpoint of the pipe</param>
        public static void CreatePipe(out Stream writer, out Stream reader) {
            PipeStreamBuffer buffer = new PipeStreamBuffer();
            writer = new PipeStreamWriter(buffer);
            reader = new PipeStreamReader(buffer);
        }

        /// <summary>
        /// Create a pipe
        /// </summary>
        /// <param name="size">The size of the pipe buffer</param>
        /// <param name="writer">The writer endpoint of the pipe</param>
        /// <param name="reader">The reader endpoint of the pipe</param>
        public static void CreatePipe(int size, out Stream writer, out Stream reader) {
            PipeStreamBuffer buffer = new PipeStreamBuffer(size);
            writer = new PipeStreamWriter(buffer);
            reader = new PipeStreamReader(buffer);
        }
    }
}