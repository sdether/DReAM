using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Dream.Web.Client;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Web.Server.Test {

    [TestFixture]
    public class XDocTests {

        [Test]
        public void XmlAsUriWithDreamContext() {
            DreamHostInfo hostInfo = DreamTestHelper.CreateRandomPortHost();
            MockServiceInfo mock = MockService.CreateMockService(hostInfo);
            mock.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
                XUri uri = mock.AtLocalMachine.Uri;
                XDoc doc = new XDoc("test").Elem("uri", uri);
                Assert.AreEqual(uri.AsPublicUri().ToString(), doc["uri"].AsText);
                Assert.AreEqual(uri, doc["uri"].AsUri());
                response.Return(DreamMessage.Ok(doc));
            };
            DreamMessage result = mock.AtLocalMachine.PostAsync().Wait();
            Assert.IsTrue(result.IsSuccessful, "failure in service");
            Assert.AreEqual(mock.AtLocalHost.Uri.WithoutQuery(), result.ToDocument()["uri"].AsUri());
        }
    }
}

