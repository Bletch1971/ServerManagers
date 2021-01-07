using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows;

namespace ServerManagerTool.Lib
{
    /// <summary>
    /// This class is responsible for managing all of the servers the tool knows about.
    /// </summary>
    public class ServerManager : DependencyObject
    {
        static ServerManager()
        {
            ServerManager.Instance = new ServerManager();
        }

        public static ServerManager Instance
        {
            get;
            private set;
        }

        public static readonly DependencyProperty ServersProperty = DependencyProperty.Register(nameof(Servers), typeof(SortableObservableCollection<Server>), typeof(ServerManager), new PropertyMetadata(new SortableObservableCollection<Server>()));

        public SortableObservableCollection<Server> Servers
        {
            get { return (SortableObservableCollection<Server>)GetValue(ServersProperty); }
            set { SetValue(ServersProperty, value); }
        }
      
        public ServerManager()
        {
            this.Servers.CollectionChanged += Servers_CollectionChanged;
        }

        void Servers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach(Server server in e.OldItems)
                {
                    server.Dispose();
                }
            }
        }

        public int AddFromPath(string path)
        {
            var server = Server.FromPath(path);
            if (server == null)
                return this.Servers.Count - 1;

            this.Servers.Add(server);
            return this.Servers.Count - 1;
        }  
      
        public int AddNew()
        {
            var server = Server.FromDefaults();
            if (server == null)
                return this.Servers.Count - 1;

            this.Servers.Add(server);
            return this.Servers.Count - 1;
        }

        public void Remove(Server server, bool deleteProfile)
        {
            if (server == null)
                return;

            // save the profile before deleting, just in case something needed has changed
            if (server.Profile != null)
                server.Profile.Save(false, false, null);

            if (deleteProfile)
            {
                var profileFile = server.Profile?.GetProfileFile();
                if (!string.IsNullOrWhiteSpace(profileFile) && File.Exists(profileFile))
                {
                    // set the file permissions
                    SecurityUtils.SetFileOwnershipForAllUsers(profileFile);
                    try
                    {
                        File.Delete(profileFile);
                    }
                    catch (Exception) { }
                }

                var profileIniDir = server.Profile?.GetProfileConfigDir_Old();
                if (!string.IsNullOrWhiteSpace(profileIniDir) && Directory.Exists(profileIniDir))
                {
                    // set the folder permissions
                    SecurityUtils.SetDirectoryOwnershipForAllUsers(profileIniDir);
                    try
                    {
                        Directory.Delete(profileIniDir, true);
                    }
                    catch (Exception) { }
                }
            }

            server.Runtime?.DeleteFirewallRules();
            server.Dispose();

            this.Servers.Remove(server);
        }

        public void CheckProfiles()
        {
            var serverIds = new Dictionary<string, bool>();
            foreach (var server in Servers)
            {
                if (server == null || server.Profile == null)
                    continue;

                while (serverIds.ContainsKey(server.Profile.ProfileID))
                {
                    server.Profile.ResetProfileId();
                }

                serverIds.Add(server.Profile.ProfileID, true);
            }
        }

        public void SortServers()
        {
            Servers.Sort(s => s.Profile?.SortKey);
        }
    }
}
