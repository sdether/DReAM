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
using System.Diagnostics;
using System.IO;
using System.Threading;
using log4net;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class PlugTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        [TearDown]
        public void Teardown() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void With_and_without_cookiejar() {
            DreamCookie global = new DreamCookie("test", "global", new XUri("http://baz.com/foo"));
            List<DreamCookie> globalCollection = new List<DreamCookie>();
            globalCollection.Add(global);
            Plug.GlobalCookies.Update(globalCollection, null);
            DreamCookie local = new DreamCookie("test", "local", new XUri("http://baz.com/foo"));
            List<DreamCookie> localCollection = new List<DreamCookie>();
            localCollection.Add(local);
            DreamCookieJar localJar = new DreamCookieJar();
            localJar.Update(localCollection, null);
            Plug globalPlug = Plug.New("http://baz.com/foo/bar");
            Plug localPlug = globalPlug.WithCookieJar(localJar);
            Plug globalPlug2 = localPlug.WithoutCookieJar();
            Assert.AreEqual("global", globalPlug.CookieJar.Fetch(globalPlug.Uri)[0].Value);
            Assert.AreEqual("local", localPlug.CookieJar.Fetch(localPlug.Uri)[0].Value);
            Assert.AreEqual("global", globalPlug2.CookieJar.Fetch(globalPlug2.Uri)[0].Value);
        }

        [Test]
        public void Get_to_exthttp_hits_dream_over_wire() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                    if(string.IsNullOrEmpty(request.Headers.DreamPublicUri)) {
                        throw new DreamBadRequestException(string.Format("got origin header '{0}', indicating we didn't arrive via local://", request.Headers.DreamOrigin));
                    }
                    response.Return(DreamMessage.Ok());
                };
                var r = Plug.New(mock.AtLocalHost.Uri.WithScheme("ext-http")).GetAsync().Wait();
                Assert.IsTrue(r.IsSuccessful, r.HasDocument ? r.ToDocument()["message"].AsText : "request failed: " + r.Status);
            }
        }

        [Test]
        public void Get_to_http_hits_dream_via_local_pathway() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                    if(!string.IsNullOrEmpty(request.Headers.DreamPublicUri)) {
                        throw new DreamBadRequestException(string.Format("got origin header '{0}', indicating we didn't arrive via local://", request.Headers.DreamOrigin));
                    }
                    response.Return(DreamMessage.Ok());
                };
                var r = mock.AtLocalHost.GetAsync().Wait();
                Assert.IsTrue(r.IsSuccessful, r.HasDocument ? r.ToDocument()["message"].AsText : "request failed: " + r.Status);
            }
        }

        [Ignore("need to run by hand.. test is too slow for regular execution")]
        [Test]
        public void Upload_a_bunch_of_large_files_via_local___SLOW_TEST() {
            Upload_Files(true);
        }

        [Ignore("need to run by hand.. test is too slow for regular execution")]
        [Test]
        public void Upload_a_bunch_of_large_files_via_http___SLOW_TEST() {
            Upload_Files(false);
        }

        [Test]
        public void Plug_uses_own_timeout_to_govern_request_and_results_in_RequestConnectionTimeout() {
            MockPlug.Register(new XUri("mock://mock"), (plug, verb, uri, request, response) => {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                response.Return(DreamMessage.Ok());
            });
            var stopwatch = Stopwatch.StartNew();
            var r = Plug.New(MockPlug.DefaultUri)
                .WithTimeout(TimeSpan.FromSeconds(1))
                .InvokeEx(Verb.GET, DreamMessage.Ok(), new Result<DreamMessage>(TimeSpan.MaxValue)).Block();
            stopwatch.Stop();
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 2);
            Assert.IsFalse(r.HasTimedOut);
            Assert.IsFalse(r.HasException);
            Assert.AreEqual(DreamStatus.RequestConnectionTimeout, r.Value.Status);
        }

        [Test]
        public void Result_timeout_superceeds_plug_timeout_and_results_in_RequestConnectionTimeout() {
            MockPlug.Register(new XUri("mock://mock"), (plug, verb, uri, request, response) => {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                response.Return(DreamMessage.Ok());
            });
            var stopwatch = Stopwatch.StartNew();
            var r = Plug.New(MockPlug.DefaultUri)
                .WithTimeout(TimeSpan.FromSeconds(20))
                .InvokeEx(Verb.GET, DreamMessage.Ok(), new Result<DreamMessage>(1.Seconds())).Block();
            stopwatch.Stop();
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 2);
            Assert.IsFalse(r.HasTimedOut);
            Assert.IsFalse(r.HasException);
            Assert.AreEqual(DreamStatus.RequestConnectionTimeout, r.Value.Status);
        }

        [Test]
        public void Plug_timeout_is_not_used_for_message_memorization() {
            var blockingStream = new MockBlockingStream();
            MockPlug.Register(new XUri("mock://mock"), (plug, verb, uri, request, response) => {
                _log.Debug("returning blocking stream");
                response.Return(new DreamMessage(DreamStatus.Ok, null, MimeType.TEXT, -1, blockingStream));
            });
            var stopwatch = Stopwatch.StartNew();
            var msg = Plug.New(MockPlug.DefaultUri)
                .WithTimeout(TimeSpan.FromSeconds(1))
                .InvokeEx(Verb.GET, DreamMessage.Ok(), new Result<DreamMessage>()).Wait();
            stopwatch.Stop();
            _log.Debug("completed request");
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 1);
            stopwatch = Stopwatch.StartNew();
            _log.Debug("memorizing request");
            var r = msg.Memorize(new Result(1.Seconds())).Block();
            stopwatch.Stop();
            blockingStream.Unblock();
            _log.Debug("completed request memorization");
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 2);
            Assert.IsTrue(r.HasTimedOut);
        }

        [Test]
        public void Result_timeout_is_used_for_message_memorization_and_results_in_ResponseDataTransferTimeout() {
            var blockingStream = new MockBlockingStream();
            MockPlug.Register(new XUri("mock://mock"), (plug, verb, uri, request, response) => {
                _log.Debug("returning blocking stream");
                response.Return(new DreamMessage(DreamStatus.Ok, null, MimeType.TEXT, -1, blockingStream));
            });
            var stopwatch = Stopwatch.StartNew();
            _log.Debug("calling plug");
            var r = Plug.New(MockPlug.DefaultUri)
                .WithTimeout(1.Seconds())
                .Get(new Result<DreamMessage>(3.Seconds())).Block();
            _log.Debug("plug done");
            stopwatch.Stop();
            blockingStream.Unblock();
            Assert.GreaterOrEqual(stopwatch.Elapsed.Seconds, 3);
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 4);
            Assert.IsFalse(r.HasTimedOut);
            Assert.IsFalse(r.HasException);
            Assert.AreEqual(DreamStatus.ResponseDataTransferTimeout, r.Value.Status);
        }


        [Test]
        public void Plug_timeout_on_request_returns_RequestConnectionTimeout_not_ResponseDataTransferTimeout() {
            var blockingStream = new MockBlockingStream();
            MockPlug.Register(new XUri("mock://mock"), (plug, verb, uri, request, response) => {
                _log.Debug("blocking request");
                Thread.Sleep(5.Seconds());
                _log.Debug("returning blocking stream");
                response.Return(new DreamMessage(DreamStatus.Ok, null, MimeType.TEXT, -1, blockingStream));
            });
            var stopwatch = Stopwatch.StartNew();
            _log.Debug("calling plug");
            var r = Plug.New(MockPlug.DefaultUri)
                .WithTimeout(1.Seconds())
                .Get(new Result<DreamMessage>(5.Seconds())).Block();
            _log.Debug("plug done");
            stopwatch.Stop();
            blockingStream.Unblock();
            Assert.GreaterOrEqual(stopwatch.Elapsed.Seconds, 1);
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 3);
            Assert.IsFalse(r.HasTimedOut);
            Assert.IsFalse(r.HasException);
            Assert.AreEqual(DreamStatus.RequestConnectionTimeout, r.Value.Status);
        }

        [Test]
        public void Can_use_Plug_extension_to_return_document_result() {
            var autoMockPlug = MockPlug.Register(new XUri("mock://mock"));
            autoMockPlug.Expect().Verb("GET").Response(DreamMessage.Ok(new XDoc("works")));
            Assert.AreEqual("works", Plug.New("mock://mock").Get(new Result<XDoc>()).Wait().Name);
        }

        [Test]
        public void Plug_extension_to_return_document_sets_exception_on_non_OK_response() {
            var autoMockPlug = MockPlug.Register(new XUri("mock://mock"));
            autoMockPlug.Expect().Verb("GET").Response(DreamMessage.BadRequest("bad puppy"));
            var r = Plug.New("mock://mock").Get(new Result<XDoc>()).Block();
            Assert.IsTrue(r.HasException);
            Assert.AreEqual(typeof(DreamResponseException), r.Exception.GetType());
            Assert.AreEqual(DreamStatus.BadRequest, ((DreamResponseException)r.Exception).Response.Status);
        }

        [Ignore("don't have an embedded resource to run the test against")]
        [Test]
        public void ResourceUri() {
            Plug p = Plug.New("resource://mindtouch.dream/MindTouch.Dream.dtds.xhtml1-strict.dtd");
            string text = p.Get().ToText();
            Assert.AreNotEqual(string.Empty, text, "resource was empty");
        }

        private void Upload_Files(bool local) {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallbackAsync = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                    return Coroutine.Invoke(Upload_Helper, context, request, response);
                };
                var mockPlug = local
                                   ? mock.AtLocalHost
                                   : Plug.New(mock.AtLocalHost.Uri.WithScheme("ext-http")).WithTimeout(TimeSpan.FromMinutes(10));
                for(var i = 0; i <= 4; i++) {
                    Stream stream = new MockAsyncReadableStream(150 * 1024 * 1024);
                    _log.DebugFormat("uploading {0}", i);
                    var response = mockPlug.PutAsync(new DreamMessage(DreamStatus.Ok, null, MimeType.BINARY, stream.Length, stream)).Wait();
                    if(!response.IsSuccessful) {
                        _log.DebugFormat("upload failed");
                        Assert.Fail(string.Format("unable to upload: {0}", response.ToText()));
                    }
                }
            }
        }

        private Yield Upload_Helper(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            using(var stream = request.ToStream()) {
                var total = 0;
                var buffer = new byte[1024 * 1024];
                while(total < request.ContentLength) {
                    Result<int> read;
                    yield return read = stream.Read(buffer, 0, buffer.Length, new Result<int>());
                    //int read = stream.Read(buffer, 0, buffer.Length);
                    if(read.Value == 0) {
                        break;
                    }
                    total += read.Value;
                    //fake some latency
                    yield return Async.Sleep(TimeSpan.FromMilliseconds(1));
                }
                _log.DebugFormat("read {0}/{1} bytes", total, request.ContentLength);
                if(total != request.ContentLength) {
                    throw new DreamBadRequestException(string.Format("was supposed to read {0} bytes, only read {1}", request.ContentLength, total));
                }
            }
            response.Return(DreamMessage.Ok());
        }
    }

    public class MockBlockingStream : Stream {


        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        private readonly ManualResetEvent _blockEvent = new ManualResetEvent(false);
        private int _readCount;
        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) { }

        public override int Read(byte[] buffer, int offset, int count) {
            _readCount++;
            if(_readCount > 1) {
                return 0;
            }
            _log.DebugFormat("blocking read {0}", _readCount);
            _blockEvent.WaitOne();
            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count) { }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { throw new NotImplementedException(); } }

        public override long Position {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public void Unblock() {
            _blockEvent.Set();
        }
    }

    public class MockAsyncReadableStream : Stream {
        private readonly long _size;
        private int _position;

        public MockAsyncReadableStream(long size) {
            _position = 0;
            _size = size;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return _size; } }
        public override long Position {
            get { return _position; }
            set { throw new NotImplementedException(); }
        }

        public override void Flush() { }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            var asyncResult = new MockAsyncResult {
                AsyncState = state,
                Buffer = buffer,
                Count = count,
                Offset = offset
            };
            Async.Fork(() => callback(asyncResult));
            return asyncResult;
        }

        public override int EndRead(IAsyncResult asyncResult) {

            // some artificial latency
            //Thread.Sleep(1);
            var result = (MockAsyncResult)asyncResult;
            var read = 0;
            for(var i = 0; i < result.Count; i++) {
                if(_position == _size) {
                    return read;
                }
                result.Buffer[result.Offset + i] = 0;
                _position++;
                read++;
            }
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }
    }
    public class MockAsyncResult : IAsyncResult {
        public bool IsCompleted { get; set; }
        public WaitHandle AsyncWaitHandle { get; set; }
        public object AsyncState { get; set; }
        public bool CompletedSynchronously { get; set; }
        public byte[] Buffer;
        public int Offset;
        public int Count;
    }
}
