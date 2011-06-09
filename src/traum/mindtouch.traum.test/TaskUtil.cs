using System.Threading.Tasks;

namespace MindTouch.Traum.Test {
    public static class TaskUtil {
        public static Task<T> Result<T>(T value) {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }
    }
}
