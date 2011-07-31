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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MindTouch.Dream;
using log4net;
using MindTouch.Extensions.Time;
using MindTouch.Xml;
using MindTouch.Traum.Test.Mock;
using NUnit.Framework;

namespace MindTouch.Traum.Test {

    [TestFixture]
    public class MockPlugTests {

        private static readonly ILog _log = LogUtils.CreateLog();

        [TearDown]
        public void PerTestCleanup() {
            MockPlug2.DeregisterAll();
        }

        [Test]
        public void Default_uri_works_as_no_op_without_registrations() {
            var msg = Plug2.New(MockPlug2.DefaultUri).Get();
            Assert.AreEqual("empty", msg.ToDocument().Name);
        }

        [Test]
        public void Default_uri_keeps_working_as_no_op_after_DeregisterAll() {
            MockPlug2.DeregisterAll();
            var msg = Plug2.New(MockPlug2.DefaultUri).Get();
            Assert.AreEqual("empty", msg.ToDocument().Name);
        }

        [Test]
        public void Register_twice_throws() {
            var uri = new XUri("http://www.mindtouch.com/foo");
            MockPlug2.Register(uri, (p, v, u, r) => System.Threading.Tasks.TaskEx.FromResult(DreamMessage2.Ok()));
            try {
                MockPlug2.Register(uri, (p, v, u, r) => DreamMessage2.Ok().AsCompletedTask());
            } catch(ArgumentException) {
                return;
            } catch(Exception e) {
                Assert.Fail("wrong exception: " + e);
            }
            Assert.Fail("no exception`");
        }

        [Test]
        public void Deregister_allows_reregister_of_uri() {
            XUri uri = new XUri("http://www.mindtouch.com/foo");
            int firstCalled = 0;
            MockPlug2.Register(uri, (p, v, u, r) => {
                firstCalled++;
                return DreamMessage2.Ok().AsCompletedTask();
            });
            Assert.IsTrue(Plug2.New(uri).Get(TimeSpan.MaxValue).Result.IsSuccessful);
            Assert.AreEqual(1, firstCalled);
            MockPlug2.Deregister(uri);
            int secondCalled = 0;
            MockPlug2.Register(uri, (p, v, u, r) => {
                secondCalled++;
                return DreamMessage2.Ok().AsCompletedTask();
            });
            Assert.IsTrue(Plug2.New(uri).Get(TimeSpan.MaxValue).Result.IsSuccessful);
            Assert.AreEqual(1, firstCalled);
            Assert.AreEqual(1, secondCalled);
        }

        [Test]
        public void DeregisterAll_clears_all_mocks() {
            int firstCalled = 0;
            XUri uri = new XUri("http://www.mindtouch.com/foo");
            MockPlug2.Register(uri, (p, v, u, r) => {
                firstCalled++;
                return DreamMessage2.Ok().AsCompletedTask();
            });
            MockPlug2.Register(new XUri("http://www.mindtouch.com/bar"), (p, v, u, r) => DreamMessage2.Ok().AsCompletedTask());
            Assert.IsTrue(Plug2.New(uri).Get(TimeSpan.MaxValue).Result.IsSuccessful);
            Assert.AreEqual(1, firstCalled);
            MockPlug2.DeregisterAll();
            int secondCalled = 0;
            MockPlug2.Register(uri, (p, v, u, r) => {
                secondCalled++;
                return DreamMessage2.Ok().AsCompletedTask();
            });
            MockPlug2.Register(new XUri("http://www.mindtouch.com/bar"), (p, v, u, r) => DreamMessage2.Ok().AsCompletedTask());
            Assert.IsTrue(Plug2.New(uri).Get(TimeSpan.MaxValue).Result.IsSuccessful);
            Assert.AreEqual(1, firstCalled);
            Assert.AreEqual(1, secondCalled);
        }

        [Test]
        public void Mock_intercepts_exact_match() {
            int called = 0;
            Plug2 calledPlug2 = null;
            string calledVerb = null;
            XUri calledUri = null;
            DreamMessage2 calledRequest;
            MockPlug2.Register(new XUri("http://www.mindtouch.com/foo"), (p, v, u, r) => {
                calledPlug2 = p;
                calledVerb = v;
                calledUri = u;
                calledRequest = r;
                called++;
                return DreamMessage2.Ok().AsCompletedTask();
            });

            DreamMessage2 response = Plug2.New("http://www.mindtouch.com").At("foo").Get(TimeSpan.MaxValue).Result;
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            Assert.AreEqual(1, called);
            Assert.AreEqual("GET", calledVerb);
            Assert.AreEqual(calledUri, new XUri("http://www.mindtouch.com/foo"));
        }

