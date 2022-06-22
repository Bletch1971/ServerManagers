using System;
using System.Net;
using System.Threading.Tasks;

namespace ServerManagerTool.Lib
{
    public class ServerStatusUpdateRegistration : IAsyncDisposable
    {
        public string InstallDirectory;
        public IPEndPoint LocalEndpoint;
        public IPEndPoint PublicEndpoint;
        public Action<IAsyncDisposable, ServerStatusUpdate> UpdateCallback;
        public Func<Task> UnregisterAction;

        public string ProfileId;

        public async ValueTask DisposeAsync()
        {
            await UnregisterAction();
        }
    }
}
