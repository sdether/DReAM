using System;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Traum {
    public static class Plug2Ex {

        public static Task<XDoc> GetDocument(this Plug2 plug, TimeSpan timeout) {
            var tcs = new TaskCompletionSource<XDoc>();
            plug.Invoke(Verb.GET, DreamMessage2.Ok(), timeout).ContinueWith(t => {
                if(t.IsFaulted) {
                    tcs.SetException(t.Exception);
                } else if(!t.Result.IsSuccessful) {
                    tcs.SetException(new DreamResponseException(t.Result));
                } else {
                    tcs.SetResult(t.Result.ToDocument());
                }
            });
            return tcs.Task;
        }

        public static Task<XDoc> GetDocument(this Plug2 plug) {
            var tcs = new TaskCompletionSource<XDoc>();
            plug.Invoke(Verb.GET, DreamMessage2.Ok(), Plug2.DEFAULT_TIMEOUT).ContinueWith(t => {
                if(t.IsFaulted) {
                    tcs.SetException(t.Exception);
                } else if(!t.Result.IsSuccessful) {
                    tcs.SetException(new DreamResponseException(t.Result));
                } else {
                    tcs.SetResult(t.Result.ToDocument());
                }
            });
            return tcs.Task;
        }

        public static Task<XDoc> GetAsync(this Plug2 plug) {
            return GetDocument(plug, Plug2.DEFAULT_TIMEOUT);
        }

        public static Plug2 AsPlug2(this Plug plug) {
            return new Plug2(plug.Uri, plug.Timeout, plug.Headers, null, null, plug.Credentials, plug.CookieJar, plug.MaxAutoRedirects);
        }

    }
}