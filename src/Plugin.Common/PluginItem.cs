namespace ServerManagerTool.Plugin.Common
{
    public sealed class PluginItem
    {
        internal PluginItem()
        {
        }

        public IPlugin Plugin
        {
            get;
            set;
        }

        public string PluginFile
        {
            get;
            set;
        }

        public string PluginType
        {
            get;
            set;
        }
    }
}
