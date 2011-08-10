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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using log4net;
using MindTouch.IO;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Traum.Webclient.Test {

    [TestFixture]
    public class PlugTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        [SetUp]
        public void Setup() {
            LocalEndpointBridge.Init();
        }

        [TearDown]
        public void Teardown() {
            MockPlug.DeregisterAll();
            MockPlug2.DeregisterAll();
        }

        [Test]
        public void With_and_without_cookiejar() {
            var global = new DreamCookie("test", "global", new XUri("http://baz.com/foo"));
            var globalCollection = new List<DreamCookie>();
            globalCollection.Add(global);
            Plug.GlobalCookies.Update(globalCollection, null);
            var local = new DreamCookie("test", "local", new XUri("http://baz.com/foo"));
            var localCollection = new List<DreamCookie>();
            localCollection.Add(local);
            var localJar = new DreamCookieJar();
            localJar.Update(localCollection, null);
            Plug globalPlug = Plug.New("http://baz.com/foo/bar");
            Plug localPlug = globalPlug.WithCookieJar(localJar);
            Plug globalPlugX = localPlug.WithoutCookieJar();
            Assert.AreEqual("global", globalPlug.CookieJar.Fetch(globalPlug.Uri)[0].Value);
            Assert.AreEqual("local", localPlug.CookieJar.Fetch(localPlug.Uri)[0].Value);
            Assert.AreEqual("global", globalPlugX.CookieJar.Fetch(globalPlugX.Uri)[0].Value);
        }

        [Test]
        public void Get_via_http_hits_dream_over_wire() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    if(string.IsNullOrEmpty(request.Headers.DreamPublicUri)) {
                        throw new Dream.DreamBadRequestException("didn't get DreamPublicUri header, indicating we didn't arrive via wire");
                    }
                    response.Return(TestEx.DreamMessage("foo"));
                };

                var r = mock.AtLocalHost.AsTraumPlug().Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual("foo", r.ToText());
            }
        }

        [Test]
        public void Get_via_http_and_AutoRedirect_off_shows_302() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var redirectUri = new XUri("mock://foo/bar");
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        redirectCalled++;
                        response.Return(Dream.DreamMessage.Redirect(redirectUri.ToDreamUri()));
                        return;
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalHost.Uri.WithScheme("ext-http").At("redirect");
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).WithHeader("h", "y").WithoutAutoRedirects().Get(TimeSpan.MaxValue).Result;
                Assert.AreEqual(DreamStatus.Found, r.Status, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect called incorrectly");
                Assert.AreEqual(redirectUri.ToString(), r.Headers.Location.ToString());
            }
        }

        [Test]
        public void Get_via_http_follows_301_and_forwards_headers() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var targetCalled = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    var h = request.Headers["h"];
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        if(h == "y") {
                            redirectCalled++;
                            var headers = new Dream.DreamHeaders {
                                { DreamHeaders.LOCATION, context.Service.Self.Uri.At("target").AsPublicUri().ToString() }
                            };
                            response.Return(new Dream.DreamMessage(Dream.DreamStatus.MovedPermanently, headers));
                            return;
                        }
                        msg = "redirect request lacked header";
                    }
                    if(context.Uri.LastSegment == "target") {
                        _log.Debug("called target");
                        if(h == "y") {
                            _log.Debug("target request had header");
                            targetCalled++;
                            response.Return(Dream.DreamMessage.Ok());
                            return;
                        }
                        msg = "target request lacked header ({1}";
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalHost.Uri.WithScheme("ext-http").At("redirect");
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).WithHeader("h", "y").Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect called incorrectly");
                Assert.AreEqual(1, targetCalled, "target called incorrectly");
            }
        }

        [Test]
        public void Get_via_http_follows_301_but_expects_query_to_be_in_location() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var targetCalled = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    var q = context.Uri.GetParam("q");
                    var forward = context.Uri.GetParam("forward");
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        var redirect = context.Service.Self.Uri.At("target").AsPublicUri();
                        if(forward == "true") {
                            redirect = redirect.With("q", q);
                        }
                        redirectCalled++;
                        var headers = new Dream.DreamHeaders { { DreamHeaders.LOCATION, redirect.ToString() } };
                        response.Return(new Dream.DreamMessage(Dream.DreamStatus.MovedPermanently, headers));
                        return;
                    }
                    if(context.Uri.LastSegment == "target") {
                        _log.Debug("called target");
                        if(q == "x") {
                            _log.Debug("target request had query");
                            targetCalled++;
                            response.Return(Dream.DreamMessage.Ok());
                            return;
                        }
                        response.Return(Dream.DreamMessage.BadRequest("missing query param"));
                        return;
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalHost.Uri.WithScheme("ext-http").At("redirect");
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).With("q", "x").Get(TimeSpan.MaxValue).Result;
                Assert.AreEqual(DreamStatus.BadRequest, r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect without forward called incorrectly");
                Assert.AreEqual(0, targetCalled, "target without forward called incorrectly");
                redirectCalled = 0;
                targetCalled = 0;
                r = Plug.New(uri).With("q", "x").With("forward", "true").Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect with forward called incorrectly");
                Assert.AreEqual(1, targetCalled, "target with forward called incorrectly");
            }
        }

        [Test]
        public void Get_via_http_follows_302_and_forwards_headers() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var targetCalled = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    var h = request.Headers["h"];
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        if(h == "y") {
                            redirectCalled++;
                            response.Return(Dream.DreamMessage.Redirect(context.Service.Self.Uri.At("target").AsPublicUri()));
                            return;
                        }
                        msg = "redirect request lacked header";
                    }
                    if(context.Uri.LastSegment == "target") {
                        _log.Debug("called target");
                        if(h == "y") {
                            _log.Debug("target request had header");
                            targetCalled++;
                            response.Return(Dream.DreamMessage.Ok());
                            return;
                        }
                        msg = "target request lacked header ({1}";
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalHost.Uri.WithScheme("ext-http").At("redirect");
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).WithHeader("h", "y").Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect called incorrectly");
                Assert.AreEqual(1, targetCalled, "target called incorrectly");
            }
        }

        [Test]
        public void Get_via_http_follows_but_expects_query_to_be_in_location() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var targetCalled = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    var q = context.Uri.GetParam("q");
                    var forward = context.Uri.GetParam("forward");
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        var redirect = context.Service.Self.Uri.At("target").AsPublicUri();
                        if(forward == "true") {
                            redirect = redirect.With("q", q);
                        }
                        redirectCalled++;
                        response.Return(Dream.DreamMessage.Redirect(redirect));
                        return;
                    }
                    if(context.Uri.LastSegment == "target") {
                        _log.Debug("called target");
                        if(q == "x") {
                            _log.Debug("target request had query");
                            targetCalled++;
                            response.Return(Dream.DreamMessage.Ok());
                            return;
                        }
                        response.Return(Dream.DreamMessage.BadRequest("missing query param"));
                        return;
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalHost.Uri.WithScheme("ext-http").At("redirect");
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).With("q", "x").Get(TimeSpan.MaxValue).Result;
                Assert.AreEqual(DreamStatus.BadRequest, r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect without forward called incorrectly");
                Assert.AreEqual(0, targetCalled, "target without forward called incorrectly");
                redirectCalled = 0;
                targetCalled = 0;
                r = Plug.New(uri).With("q", "x").With("forward", "true").Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect with forward called incorrectly");
                Assert.AreEqual(1, targetCalled, "target with forward called incorrectly");
            }
        }

        [Test]
        public void Get_via_internal_routing_and_AutoRedirect_off_shows_302() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var redirectUri = new XUri("mock://foo/bar");
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        redirectCalled++;
                        response.Return(Dream.DreamMessage.Redirect(redirectUri.ToDreamUri()));
                        return;
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalMachine.At("redirect").ToTraumUri();
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).WithHeader("h", "y").WithoutAutoRedirects().Get(TimeSpan.MaxValue).Result;
                Assert.AreEqual(DreamStatus.Found, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect called incorrectly");
                Assert.AreEqual(redirectUri.ToString(), r.Headers.Location.ToString());
            }
        }

        [Test]
        public void Get_via_internal_routing_follows_301_and_forwards_headers() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var targetCalled = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    var h = request.Headers["h"];
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        if(h == "y") {
                            redirectCalled++;
                            var headers = new Dream.DreamHeaders {
                                { DreamHeaders.LOCATION, context.Service.Self.Uri.At("target").AsPublicUri().ToString() }
                            };
                            response.Return(new Dream.DreamMessage(Dream.DreamStatus.MovedPermanently, headers));
                            return;
                        }
                        msg = "redirect request lacked header";
                    }
                    if(context.Uri.LastSegment == "target") {
                        _log.Debug("called target");
                        if(h == "y") {
                            _log.Debug("target request had header");
                            targetCalled++;
                            response.Return(Dream.DreamMessage.Ok());
                            return;
                        }
                        msg = "target request lacked header ({1}";
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalMachine.At("redirect").ToTraumUri();
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).WithHeader("h", "y").Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect called incorrectly");
                Assert.AreEqual(1, targetCalled, "target called incorrectly");
            }
        }

        [Test]
        public void Get_via_internal_routing_follows_301_but_expects_query_to_be_in_location() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var targetCalled = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    var q = context.Uri.GetParam("q");
                    var forward = context.Uri.GetParam("forward");
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        var redirect = context.Service.Self.Uri.At("target").AsPublicUri();
                        if(forward == "true") {
                            redirect = redirect.With("q", q);
                        }
                        redirectCalled++;
                        var headers = new Dream.DreamHeaders { { DreamHeaders.LOCATION, redirect.ToString() } };
                        response.Return(new Dream.DreamMessage(Dream.DreamStatus.MovedPermanently, headers));
                        return;
                    }
                    if(context.Uri.LastSegment == "target") {
                        _log.Debug("called target");
                        if(q == "x") {
                            _log.Debug("target request had query");
                            targetCalled++;
                            response.Return(Dream.DreamMessage.Ok());
                            return;
                        }
                        response.Return(Dream.DreamMessage.BadRequest("missing query param"));
                        return;
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalMachine.At("redirect").ToTraumUri();
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).With("q", "x").Get(TimeSpan.MaxValue).Result;
                Assert.AreEqual(DreamStatus.BadRequest, r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect without forward called incorrectly");
                Assert.AreEqual(0, targetCalled, "target without forward called incorrectly");
                redirectCalled = 0;
                targetCalled = 0;
                r = Plug.New(uri).With("q", "x").With("forward", "true").Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect with forward called incorrectly");
                Assert.AreEqual(1, targetCalled, "target with forward called incorrectly");
            }
        }

        [Test]
        public void Get_via_internal_routing_follows_302_and_forwards_headers() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var targetCalled = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    var h = request.Headers["h"];
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        if(h == "y") {
                            redirectCalled++;
                            response.Return(Dream.DreamMessage.Redirect(context.Service.Self.Uri.At("target").AsPublicUri()));
                            return;
                        }
                        msg = "redirect request lacked header";
                    }
                    if(context.Uri.LastSegment == "target") {
                        _log.Debug("called target");
                        if(h == "y") {
                            _log.Debug("target request had header");
                            targetCalled++;
                            response.Return(Dream.DreamMessage.Ok());
                            return;
                        }
                        msg = "target request lacked header ({1}";
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalMachine.At("redirect").ToTraumUri();
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).WithHeader("h", "y").Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect called incorrectly");
                Assert.AreEqual(1, targetCalled, "target called incorrectly");
            }
        }

        [Test]
        public void Get_via_internal_routing_follows_but_expects_query_to_be_in_location() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var redirectCalled = 0;
                var targetCalled = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    var msg = "nothing here";
                    var q = context.Uri.GetParam("q");
                    var forward = context.Uri.GetParam("forward");
                    if(context.Uri.LastSegment == "redirect") {
                        _log.Debug("called redirect");
                        var redirect = context.Service.Self.Uri.At("target").AsPublicUri();
                        if(forward == "true") {
                            redirect = redirect.With("q", q);
                        }
                        redirectCalled++;
                        response.Return(Dream.DreamMessage.Redirect(redirect));
                        return;
                    }
                    if(context.Uri.LastSegment == "target") {
                        _log.Debug("called target");
                        if(q == "x") {
                            _log.Debug("target request had query");
                            targetCalled++;
                            response.Return(Dream.DreamMessage.Ok());
                            return;
                        }
                        response.Return(Dream.DreamMessage.BadRequest("missing query param"));
                        return;
                    }
                    _log.DebugFormat("called uri: {0} => {1}", context.Uri, msg);
                    response.Return(Dream.DreamMessage.NotFound(msg));
                };
                var uri = mock.AtLocalMachine.At("redirect").ToTraumUri();
                _log.DebugFormat("calling redirect service at {0}", uri);
                var r = Plug.New(uri).With("q", "x").Get(TimeSpan.MaxValue).Result;
                Assert.AreEqual(DreamStatus.BadRequest, r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect without forward called incorrectly");
                Assert.AreEqual(0, targetCalled, "target without forward called incorrectly");
                redirectCalled = 0;
                targetCalled = 0;
                r = Plug.New(uri).With("q", "x").With("forward", "true").Get(TimeSpan.MaxValue).Result;
                Assert.IsTrue(r.IsSuccessful, "request failed: " + r.Status);
                Assert.AreEqual(1, redirectCalled, "redirect with forward called incorrectly");
                Assert.AreEqual(1, targetCalled, "target with forward called incorrectly");
            }
        }

        [Test]
        public void New_plug_gets_default_redirects() {
            Assert.AreEqual(Plug.DEFAULT_MAX_AUTO_REDIRECTS, Plug.New("http://foo/").MaxAutoRedirects);
        }

        [Test]
        public void AutoRedirect_only_follows_specified_times() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                var totalCalls = 0;
                mock.Service.CatchAllCallback = delegate(DreamContext context, Dream.DreamMessage request, Result<Dream.DreamMessage> response) {
                    totalCalls++;
                    _log.DebugFormat("call {0} to redirect", totalCalls);
                    response.Return(Dream.DreamMessage.Redirect(context.Uri.WithoutQuery().With("c", totalCalls.ToString())));
                };
                var uri = mock.AtLocalMachine.At("redirect").ToTraumUri();
                var redirects = 10;
                var expectedCalls = redirects + 1;
                var r = Plug.New(uri).WithAutoRedirects(10).Get(TimeSpan.MaxValue).Result;
                Assert.AreEqual(DreamStatus.Found, r.Status);
                Assert.AreEqual(expectedCalls, totalCalls, "redirect without forward called incorrectly");
                Assert.AreEqual(uri.With("c", expectedCalls.ToString()).ToString(), r.Headers.Location.ToString());
            }
        }

        [Test]
        public void Result_timeout_is_used_for_message_memorization_and_results_in_ResponseDataTransferTimeout() {
            var blockingStream = new MockBlockingStream();
            MockPlug2.Register(new XUri("mock://mock"), (plug, verb, uri, request) => {
                _log.Debug("returning blocking stream");
                return new DreamMessage(DreamStatus.Ok, null, MimeType.TEXT, -1, blockingStream).AsCompletedTask();
            });
            var stopwatch = Stopwatch.StartNew();
            _log.Debug("calling plug");
            var r = Plug.New(MockPlug2.DefaultUri)
                .WithTimeout(1.Seconds())
                .Get(3.Seconds())
                .Block();
            _log.Debug("plug done");
            stopwatch.Stop();
            blockingStream.Unblock();
            Assert.GreaterOrEqual(stopwatch.Elapsed.Seconds, 3);
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 4);
            Assert.IsFalse(r.IsFaulted);
            Assert.AreEqual(DreamStatus.ResponseDataTransferTimeout, r.Result.Status);
        }


        [Test]
        public void Plug_timeout_on_request_returns_RequestConnectionTimeout_not_ResponseDataTransferTimeout() {
            var blockingStream = new MockBlockingStream();
            MockPlug2.Register(new XUri("mock://mock"), (plug, verb, uri, request) => {
                _log.Debug("blocking request");
                Thread.Sleep(5.Seconds());
                _log.Debug("returning blocking stream");
                return new DreamMessage(DreamStatus.Ok, null, MimeType.TEXT, -1, blockingStream).AsCompletedTask();
            });
            var stopwatch = Stopwatch.StartNew();
            _log.Debug("calling plug");
            var r = Plug.New(MockPlug2.DefaultUri)
                .WithTimeout(1.Seconds())
                .Get(5.Seconds())
                .Block();
            _log.Debug("plug done");
            stopwatch.Stop();
            blockingStream.Unblock();
            Assert.GreaterOrEqual(stopwatch.Elapsed.Seconds, 1);
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 3);
            Assert.IsFalse(r.IsFaulted);
            Assert.AreEqual(DreamStatus.RequestConnectionTimeout, r.Result.Status);
        }

        [Test]
        public void Can_append_trailing_slash() {
            var plug = Plug.New("http://foo/bar").WithTrailingSlash();
            Assert.IsTrue(plug.Uri.TrailingSlash);
            Assert.AreEqual("http://foo/bar/", plug.ToString());
        }

        [Test]
        public void WithTrailingSlash_only_adds_when_needed() {
            var plug = Plug.New("http://foo/bar/");
            Assert.IsTrue(plug.Uri.TrailingSlash);
            plug = plug.WithTrailingSlash();
            Assert.IsTrue(plug.Uri.TrailingSlash);
            Assert.AreEqual("http://foo/bar/", plug.ToString());
        }

        [Test]
        public void Can_remove_trailing_slash() {
            var plug = Plug.New("http://foo/bar/").WithoutTrailingSlash();
            Assert.IsFalse(plug.Uri.TrailingSlash);
            Assert.AreEqual("http://foo/bar", plug.ToString());
        }

        [Test]
        public void WithoutTrailingSlash_only_removes_when_needed() {
            var plug = Plug.New("http://foo/bar");
            Assert.IsFalse(plug.Uri.TrailingSlash);
            plug = plug.WithoutTrailingSlash();
            Assert.IsFalse(plug.Uri.TrailingSlash);
            Assert.AreEqual("http://foo/bar", plug.ToString());
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