        [Test]
        public void Mock_intercepts_child_path() {
            int called = 0;
            Plug2 calledPlug2 = null;
            string calledVerb = null;
            XUri calledUri = null;
            DreamMessage2 calledRequest;
            MockPlug2.Register(new XUri("http://www.mindtouch.com"), (p, v, u, r) => {
                calledPlug2 = p;
                calledVerb = v;
                calledUri = u;
                calledRequest = r;
                called++;
                return DreamMessage2.Ok().AsCompletedTask();
            });

            Plug2 plug = Plug2.New("http://www.mindtouch.com").At("foo");
            DreamMessage2 response = plug.Get(TimeSpan.MaxValue).Result;
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            Assert.AreEqual(1, called);
            Assert.AreEqual("GET", calledVerb);
            Assert.AreEqual(new XUri("http://www.mindtouch.com").At("foo"), calledUri);
            Assert.AreEqual(plug, calledPlug2);
        }

        [Test]
        public void Mock_receives_proper_request_body() {
            int called = 0;
            Plug2 calledPlug2 = null;
            string calledVerb = null;
            XUri calledUri = null;
            DreamMessage2 calledRequest = null;
            MockPlug2.Register(new XUri("http://www.mindtouch.com/foo"), (p, v, u, r) => {
                calledPlug2 = p;
                calledVerb = v;
                calledUri = u;
                calledRequest = r;
                called++;
                return DreamMessage2.Ok().AsCompletedTask();
            });
            XDoc doc = new XDoc("message").Elem("foo");
            DreamMessage2 response = Plug2.New("http://www.mindtouch.com").At("foo").Post(doc,TimeSpan.MaxValue).Result;
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            Assert.AreEqual(1, called);
            Assert.AreEqual("POST", calledVerb);
            Assert.AreEqual(doc, calledRequest.ToDocument());
            Assert.AreEqual(calledUri, new XUri("http://www.mindtouch.com/foo"));
        }

        [Test]
        public void Mock_sends_back_proper_response_body() {
            int called = 0;
            Plug2 calledPlug2 = null;
            string calledVerb = null;
            XUri calledUri = null;
            DreamMessage2 calledRequest = null;
            XDoc responseDoc = new XDoc("message").Elem("foo");
            MockPlug2.Register(new XUri("http://www.mindtouch.com/foo"), (p, v, u, r) => {
                calledPlug2 = p;
                calledVerb = v;
                calledUri = u;
                calledRequest = r;
                called++;
                return DreamMessage2.Ok(responseDoc).AsCompletedTask();
            });
            XDoc doc = new XDoc("message").Elem("foo");
            DreamMessage2 response = Plug2.New("http://www.mindtouch.com").At("foo").Get(TimeSpan.MaxValue).Result;
            Assert.IsTrue(response.IsSuccessful, response.ToText());
            Assert.AreEqual(1, called);
            Assert.AreEqual("GET", calledVerb);
            Assert.AreEqual(responseDoc, response.ToDocument());
            Assert.AreEqual(calledUri, new XUri("http://www.mindtouch.com/foo"));
        }

        [Test]
        public void PostAsync_from_nested_async_workers() {
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            MockPlug2.Register(new XUri("http://foo/bar"), (p, v, u, r) => {
                resetEvent.Set();
                return DreamMessage2.Ok().AsCompletedTask();
            });
            Plug2.New("http://foo/bar").Post(TimeSpan.MaxValue);
            Assert.IsTrue(resetEvent.WaitOne(1000, false), "no async failed");
            System.Threading.Tasks.TaskEx.Run(() => Plug2.New("http://foo/bar").Post(TimeSpan.MaxValue));
            Assert.IsTrue(resetEvent.WaitOne(1000, false), "async failed");
            System.Threading.Tasks.TaskEx.Run(() => System.Threading.Tasks.TaskEx.Run(() => Plug2.New("http://foo/bar").Post(TimeSpan.MaxValue)));
            Assert.IsTrue(resetEvent.WaitOne(1000, false), "nested async failed");
            System.Threading.Tasks.TaskEx.Run(() => System.Threading.Tasks.TaskEx.Run(() => System.Threading.Tasks.TaskEx.Run(() => Plug2.New("http://foo/bar").Post(TimeSpan.MaxValue))));
            Assert.IsTrue(resetEvent.WaitOne(1000, false), "double async failed");
        }

