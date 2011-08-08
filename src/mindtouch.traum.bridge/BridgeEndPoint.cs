using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Tasking;
using Traum = MindTouch.Traum;
using Dream = MindTouch.Dream;

namespace MindTouch.Traum.Bridge {
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

        public Task<DreamMessage2> Invoke(Plug2 plug, string verb, Traum.XUri uri, DreamMessage2 request, TimeSpan timeout) {
            var completion = new TaskCompletionSource<DreamMessage2>();
            Plug.New(uri.ToString(true)).Invoke(verb, MakeMessage(request), new Result<DreamMessage>(timeout)).WhenDone(
                msg => completion.SetResult(MakeMessage(msg)),
                completion.SetException
            );
            return completion.Task;
        }

        private DreamMessage MakeMessage(DreamMessage2 request) {
            var msg = new DreamMessage((Dream.DreamStatus)(int)request.Status,MakeHeaders(request.Headers),new Dream.MimeType(request.ContentType.ToString()),request.ToBytes());
            return msg;
        }

        private Dream.DreamHeaders MakeHeaders(Traum.DreamHeaders headers) {
            throw new NotImplementedException();
        }

        private DreamMessage2 MakeMessage(DreamMessage request) {
            var msg = new DreamMessage2((Traum.DreamStatus)(int)request.Status,MakeHeaders(request.Headers),);
            return msg;
        }

        private Traum.DreamHeaders MakeHeaders(Dream.DreamHeaders headers) {
            throw new NotImplementedException();
        }

    }
}
