using System;
using System.Threading.Tasks;
using MindTouch.Dream;
using MindTouch.Tasking;
using log4net;

namespace MindTouch.Traum.Webclient.Test {
    public class LocalEndpointBridge : IPlugEndpoint {

        //--- Class Fields ---
        private static readonly LocalEndpointBridge _instance = new LocalEndpointBridge();
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Constructors ---
        static LocalEndpointBridge() {
            Plug.AddEndpoint(_instance);
        }

        //--- Class Methods ---
        public static void Init() {}

        public int GetScoreWithNormalizedUri(XUri uri, out XUri normalized) {
            _log.DebugFormat("considering uri: {0}", uri);
            normalized = uri;
            return uri.Scheme == "local" ? int.MaxValue : 0;
        }

        public Task<DreamMessage> Invoke(Plug plug, string verb, XUri uri, DreamMessage request, TimeSpan timeout) {
            _log.DebugFormat("routing local call to Dream for uri: {0}", uri);
            var completion = new TaskCompletionSource<DreamMessage>();
            Dream.Plug.New(uri.ToString(true)).Invoke(verb, MakeMessage(request), new Result<Dream.DreamMessage>(timeout)).WhenDone(
                msg => completion.SetResult(MakeMessage(msg)),
                completion.SetException
            );
            return completion.Task;
        }

        private Dream.DreamMessage MakeMessage(DreamMessage request) {
            return new Dream.DreamMessage(
                (Dream.DreamStatus)(int)request.Status,
                new Dream.DreamHeaders(request.Headers),
                new Dream.MimeType(request.ContentType.ToString()),
                request.ToBytes()
            );
        }

        private DreamMessage MakeMessage(Dream.DreamMessage request) {
            return new DreamMessage(
                (DreamStatus)(int)request.Status,
                new DreamHeaders(request.Headers),
                new MimeType(request.ContentType.ToString()),
                request.ToBytes()
            );
        }
    }
}
