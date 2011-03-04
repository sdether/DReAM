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
using log4net;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.IO;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class DreamFeatureTests {
        private static readonly ILog _log = LogUtils.CreateLog();
        private DreamHostInfo _hostInfo;
        private Plug _plug;
        private XDoc _blueprint;

        [TestFixtureSetUp]
        public void Init() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.web.server").Post(DreamMessage.Ok());
            var config = new XDoc("config")
               .Elem("path", "test")
               .Elem("sid", "http://services.mindtouch.com/dream/test/2010/07/featuretestserver");
            DreamMessage result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(config).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            _plug = Plug.New(_hostInfo.LocalHost.Uri.WithoutQuery()).At("test");
            _blueprint = _plug.At("@blueprint").Get().ToDocument();
        }

        [TestFixtureTearDown]
        public void Teardown() {
            _hostInfo.Dispose();
        }

        [Test]
        public void Can_define_sync_no_arg_feature() {
            AssertFeature(
                "GET:sync/nada",
                _plug.At("sync", "nada"));
        }

        [Test]
        public void Can_define_async_no_arg_feature() {
            AssertFeature(
                "GET:async/nada",
                _plug.At("async", "nada"));
        }

        [Test]
        public void Can_inject_path_parameters() {
            AssertFeature(
                "GET:sync/{x}/{y}",
                _plug.At("sync", "xx", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_path_parameters_without_attribute() {
            AssertFeature(
                "GET:sync/noattr/{x}/{y}",
                _plug.At("sync", "noattr", "xx", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_query_args() {
            AssertFeature(
                "GET:sync/queryargs",
                _plug.At("sync", "queryargs").With("x", "xx").With("y", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_query_args_without_attributes() {
            AssertFeature(
                "GET:sync/queryargs/noattr",
                _plug.At("sync", "queryargs", "noattr").With("x", "xx").With("y", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_list_query_args() {
            AssertFeature(
                "GET:sync/multiqueryargs",
                _plug.At("sync", "multiqueryargs").With("x", "1").With("x", "2").With("x", "3").With("y", "yy"),
                new XDoc("r").Elem("x", "1:2:3").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_list_query_args_without_attributes() {
            AssertFeature(
                "GET:sync/multiqueryargs/noattr",
                _plug.At("sync", "multiqueryargs", "noattr").With("x", "1").With("x", "2").With("x", "3").With("y", "yy"),
                new XDoc("r").Elem("x", "1:2:3").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_headers() {
            AssertFeature(
                "GET:sync/headers",
                _plug.At("sync", "headers").WithHeader("x", "xx").WithHeader("y", "yy"),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_cookie_strings() {
            var jar = new DreamCookieJar();
            jar.Update(new DreamCookie("x", "xx", _plug), _plug);
            jar.Update(new DreamCookie("y", "yy", _plug), _plug);
            AssertFeature(
                "GET:sync/cookies/string",
                _plug.At("sync", "cookies", "string").WithCookieJar(jar),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_cookie_objects() {
            var jar = new DreamCookieJar();
            jar.Update(new DreamCookie("x", "xx", _plug), _plug);
            jar.Update(new DreamCookie("y", "yy", _plug), _plug);
            AssertFeature(
                "GET:sync/cookies/obj",
                _plug.At("sync", "cookies", "obj").WithCookieJar(jar),
                new XDoc("r").Elem("x", "xx").Elem("y", "yy"));
        }

        [Test]
        public void Can_inject_cookie_objects_without_cookie_attribute() {
            var jar = new DreamCookieJar();
            jar.Update(new DreamCookie("x", "xx", _plug), _plug);
            AssertFeature(
                "GET:sync/cookies/obj",
                _plug.At("sync", "cookies", "obj").WithCookieJar(jar),
                new XDoc("r").Elem("x", "xx"));
        }

        [Test]
        public void Can_inject_verb() {
            AssertFeature(
                "GET:sync/verb",
                _plug.At("sync", "verb"),
                new XDoc("r").Elem("verb", "GET"));
        }

        [Test]
        public void Can_inject_path() {
            AssertFeature(
                "GET:sync/path",
                _plug.At("sync", "path"),
                new XDoc("r").Elem("path", "sync:path"));
        }

        [Test]
        public void Can_inject_uri() {
            var plug = _plug.At("sync", "uri");
            AssertFeature(
                "GET:sync/uri",
                plug,
                new XDoc("r").Elem("uri", plug));
        }

        [Test]
        public void Can_inject_document_body() {
            AssertFeature(
                "POST:sync/body/xdoc",
                _plug.At("sync", "body", "xdoc").Post(new XDoc("body").Elem("foo", "Bar"), new Result<DreamMessage>()),
                new XDoc("body").Elem("foo", "Bar"));
        }

        [Test]
        public void Can_inject_string_body() {
            AssertFeature(
                "POST:sync/body/string",
                _plug.At("sync", "body", "string").Post(DreamMessage.Ok(MimeType.TEXT, "foo"), new Result<DreamMessage>()),
                new XDoc("body").Value("foo"));
        }

        [Test]
        public void Can_inject_stream_body() {
            AssertFeature(
                "POST:sync/body/stream",
                _plug.At("sync", "body", "stream").Post(DreamMessage.Ok(MimeType.TEXT, "foo"), new Result<DreamMessage>()),
                new XDoc("body").Value("foo"));
        }

        [Test]
        public void Can_inject_byte_array_body() {
            AssertFeature(
                "POST:sync/body/bytes",
                _plug.At("sync", "body", "bytes").Post(DreamMessage.Ok(MimeType.TEXT, "foo"), new Result<DreamMessage>()),
                new XDoc("body").Value("foo"));
        }

        [Test]
        public void Can_inject_DreamContext() {
            AssertFeature(
                "POST:sync/context",
                _plug.At("sync", "context").Post(new XDoc("body").Elem("foo", "Bar"), new Result<DreamMessage>()),
                new XDoc("body").Elem("foo", "Bar"));
        }

        [Test]
        public void Can_inject_request_DreamMessage() {
            AssertFeature(
                "POST:sync/message",
                _plug.At("sync", "message").Post(new XDoc("body").Elem("foo", "Bar"), new Result<DreamMessage>()),
                new XDoc("body").Elem("foo", "Bar"));
        }

        private void AssertFeature(string pattern, Plug plug) {
            AssertFeature(pattern, plug, null);
        }

        private void AssertFeature(string pattern, Plug plug, XDoc expected) {
            AssertFeature(pattern, plug.Get(new Result<DreamMessage>()), expected);
        }

        private void AssertFeature(string pattern, Result<DreamMessage> result, XDoc expected) {
            var feature = _blueprint[string.Format("features/feature[pattern='{0}']", pattern)];
            Assert.IsTrue(feature.Any(), string.Format("service doesn't have a feature for {0}", pattern));
            var doc = new XDoc("response").Elem("method", feature["method"].AsText);
            if(expected != null) {
                doc.Add(expected);
            }
            var response = result.Wait();
            Assert.IsTrue(response.IsSuccessful, response.GetErrorString());
            Assert.AreEqual(doc.ToCompactString(), response.ToDocument().ToCompactString());
        }

        [DreamService("MindTouch Feature Test Service", "Copyright (c) 2006-2011 MindTouch, Inc.",
            Info = "http://www.mindtouch.com",
            SID = new[] { "http://services.mindtouch.com/dream/test/2010/07/featuretestserver" }
        )]
        public class FeatureTestService : DreamService {

            // --- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog();

            //-- Fields ---
            private Plug _inner;

            //--- Features ---
            [DreamFeature("GET:sync/nada", "")]
            public DreamMessage SyncNada() {
                return Response(null);
            }

            [DreamFeature("GET:async/nada", "")]
            public Yield AsyncNada(Result<DreamMessage> response) {
                response.Return(Response("AsyncNada", null));
                yield break;
            }

            [DreamFeature("GET:sync/{x}/{y}", "")]
            public DreamMessage SyncXY(
                [Path] string x,
                [Path] string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/noattr/{x}/{y}", "")]
            public DreamMessage SyncXYNoAttr(
                string x,
                string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/multiqueryargs", "")]
            public DreamMessage SyncMultiQueryArgs(
                [Query] string[] x,
                [Query] string y
            ) {
                return Response(new XDoc("r").Elem("x", string.Join(":", x)).Elem("y", y));
            }

            [DreamFeature("GET:sync/multiqueryargs/noattr", "")]
            public DreamMessage SyncMultiQueryArgsNoAttr(
                string[] x,
                string y
            ) {
                return Response(new XDoc("r").Elem("x", string.Join(":", x)).Elem("y", y));
            }

            [DreamFeature("GET:sync/queryargs", "")]
            public DreamMessage SyncQueryArgs(
                [Query] string x,
                [Query] string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/queryargs/noattr", "")]
            public DreamMessage SyncQueryArgsNoAttr(
                string x,
                string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/headers", "")]
            public DreamMessage SyncHeaders(
                [Header] string x,
                [Header] string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/cookies/string", "")]
            public DreamMessage SyncStringCookies(
                [Cookie] string x,
                [Cookie] string y
            ) {
                return Response(new XDoc("r").Elem("x", x).Elem("y", y));
            }

            [DreamFeature("GET:sync/cookies/obj", "")]
            public DreamMessage SyncDreamCookies(
                [Cookie] DreamCookie x,
                [Cookie] string y
            ) {
                return Response(new XDoc("r").Elem("x", x.Value).Elem("y", y));
            }

            [DreamFeature("GET:sync/cookies/obj/noattr", "")]
            public DreamMessage SyncDreamCookiesNoAttr(
                DreamCookie x
            ) {
                return Response(new XDoc("r").Elem("x", x.Value));
            }

            [DreamFeature("GET:sync/verb", "")]
            public DreamMessage SyncVerb(string verb) {
                return Response(new XDoc("r").Elem("verb", verb));
            }

            [DreamFeature("GET:sync/path", "")]
            public DreamMessage SyncPath(string[] path) {
                return Response(new XDoc("r").Elem("path", string.Join(":", path)));
            }

            [DreamFeature("GET:sync/uri", "")]
            public DreamMessage SyncUri(XUri uri) {
                return Response(new XDoc("r").Elem("uri", uri.WithoutQuery()));
            }

            [DreamFeature("POST:sync/body/xdoc", "")]
            public DreamMessage SyncBodyXDoc(XDoc body) {
                return Response(body);
            }

            [DreamFeature("POST:sync/body/string", "")]
            public DreamMessage SyncBodyString(string body) {
                return Response(new XDoc("body").Value(body));
            }

            [DreamFeature("POST:sync/body/bytes", "")]
            public DreamMessage SyncBodyBytes(byte[] body) {
                return Response(new XDoc("body").Value(Encoding.UTF8.GetString(body)));
            }

            [DreamFeature("POST:sync/body/stream", "")]
            public DreamMessage SyncBodyStream(Stream body) {
                using(var reader = new StreamReader(body)) {
                    return Response(new XDoc("body").Value(reader.ReadToEnd()));
                }
            }

            [DreamFeature("POST:sync/context", "")]
            public DreamMessage SyncContext(DreamContext context) {
                return Response(context.Request.ToDocument());
            }

            [DreamFeature("POST:sync/message", "")]
            public DreamMessage SyncMessage(DreamMessage request) {
                return Response(request.ToDocument());
            }

            //--- Methods ---
            protected override Yield Start(XDoc config, Result result) {
                yield return Coroutine.Invoke(base.Start, config, new Result());
                yield return CreateService("inner", "http://services.mindtouch.com/dream/test/2007/03/sample-inner", new XDoc("config").Start("prologue").Attr("name", "dummy").Value("p3").End().Start("epilogue").Attr("name", "dummy").Value("e3").End(), new Result<Plug>()).Set(v => _inner = v);
                result.Return();
            }

            protected override Yield Stop(Result result) {
                if(_inner != null) {
                    yield return _inner.DeleteAsync().CatchAndLog(_log);
                    _inner = null;
                }
                yield return Coroutine.Invoke(base.Stop, new Result());
                result.Return();
            }

            private DreamMessage Response(XDoc body) {
                var frame = new System.Diagnostics.StackFrame(1, false);
                return Response(frame.GetMethod().Name, body);
            }

            private DreamMessage Response(string methodName, XDoc body) {
                var doc = new XDoc("response").Elem("method", methodName);
                if(body != null) {
                    doc.Add(body);
                }
                return DreamMessage.Ok(doc);
            }
        }
    }
}
