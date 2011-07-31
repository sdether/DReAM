using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MindTouch.Traum {
    internal static class TraumExtensions {
        public static bool IsStreamMemorized(this Stream stream) {
            return stream is MemoryStream;
        }

        public static Task<T> AsCompletedTask<T>(this T result) {
            var completion = new TaskCompletionSource<T>();
            completion.SetResult(result);
            return completion.Task;
        }

        public static Task<T> AsFaultedTask<T>(this Exception exception) {
            var completion = new TaskCompletionSource<T>();
            completion.SetException(exception);
            return completion.Task;
        }

        public static DateTime ToSafeUniversalTime(this DateTime date) {
            if(date != DateTime.MinValue && date != DateTime.MaxValue) {
                switch(date.Kind) {
                case DateTimeKind.Unspecified:
                    date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, DateTimeKind.Utc);
                    break;
                case DateTimeKind.Local:
                    date = date.ToUniversalTime();
                    break;
                }
            }
            return date;
        }

        public static Exception UnwrapFault(this Task task) {
            return task.Exception.Flatten().InnerExceptions.First();
        }
    }
}