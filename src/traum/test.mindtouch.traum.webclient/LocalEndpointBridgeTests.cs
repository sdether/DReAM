using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Traum.Webclient.Test {

    [TestFixture]
    public class LocalEndpointBridgeTests {

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
        public void Local_scheme_gets_redirected_to_Dream() {
            MockPlug.Setup("local://foo/bar").Returns(TestEx.DreamMessage("foobar"));
            var msg = Plug.New("local://foo").At("bar").Get().Result;
            Assert.IsTrue(msg.IsSuccessful, "request failed:" + msg.Status);
            Assert.AreEqual("foobar", msg.ToText());
        }

        [Test]
        public void Local_scheme_gets_redirected_to_Dreamhost() {
            using(var hostInfo = DreamTestHelper.CreateRandomPortHost()) {
                var mock = MockService.CreateMockService(hostInfo);
                mock.Service.CatchAllCallback = (context, request, response) => response.Return(TestEx.DreamMessage("foobar"));
                var msg = mock.AtLocalMachine.AsTraumPlug().Get().Result;
                Assert.IsTrue(msg.IsSuccessful, "request failed:" + msg.Status);
                Assert.AreEqual("foobar", msg.ToText());
            }
        }
    }
}
