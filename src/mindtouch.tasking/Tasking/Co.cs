/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace MindTouch.Tasking {
    public static class Co {

        //--- Class Methods ---
        public static TResult Invoke<TResult>(CoroutineHandler<TResult> callee, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(result));
            return result;
        }

        public static TResult Invoke<T1, TResult>(CoroutineHandler<T1, TResult> callee, T1 arg1, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, result));
            return result;
        }

        public static TResult Invoke<T1, T2, TResult>(CoroutineHandler<T1, T2, TResult> callee, T1 arg1, T2 arg2, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, result));
            return result;
        }

        public static TResult Invoke<T1, T2, T3, TResult>(CoroutineHandler<T1, T2, T3, TResult> callee, T1 arg1, T2 arg2, T3 arg3, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, result));
            return result;
        }

        public static TResult Invoke<T1, T2, T3, T4, TResult>(CoroutineHandler<T1, T2, T3, T4, TResult> callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, arg4, result));
            return result;
        }

        public static TResult Invoke<T1, T2, T3, T4, T5, TResult>(CoroutineHandler<T1, T2, T3, T4, T5, TResult> callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, arg4, arg5, result));
            return result;
        }

        public static TResult Invoke<T1, T2, T3, T4, T5, T6, TResult>(CoroutineHandler<T1, T2, T3, T4, T5, T6, TResult> callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, arg4, arg5, arg6, result));
            return result;
        }

        public static TResult Invoke<T1, T2, T3, T4, T5, T6, T7, TResult>(CoroutineHandler<T1, T2, T3, T4, T5, T6, T7, TResult> callee, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, TResult result) where TResult : AResult {
            new Coroutine(callee, result).Invoke(() => callee(arg1, arg2, arg3, arg4, arg5, arg6, arg7, result));
            return result;
        }
    }
}