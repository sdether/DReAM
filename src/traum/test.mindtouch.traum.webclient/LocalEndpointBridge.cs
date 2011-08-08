using System;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Tasking;
using log4net;

namespace MindTouch.Traum.Webclient.Test {
    public class LocalEndpointBridge : IPlugEndpoint2 {

        //--- Class Fields ---
        private static readonly LocalEndpointBridge _instance = new LocalEndpointBridge();
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Constructors ---
        static LocalEndpointBridge() {
            Plug2.AddEndpoint(_instance);
        }

        //--- Class Methods ---
        public static void Init() {}

        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            _log.DebugFormat("considering uri: {0}", uri);
            normalized = uri;
            return uri.Scheme == "local" ? int.MaxValue : 0;
        }

        public Task<DreamMessage2> Invoke(Plug2 plug, string verb, XUri uri, DreamMessage2 request, TimeSpan timeout) {
            _log.DebugFormat("routing local call to Dream for uri: {0}", uri);
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
