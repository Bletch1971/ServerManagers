using System;
using System.Net;
using System.Threading.Tasks;
using ServerManagerTool.Common.Interfaces;

namespace ServerManagerTool.Lib
{
    public class ServerStatusUpdateRegistration : IAsyncDisposable
    {
        public string InstallDirectory;
        public IPEndPoint LocalEndpoint;
        public IPEndPoint SteamEndpoint;
        public Action<IAsyncDisposable, ServerStatusUpdate> UpdateCallback;
        public Func<Task> UnregisterAction;

        public string ProfileId;

        public async Task DisposeAsync()
        {
            await UnregisterAction();
        }
    }
}