        [Test]
        public void MockPlug_can_verify_call_via_VerifyAll() {
            MockPlug2.Setup(new XUri("http://mock/foo")).ExpectCalls(Times.AtLeastOnce());
            Assert.IsTrue(Plug2.New("http://mock/foo").GetAsync().Result.IsSuccessful);
            MockPlug2.VerifyAll();
        }

        [Test]
        public void Can_verify_call() {
            var mock = MockPlug2.Setup(new XUri("http://mock/foo")).ExpectCalls(Times.AtLeastOnce());
            Assert.IsTrue(Plug2.New("http://mock/foo").GetAsync().Result.IsSuccessful);
            mock.Verify();
        }

        [Test]
        public void Can_verify_lack_of_call() {
            var mock = MockPlug2.Setup(new XUri("http://mock/foo")).ExpectCalls(Times.Never());
            mock.Verify(TimeSpan.FromSeconds(3));
        }

        [Test]
        public void MockPlug_without_call_expectation_does_not_throw_on_Verify() {
            var mock = MockPlug2.Setup(new XUri("http://mock/foo"));
            mock.Verify(TimeSpan.FromSeconds(3));
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_verb() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).Verb("POST");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).Verb("GET");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).Verb("DELETE");
            Assert.IsTrue(Plug2.New("http://mock/foo").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Specific_verb_gets_picked_over_wildcard1() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo"));
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).Verb("GET");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).Verb("DELETE");
            Assert.IsTrue(Plug2.New("http://mock/foo").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Specific_verb_gets_picked_over_wildcard2() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).Verb("DELETE");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).Verb("GET");
            var c = MockPlug2.Setup(new XUri("http://mock/foo"));
            Assert.IsTrue(Plug2.New("http://mock/foo").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_subpath() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).At("bar");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).At("eek");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).At("baz");
            Assert.IsTrue(Plug2.New("http://mock/foo/eek").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_query() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).With("eek", "b");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).With("baz", "c");
            Assert.IsTrue(Plug2.New("http://mock/foo/").With("eek", "b").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_queryarg_values() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "b");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "c");
            Assert.IsTrue(Plug2.New("http://mock/foo/").With("bar", "b").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_queryarg_values_via_callback() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", x => x == "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", x => x == "b");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", x => x == "c");
            Assert.IsTrue(Plug2.New("http://mock/foo/").With("bar", "b").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_most_specific_query_args() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "1").With("y", "2");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "1");
            Assert.IsTrue(Plug2.New("http://mock/foo/").With("bar", "a").With("x", "1").With("y", "2").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_most_less_specific_query_arg_with_value_match() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "1");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "2").With("y", "2");
            Assert.IsTrue(Plug2.New("http://mock/foo/").With("bar", "a").With("x", "1").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Extraneous_args_are_not_considered_in_matching() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "1");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).With("bar", "a").With("x", "2").With("y", "2");
            Assert.IsTrue(Plug2.New("http://mock/foo/").With("bar", "a").With("x", "1").With("y", "2").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_differentiate_multiple_plugs_and_their_call_counts() {
            var bar = MockPlug2.Setup(new XUri("http://mock/foo")).At("bar").ExpectAtLeastOneCall();
            var eek = MockPlug2.Setup(new XUri("http://mock/foo")).At("eek").With("a", "b").ExpectCalls(Times.Exactly(3));
            var baz = MockPlug2.Setup(new XUri("http://mock/foo")).At("eek").With("b", "c").ExpectAtLeastOneCall();
            Assert.IsTrue(Plug2.New("http://mock/foo/bar").GetAsync().Result.IsSuccessful);
            Assert.IsTrue(Plug2.New("http://mock/foo/bar").GetAsync().Result.IsSuccessful);
            Assert.IsTrue(Plug2.New("http://mock/foo/eek").With("a", "b").GetAsync().Result.IsSuccessful);
            Assert.IsTrue(Plug2.New("http://mock/foo/eek").With("a", "b").GetAsync().Result.IsSuccessful);
            Assert.IsTrue(Plug2.New("http://mock/foo/eek").With("a", "b").GetAsync().Result.IsSuccessful);
            Assert.IsTrue(Plug2.New("http://mock/foo/eek").With("b", "c").GetAsync().Result.IsSuccessful);
            bar.Verify();
            baz.Verify();
            eek.Verify();
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_headers() {
            var bar = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var eek = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("eek", "b");
            var baz = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("baz", "c");
            Assert.IsTrue(Plug2.New("http://mock/foo/").WithHeader("eek", "b").GetAsync().Result.IsSuccessful);
            bar.Verify(2.Seconds(), Times.Never());
            baz.Verify(0.Seconds(), Times.Never());
            eek.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_header_values() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "b");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "c");
            Assert.IsTrue(Plug2.New("http://mock/foo/").WithHeader("bar", "b").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_header_values_via_callback() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", x => x == "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", x => x == "b");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", x => x == "c");
            Assert.IsTrue(Plug2.New("http://mock/foo/").WithHeader("bar", "b").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_most_specific_headers() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "1").WithHeader("y", "2");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "1");
            Assert.IsTrue(Plug2.New("http://mock/foo/").WithHeader("bar", "a").WithHeader("x", "1").WithHeader("y", "2").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_most_less_specific_header_with_value_match() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "1");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "2").WithHeader("y", "2");
            Assert.IsTrue(Plug2.New("http://mock/foo/").WithHeader("bar", "a").WithHeader("x", "1").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Extraneous_headers_are_not_considered_in_matching() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a");
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "1");
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).WithHeader("bar", "a").WithHeader("x", "2").WithHeader("y", "2");
            Assert.IsTrue(Plug2.New("http://mock/foo/").WithHeader("bar", "a").WithHeader("x", "1").WithHeader("y", "2").GetAsync().Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }


        [Test]
        public void Can_pick_appropriate_MockPlug_based_on_body() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).WithBody(new XDoc("doc").Elem("x", "a"));
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).WithBody(new XDoc("doc").Elem("x", "b"));
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).WithBody(new XDoc("doc").Elem("x", "c"));
            Assert.IsTrue(Plug2.New("http://mock/foo/").PostAsync(new XDoc("doc").Elem("x", "b")).Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_provide_callback_for_custom_body_check_and_pick_mockplug_based_on_its_return() {
            var a = MockPlug2.Setup(new XUri("http://mock/foo")).WithMessage(msg => false);
            var b = MockPlug2.Setup(new XUri("http://mock/foo")).WithMessage(msg => true);
            var c = MockPlug2.Setup(new XUri("http://mock/foo")).WithMessage(msg => false);
            Assert.IsTrue(Plug2.New("http://mock/foo/").PostAsync(new XDoc("doc")).Result.IsSuccessful);
            a.Verify(2.Seconds(), Times.Never());
            c.Verify(0.Seconds(), Times.Never());
            b.Verify(Times.Once());
        }

        [Test]
        public void Can_add_headers_to_the_response() {
            MockPlug2.Setup(new XUri("http://mock/foo")).WithResponseHeader("foo", "bar");
            var msg = Plug2.New("http://mock/foo").GetAsync().Result;
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual("bar", msg.Headers["foo"]);
        }

        [Test]
        public void Can_add_headers_to_the_response_after_specifying_message() {
            MockPlug2.Setup(new XUri("http://mock/foo")).Returns(DreamMessage2.NotModified()).WithResponseHeader("foo", "bar");
            var msg = Plug2.New("http://mock/foo").GetAsync().Result;
            Assert.AreEqual(DreamStatus.NotModified, msg.Status);
            Assert.AreEqual("bar", msg.Headers["foo"]);
        }

        [Test]
        public void Can_add_headers_to_the_response_before_specifying_message() {
            MockPlug2.Setup(new XUri("http://mock/foo")).WithResponseHeader("foo", "bar").Returns(DreamMessage2.NotModified());
            var msg = Plug2.New("http://mock/foo").GetAsync().Result;
            Assert.AreEqual(DreamStatus.NotModified, msg.Status);
            Assert.AreEqual("bar", msg.Headers["foo"]);
        }

        public void Can_return_XDoc() {
            var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            MockPlug2.Setup(new XUri("http://mock/foo")).Returns(doc);
            var msg = Plug2.New("http://mock/foo").GetAsync().Result;
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual(doc, msg.ToDocument());
        }

        [Test]
        public void Can_add_headers_to_the_response_after_specifying_document() {
            var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            MockPlug2.Setup(new XUri("http://mock/foo")).Returns(doc).WithResponseHeader("foo", "bar");
            var msg = Plug2.New("http://mock/foo").GetAsync().Result;
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual("bar", msg.Headers["foo"]);
            Assert.AreEqual(doc, msg.ToDocument());
        }

        [Test]
        public void Can_add_headers_to_the_response_before_specifying_document() {
            var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            MockPlug2.Setup(new XUri("http://mock/foo")).WithResponseHeader("foo", "bar").Returns(doc);
            var msg = Plug2.New("http://mock/foo").GetAsync().Result;
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual("bar", msg.Headers["foo"]);
            Assert.AreEqual(doc, msg.ToDocument());
        }

        [Test]
        public void Returns_callback_gets_request_data() {
            var doc = new XDoc("doc").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            var success = new XDoc("yay");
            var uri = new XUri("http://mock/foo/").With("foo", "baz");
            MockPlug2.Setup(new XUri("http://mock/foo")).Returns(invocation => {
                if(invocation.Verb != "POST") {
                    return DreamMessage2.BadRequest("wrong verb: " + invocation.Verb);
                }
                if(invocation.Uri != uri) {
                    return DreamMessage2.BadRequest("wrong uri: " + invocation.Uri);
                }
                if(invocation.Request.Headers["header"] != "value") {
                    return DreamMessage2.BadRequest("wrong header value");
                }
                if(invocation.Request.ToDocument() != doc) {
                    return DreamMessage2.BadRequest("wrong body");
                }
                return DreamMessage2.Ok(success);
            });
            var msg = Plug2.New(uri).WithHeader("header", "value").PostAsync(doc).Result;
            Assert.IsTrue(msg.IsSuccessful, msg.ToDocument().ToPrettyString());
            Assert.AreEqual(success, msg.ToDocument());
        }

        [Test]
        public void Returns_callback_gets_response_headers_if_added_before_callback() {
            var success = new XDoc("yay");
            MockPlug2.Setup(new XUri("http://mock/foo"))
                .WithResponseHeader("foo", "bar")
                .Returns(invocation => {
                    return invocation.ResponseHeaders["foo"] != "bar" ? DreamMessage2.BadRequest("wrong response header") : DreamMessage2.Ok(success);
                });
            var msg = Plug2.New("http://mock/foo/").GetAsync().Result;
            Assert.IsTrue(msg.IsSuccessful, msg.ToDocument().ToPrettyString());
            Assert.AreEqual(success, msg.ToDocument());
        }

        [Test]
        public void Returns_callback_gets_response_headers_if_added_after_callback() {
            var success = new XDoc("yay");
            MockPlug2.Setup(new XUri("http://mock/foo"))
                .Returns(invocation => {
                    return invocation.ResponseHeaders["foo"] != "bar" ? DreamMessage2.BadRequest("wrong response header") : DreamMessage2.Ok(success);
                })
                .WithResponseHeader("foo", "bar");
            var msg = Plug2.New("http://mock/foo/").GetAsync().Result;
            Assert.IsTrue(msg.IsSuccessful, msg.ToDocument().ToPrettyString());
            Assert.AreEqual(success, msg.ToDocument());
        }

        [Test]
        public void Can_mock_a_request_with_a_stream_body() {
            var tmp = Path.GetTempFileName();
            var payload = "blahblah";
            File.WriteAllText(tmp, payload);
            var message = DreamMessage2.FromFile(tmp);
            var uri = new XUri("http://mock/post/stream");
            MockPlug2.Setup(uri).Verb("POST")
                .WithMessage(m => m.ToText() == payload)
                .ExpectAtLeastOneCall();
            var response = Plug2.New(uri).PostAsync(message).Result;
            response.AssertSuccess();
            MockPlug2.VerifyAll(1.Seconds());
        }
    }
}
