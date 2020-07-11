using System;
using System.Threading.Tasks;

namespace ServerManagerTool.Plugin.Discord
{
    internal static class TaskUtils
    {
        public static readonly Task FinishedTask = Task.FromResult(true);

        public static void DoNotWait(this Task task)
        {
            // Do nothing, let the task continue.  Eliminates compiler warning about non-awaited tasks in an async method.
        }
    }
}
