using System;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Traum.Webclient;

namespace MindTouch.Traum.Webclient.Bridge {
    public class BridgeEndpoint : IPlugEndpoint2 {

        //--- Class Fields ---
        public static readonly BridgeEndpoint Instance = new BridgeEndpoint();

        //--- Class Constructors ---
        static BridgeEndpoint() {
            Plug2.AddEndpoint(Instance);
        }

        public bool EnableBridge { get; set; }
        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            normalized = uri;
            return EnableBridge ? int.MaxValue : 0;
        }

        public Task<DreamMessage2> Invoke(Plug2 plug, string verb, XUri uri, DreamMessage2 request, TimeSpan timeout) {
            var completion = new TaskCompletionSource<DreamMessage2>();
            Plug.New(uri.ToString(true)).Invoke(verb, MakeMessage(request), new Result<DreamMessage>(timeout)).WhenDone(
                msg => completion.SetResult(MakeMessage(msg)),
                completion.SetException
            );
            return completion.Task;
        }

        private DreamMessage MakeMessage(DreamMessage2 request) {
            return new DreamMessage(
                (Dream.DreamStatus)(int)request.Status,
                new Dream.DreamHeaders(request.Headers),
                new Dream.MimeType(request.ContentType.ToString()),
                request.ToBytes()
            );
        }

        private DreamMessage2 MakeMessage(DreamMessage request) {
            return new DreamMessage2(
                (DreamStatus)(int)request.Status,
                new DreamHeaders(request.Headers),
                new MimeType(request.ContentType.ToString()),
                request.ToBytes()
            );
        }
    }
}
