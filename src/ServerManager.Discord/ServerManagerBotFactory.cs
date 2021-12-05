using ServerManagerTool.DiscordBot.Interfaces;

namespace ServerManagerTool.DiscordBot
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
