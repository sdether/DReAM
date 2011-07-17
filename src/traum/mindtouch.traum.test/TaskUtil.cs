using System.Threading.Tasks;

namespace MindTouch.Traum.Test {
    public static class TaskUtil {
        public static Task<T> AsCompletedTask<T>(this T value) {
            return TaskEx.FromResult(value);
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
