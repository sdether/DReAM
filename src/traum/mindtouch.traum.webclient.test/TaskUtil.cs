using System.Threading.Tasks;

namespace MindTouch.Traum.Webclient.Test {
    public static class TaskUtil {
        public static Task<T> AsCompletedTask<T>(this T value) {
            var completion = new TaskCompletionSource<T>();
            completion.SetResult(value);
            return completion.Task;
        }

        public static Task Block(this Task task) {
            task.Wait(-1);
            return task;
        }
        
        public static Task<T> Block<T>(this Task<T> task) {
            task.Wait(-1);
            return task;
        }
    }
}
