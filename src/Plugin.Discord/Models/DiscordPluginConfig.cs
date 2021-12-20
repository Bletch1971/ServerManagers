using System;
using System.Runtime.Serialization;

namespace ServerManagerTool.Plugin.Discord
{
    [DataContract]
    internal sealed class DiscordPluginConfig : Bindable
    {
        public DiscordPluginConfig()
            : base()
        {
            PluginCallUrlLast = DateTime.MinValue;
            ConfigProfiles = new ObservableList<ConfigProfile>();
        }

        [DataMember]
        public DateTime PluginCallUrlLast
        {
            get;
            set;
        }

        [DataMember]
        public ObservableList<ConfigProfile> ConfigProfiles
        {
            get { return Get<ObservableList<ConfigProfile>>(); }
            private set { Set(value); }
        }

        public override bool HasAnyChanges => base.HasChanges || ConfigProfiles.HasAnyChanges;
    }
}
