using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ServerManagerTool.Common.Utils
{
    public static class TaskUtils
    {
        public static void DoNotWait(this Task task)
        {
            // Do nothing, let the task continue.  Eliminates compiler warning about non-awaited tasks in an async method.
        }

        public static void DoNotWait(this ValueTask task)
        {
            // Do nothing, let the task continue.  Eliminates compiler warning about non-awaited tasks in an async method.
        }

        public static async Task RunOnUIThreadAsync(Action action)
        {
            var app = Application.Current;
            if (app != null)
            {
                await app.Dispatcher.InvokeAsync(action);
            }
        }

        public static readonly Task FinishedTask = Task.FromResult(true);

        public static async Task TimeoutAfterAsync(this Task task, int millisecondsDelay)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(millisecondsDelay, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        public static async Task<TResult> TimeoutAfterAsync<TResult>(this Task<TResult> task, int millisecondsDelay)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {                
                var completedTask = await Task.WhenAny(task, Task.Delay(millisecondsDelay, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }
    }
}
