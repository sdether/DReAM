using System;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Web.Server.Plug {
    internal class DreamContextPlugInterceptor : Dream.Plug.IPlugInterceptor {

        //--- Methods ---
        public DreamMessage PreProcess(string verb, XUri uri, XUri normalizedUri, DreamMessage message, DreamCookieJar cookies) {
            var context = DreamContext.CurrentOrNull;
            if(context == null) {
                return message;
            }

            // set request id header
            message.Headers.DreamRequestId = context.GetState<string>(DreamHeaders.DREAM_REQUEST_ID);

            // set dream service header
            if(context.Service.Self != null) {
                message.Headers.DreamService = context.AsPublicUri(context.Service.Self).ToString();
            }

            // check if uri is local://
            if(normalizedUri.Scheme.EqualsInvariant("local")) {
                DreamUtil.AppendHeadersToInternallyForwardedMessage(context.Request, message);
            }
            return message;
        }

        public DreamMessage PostProcess(string verb, XUri uri, XUri normalizedUri, DreamMessage message, DreamCookieJar cookies) {
            return message;
        }
    }

    internal class DreamContextCookieJarSource : Dream.Plug.ICookieJarSource {
        public DreamCookieJar CookieJar {
            get {
                var context = DreamContext.CurrentOrNull;
                return ((context != null) && (context.Service.Cookies != null)) ? context.Service.Cookies : null;
            }
        }
    }

    internal class DreamMessageDiagnosticsInterceptor : DreamMessage.IDiagnosticsInterceptor {

        //--- Methods ---
        public string Path {
            get {
                var context = DreamContext.CurrentOrNull;
                return context == null ? null : context.Uri.Path;
            }
        }

        public void AmendErrorDocument(XDoc result, DreamStatus status) {
            var context = DreamContext.CurrentOrNull;
            if((context != null) && (context.Env.Self != null)) {
                result.WithXslTransform(context.AsPublicUri(context.Env.Self).At("resources", "error.xslt").Path);
            }
            if(context != null) {
                result.Elem("uri", context.Uri);
            }

        }
    }
}
