using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Dream;

namespace MindTouch.Traum.Webclient.Test {
    public static class TestEx {
        public static DreamMessage DreamMessage(string content) {
            return Dream.DreamMessage.Ok(Dream.MimeType.TEXT_UTF8, content);
        }

        public static Plug2 AsPlug2(this Plug plug) {

            // TODO: cookies?
            return Plug2.New(plug.Uri.ToString());
        }

        public static XUri ToTraumUri(this Dream.XUri uri) {
            return new XUri(uri.ToString(true));
        }

        public static Dream.XUri ToDreamUri(this XUri uri) {
            return new Dream.XUri(uri.ToString(true));
        }

        public static XUri ToTraumUri(this Plug plug) {
            return new XUri(plug.Uri.ToString(true));
        }
    }
}
