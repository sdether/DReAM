using MindTouch.Dream;
using MindTouch.Dream.Web.Client;

namespace MindTouch.dream {
    internal class DreamContextUriTranslator : XUriEx.IUriTranslator {

        public XUri AsPublicUri(XUri uri) {
            var context = DreamContext.CurrentOrNull;
            return context == null ? uri : context.AsPublicUri(uri);
        }

        public XUri AsLocalUri(XUri uri) {
            var context = DreamContext.CurrentOrNull;
            return context == null ? uri : context.AsLocalUri(uri);
        }

        public XUri AsServerUri(XUri uri) {
            var context = DreamContext.CurrentOrNull;
            return context == null ? uri : context.AsServerUri(uri);
        }

    }
}
