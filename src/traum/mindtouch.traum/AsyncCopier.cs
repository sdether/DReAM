using System;
using System.IO;
using System.Threading.Tasks;

namespace MindTouch.IO {
    public class StreamMemorizer {

        public static Task<MemoryStream> Memorize(Stream source, int max) {
            var memorizer = new StreamMemorizer(source, max);
            memorizer.Copy(max);
            return memorizer.Completion.Task;
        }

        public TaskCompletionSource<MemoryStream> Completion;
        private readonly byte[] _readBuffer = new byte[16 * 1024];
        private readonly MemoryStream _target = new MemoryStream();
        private readonly Stream _source;
        private readonly int _max;

        private StreamMemorizer(Stream source, int max) {
            _source = source;
            _max = max;
        }

        private void Copy(int length) {
            Task<int>.Factory.FromAsync(_source.BeginRead, _source.EndRead, _readBuffer, 0, Math.Min(length, _max + 1), null)
                .ContinueWith(t => {
                    var read = t.Result;
                    if(read == 0) {
                        _target.Position = 0;
                        Completion.SetResult(_target);
                        return;
                    }
                    _target.Write(_readBuffer, 0, t.Result);
                    Copy(length - read);
                });
        }
    }
}
