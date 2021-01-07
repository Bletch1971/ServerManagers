using System;

namespace ServerManagerTool.Common.Lib
{
    public class CommandListener : IDisposable
    {
        public Action<ConsoleCommand> Callback { get; set; }
        public Action<CommandListener> DisposeAction { get; set; }

        public void Dispose()
        {
            DisposeAction(this);
        }
    }
}
