using ServerManagerTool.Discord.Interfaces;

namespace ServerManagerTool.Discord
{
    public static class ServerManagerBotFactory
    {
        private static IServerManagerBot _serverManagerBot;

        public static IServerManagerBot GetServerManagerBot()
        {
            if (_serverManagerBot is null)
            {
                _serverManagerBot = new ServerManagerBot(); 
            }

            return _serverManagerBot;
        }
    }
}
