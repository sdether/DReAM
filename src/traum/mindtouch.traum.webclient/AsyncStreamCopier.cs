using System;
using System.IO;
using System.Threading.Tasks;
using log4net;

namespace MindTouch.Traum.Webclient {
    public class AsyncStreamCopier {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

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
            var length = Math.Min(_buffer.Length, _remaining);
            if(_source is MemoryStream) {
                int read;
                try {
                    read = _source.Read(_buffer, 0, length);
                } catch(Exception e) {
                    Completion.SetException(e);
                    return;
                }
                FinishRead(read);
            } else {
                Task<int>.Factory.FromAsync(_source.BeginRead, _source.EndRead, _buffer, 0, length, null)
                    .ContinueWith(t => {
                        if(t.IsFaulted) {
                            Completion.SetException(t.UnwrapFault());
                            return;
                        }
                        var read = t.Result;
                        FinishRead(read);
                    });
            }
        }

        private void FinishRead(int read) {
            if(read == 0) {
                _log.Debug("done reading");
                _doneReading = true;
            }
            Write(read);
        }

        private void Write(int length) {
            if(_destination is MemoryStream) {
                try {
                    _destination.Write(_buffer, 0, length);
                } catch(Exception e) {
                    Completion.SetException(e);
                    return;
                }
                FinishWrite(length);
            } else {
                Task.Factory.FromAsync(_destination.BeginWrite, _destination.EndWrite, _buffer, 0, length, null)
                    .ContinueWith(t => {
                        if(t.IsFaulted) {
                            Completion.SetException(t.UnwrapFault());
                            return;
                        }
                        FinishWrite(length);
                    });
            }
        }

        private void FinishWrite(int length) {
            _remaining -= length;
            _log.DebugFormat("wrote: {0}, remaining: {1}", length, _remaining);
            if(_remaining == 0 || _doneReading) {
                _log.DebugFormat("done writing");
                Completion.SetResult(true);
                return;
            }
            Read();
        }
    }
}