using System.IO;
using System.Threading.Tasks;

namespace MindTouch.Traum.Webclient {

    public static class AsyncStreamUtils {
        public static Task<MemoryStream> MemorizeAsync(this Stream source, int max) {
            var destination = new MemoryStream();
            var completion = new TaskCompletionSource<MemoryStream>();
            AsyncStreamCopier.Copy(source, destination, max).ContinueWith(t => {
                if(t.IsFaulted) {
                    completion.SetException(t.UnwrapFault());
                    return;
                }
                destination.Seek(0, SeekOrigin.Begin);
                completion.SetResult(destination);
            });
            return completion.Task;
        }

        public static Task CopyAsync(this Stream source, Stream destination, int length) {
            return AsyncStreamCopier.Copy(source, destination, length);
        }
    }
}
