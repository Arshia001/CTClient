using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Network
{
    public static class TaskExtensions
    {
        public static void Then(this Task Task, Action<Task> OnCompleted, Action<Exception> OnFailed)
        {
            Task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                    OnFailed(new TaskCanceledException());
                else if (t.IsFaulted)
                    OnFailed(t.Exception);
                else
                    OnCompleted(t);
            });
        }

        public static void Then<T>(this Task<T> Task, Action<Task<T>> OnCompleted, Action<Exception> OnFailed)
        {
            Task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                    OnFailed(new TaskCanceledException());
                else if (t.IsFaulted)
                    OnFailed(t.Exception);
                else
                    OnCompleted(t);
            });
        }

        public static Task<TResult> ConvertResult<T, TResult>(this Task<T> Task, Func<T, TResult> OnConvert)
        {
            var TCS = new TaskCompletionSource<TResult>();

            Task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                    TCS.SetCanceled();
                else if (t.IsFaulted)
                    TCS.SetException(t.Exception);
                else
                    TCS.SetResult(OnConvert(t.Result));
            });

            return TCS.Task;
        }
    }
}
