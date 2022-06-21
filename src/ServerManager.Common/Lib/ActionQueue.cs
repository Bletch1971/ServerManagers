using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ServerManagerTool.Common.Lib
{
    /// <summary>
    /// This class ensures the following:
    /// 1. All work items run in the order posted.
    /// 2. Work items run on a background thread.
    /// 3. Work items do not overlap
    /// 4. If requested, the completion status of a work item is returned via a task.
    /// </summary>
    public class ActionQueue : IAsyncDisposable
    {
        public ActionBlock<Action> workQueue;

        public ActionQueue(TaskScheduler scheduler = null)
        {
            this.workQueue = new ActionBlock<Action>(a => a.Invoke(), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, TaskScheduler = scheduler ?? TaskScheduler.Default });
        }
       
        public Task<T> PostAction<T>(Func<T> action)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            this.workQueue.Post(() => 
                {                    
                    try
                    {
                        var result = action.Invoke();
                        Task.Run(() => tcs.TrySetResult(result));
                    }
                    catch(Exception ex)
                    {
                        Task.Run(() => tcs.TrySetException(ex));
                    }
                });
            return tcs.Task;
        }

        public Task PostAction(Action action)
        {
            return PostAction(() => { action.Invoke(); return true; });
        }

        public async ValueTask DisposeAsync()
        {
            await PostAction(() => this.workQueue.Complete());
        }
    }
}
