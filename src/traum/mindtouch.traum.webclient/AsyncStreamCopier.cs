using System;
using System.IO;
using System.Threading.Tasks;

namespace MindTouch.Traum.Webclient {
    public class AsyncStreamCopier {

        public static Task Copy(Stream source, Stream destination, int length) {
            var copier = new AsyncStreamCopier(source, destination);
            copier.Copy(length);
            return copier.Completion.Task;
        }

        private readonly Stream _source;
        private readonly Stream _destination;
        public readonly TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
        private readonly byte[] _buffer = new byte[16 * 1024];
        private int _remaining = 0;
        private bool _doneReading = false;

        private AsyncStreamCopier(Stream source, Stream destination) {
            _source = source;
            _destination = destination;
        }

        private void Copy(int length) {
            _remaining = length;
            Read();
        }

        private void Read() {
            var chunkLength = Math.Min(_buffer.Length, _remaining);
            if(_source is MemoryStream) {
                _source.Read(_buffer, 0, chunkLength);
                Write(chunkLength);
            } else {
                Task<int>.Factory.FromAsync(_source.BeginRead, _source.EndRead, _buffer, 0, chunkLength, null)
                    .ContinueWith(t => {
                        if(t.IsFaulted) {
                            Completion.SetException(t.UnwrapFault());
                            return;
                        }
                        var read = t.Result;
                        if(read == 0) {
                            _doneReading = true;
                            return;
                        }
                        Write(read);
                    });
            }
        }

        private void Write(int length) {
            if(_destination is MemoryStream) {
                _destination.Write(_buffer, 0, length);
                if(_remaining == 0 || _doneReading) {
                    Completion.SetResult(true);
                    return;
                }
                Read();
            } else {
                Task.Factory.FromAsync(_destination.BeginWrite, _destination.EndWrite, _buffer, 0, length, null)
                    .ContinueWith(t => {
                        if(t.IsFaulted) {
                            Completion.SetException(t.UnwrapFault());
                            return;
                        }
                        _remaining -= length;
                        if(_remaining == 0 || _doneReading) {
                            Completion.SetResult(true);
                            return;
                        }
                        Read();
                    });
            }
        }
    }
}