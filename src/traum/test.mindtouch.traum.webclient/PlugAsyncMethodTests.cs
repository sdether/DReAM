using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using NUnit.Framework;
using log4net;

namespace MindTouch.Traum.Webclient.Test {

    [TestFixture]
    public class PlugAsyncMethodTests {

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
        public void Plug_uses_own_timeout_to_govern_request_and_results_in_RequestConnectionTimeout() {
            MockPlug2.Register(new XUri("mock://mock"), (plug, verb, uri, request) => {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                return DreamMessage.Ok().AsCompletedTask();

            });
            var stopwatch = Stopwatch.StartNew();
            var r = Plug.New(MockPlug2.DefaultUri)
                .WithTimeout(TimeSpan.FromSeconds(1))
                .InvokeEx(Verb.GET, DreamMessage.Ok(), TimeSpan.MaxValue).Block();
            stopwatch.Stop();
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 2);
            Assert.IsFalse(r.IsFaulted);
            Assert.AreEqual(DreamStatus.RequestConnectionTimeout, r.Result.Status);
        }

        [Test]
        public void Result_timeout_superceeds_plug_timeout_and_results_in_RequestConnectionTimeout() {
            MockPlug2.Register(new XUri("mock://mock"), (plug, verb, uri, request) => {
                Thread.Sleep(TimeSpan.FromSeconds(10));
                return DreamMessage.Ok().AsCompletedTask();
            });
            var stopwatch = Stopwatch.StartNew();
            var r = Plug.New(MockPlug2.DefaultUri)
                .WithTimeout(TimeSpan.FromSeconds(20))
                .InvokeEx(Verb.GET, DreamMessage.Ok(), 1.Seconds()).Block();
            stopwatch.Stop();
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 2);
            Assert.IsFalse(r.IsFaulted);
            Assert.AreEqual(DreamStatus.RequestConnectionTimeout, r.Result.Status);
        }

        [Test]
        public void Plug_timeout_is_not_used_for_message_memorization() {
            var blockingStream = new MockBlockingStream();
            MockPlug2.Register(new XUri("mock://mock"), (plug, verb, uri, request) => {
                _log.Debug("returning blocking stream");
                return new DreamMessage(DreamStatus.Ok, null, MimeType.TEXT, -1, blockingStream).AsCompletedTask();
            });
            var stopwatch = Stopwatch.StartNew();
            var msg = Plug.New(MockPlug2.DefaultUri)
                .WithTimeout(TimeSpan.FromSeconds(1))
                .InvokeEx(Verb.GET, DreamMessage.Ok(), TimeSpan.MaxValue).Result;
            stopwatch.Stop();
            _log.Debug("completed request");
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 1);
            stopwatch = Stopwatch.StartNew();
            _log.Debug("memorizing request");
            var r = msg.Memorize(1.Seconds()).Block();
            stopwatch.Stop();
            blockingStream.Unblock();
            _log.Debug("completed request memorization");
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 2);
            Assert.IsTrue(r.IsFaulted);
            Assert.AreEqual(typeof(TimeoutException), r.Exception);
        }
    }
}
