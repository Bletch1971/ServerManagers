using ServerManagerTool.Utils;
using System;
using System.Collections.Generic;
using System.Net;

namespace ServerManagerTool.Lib
{
    public class ServerProfileSnapshot
    {
        private ServerProfileSnapshot()
        {
        }

        public string ProfileId;
        public string ProfileName;
        public string ServerName;
        public string InstallDirectory;
        public string GameFile;
        public string AdminPassword;
        public IPAddress ServerIPAddress;
        public int ServerPort;
        public int ServerPeerPort;
        public int QueryPort;
        public string ServerMap;
        public List<string> ServerModIds;
        public bool RconEnabled;
        public int RconPort;
        public string RconPassword;
        public int MaxPlayerCount;

        public string MOTD;
        public bool MOTDIntervalEnabled;
        public int MOTDInterval;

        public string AppId;
        public string AppIdServer;
        public bool UseTestlive;
        public string BranchName;
        public string BranchPassword;

        public string SchedulerKey;
        public bool EnableAutoBackup;
        public bool EnableAutoUpdate;
        public bool EnableAutoShutdown1;
        public bool RestartAfterShutdown1;
        public bool UpdateAfterShutdown1;
        public bool EnableAutoShutdown2;
        public bool RestartAfterShutdown2;
        public bool UpdateAfterShutdown2;
        public bool AutoRestartIfShutdown;

        public bool ServerUpdated;
        public string LastInstalledVersion;
        public DateTime LastStarted;

        public static ServerProfileSnapshot Create(ServerProfile profile)
        {
            return new ServerProfileSnapshot
            {
                ProfileId = profile.ProfileID,
                ProfileName = profile.ProfileName,
                ServerName = profile.ServerName,
                InstallDirectory = profile.InstallDirectory,
                GameFile = profile.GetServerWorldFile(),
                AdminPassword = profile.AdminPassword,
                ServerIPAddress = string.IsNullOrWhiteSpace(profile.ServerIP) || !IPAddress.TryParse(profile.ServerIP.Trim(), out IPAddress ipAddress) ? IPAddress.Loopback : ipAddress,
                ServerPort = profile.ServerPort,
                ServerPeerPort = profile.ServerPeerPort,
                QueryPort = profile.QueryPort,
                ServerMap = ServerProfile.GetProfileMapName(profile),
                ServerModIds = ModUtils.GetModIdList(profile.ServerModIds),
                RconEnabled = profile.RconEnabled,
                RconPort = profile.RconPort,
                RconPassword = profile.RconPassword,
                MaxPlayerCount = profile.MaxPlayers,

                MOTD = profile.MOTD,
                MOTDIntervalEnabled = profile.MOTDIntervalEnabled && !string.IsNullOrWhiteSpace(profile.MOTD),
                MOTDInterval = Math.Max(1, Math.Min(int.MaxValue, profile.MOTDInterval)),

                AppId = profile.UseTestlive ? Config.Default.AppId_Testlive : Config.Default.AppId,
                AppIdServer = profile.UseTestlive ? Config.Default.AppIdServer_Testlive : Config.Default.AppIdServer,
                UseTestlive = profile.UseTestlive,
                BranchName = profile.BranchName,
                BranchPassword = profile.BranchPassword,

                SchedulerKey = profile.GetProfileKey(),
                EnableAutoBackup = profile.EnableAutoBackup,
                EnableAutoUpdate = profile.EnableAutoUpdate,
                EnableAutoShutdown1 = profile.EnableAutoShutdown1,
                RestartAfterShutdown1 = profile.RestartAfterShutdown1,
                UpdateAfterShutdown1 = profile.UpdateAfterShutdown1,
                EnableAutoShutdown2 = profile.EnableAutoShutdown2,
                RestartAfterShutdown2 = profile.RestartAfterShutdown2,
                UpdateAfterShutdown2 = profile.UpdateAfterShutdown2,
                AutoRestartIfShutdown = profile.AutoRestartIfShutdown,

                ServerUpdated = false,
                LastInstalledVersion = profile.LastInstalledVersion ?? new Version(0, 0).ToString(),
                LastStarted = profile.LastStarted,
            };
        }

        public void Update(ServerProfile profile)
        {
            profile.LastInstalledVersion = LastInstalledVersion;
            profile.LastStarted = LastStarted;
        }
    }
}
