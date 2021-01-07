using ServerManagerTool.Common.Interfaces;
using System;
using System.Net;
using System.Threading.Tasks;

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
        public string GameFile;

        public async Task DisposeAsync()
        {
            await UnregisterAction();
        }
    }
}
