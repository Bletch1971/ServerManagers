using QueryMaster;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Utils
{
    internal static class DiscordBotHelper
    {
        private static readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private static bool _runningCommand = false;

        private static readonly Dictionary<string, CommandType> _currentProfileCommands = new Dictionary<string, CommandType>();

        public static bool HasRunningCommands => _currentProfileCommands.Count > 0;

        public static IList<string> HandleDiscordCommand(CommandType commandType, string serverId, string channelId, string profileId)
        {
            // check if incoming values are valid
            if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(channelId))
                return null;

            // check if the server ids match
            if (!serverId.Equals(Config.Default.DiscordBotServerId))
                return new List<string>();

            if (_runningCommand)
                return new List<string> { _globalizer.GetResourceString("DiscordBot_CommandRunning") };
            _runningCommand = true;

            try
            {
                switch (commandType)
                {
                    case CommandType.Info:
                        return GetServerInfo(channelId, profileId);
                    case CommandType.List:
                        return GetServerList(channelId);
                    case CommandType.Status:
                        return GetServerStatus(channelId, profileId);

                    case CommandType.Backup:
                        return BackupServer(channelId, profileId);
                    case CommandType.Shutdown:
                        return ShutdownServer(channelId, profileId);
                    case CommandType.Stop:
                        return StopServer(channelId, profileId);
                    case CommandType.Start:
                        return StartServer(channelId, profileId);
                    case CommandType.Update:
                        return UpdateServer(channelId, profileId);

                    default:
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), commandType) };
                }
            }
            catch (Exception ex)
            {
                var message = ex.InnerException is null ? ex.Message : ex.InnerException.Message;
                return new string[] { message };
            }
            finally
            {
                _runningCommand = false;
            }
        }

        public static string HandleTranslation(string translationKey)
        {
            return string.IsNullOrWhiteSpace(translationKey) ? string.Empty : _globalizer.GetResourceString(translationKey) ?? translationKey;
        }

        private static IList<string> GetServerInfo(string channelId, string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Info) };
            }

            // check if another command is being run against the profile
            if (_currentProfileCommands.ContainsKey(profileId))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[profileId], profileId) };
            }
            _currentProfileCommands.Add(profileId, CommandType.Info);

            try
            {
                var serverName = string.Empty;
                var serverIp = IPAddress.Loopback;
                var queryPort = 0;

                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var server = ServerManager.Instance.Servers.FirstOrDefault(s => Equals(channelId, s.Profile.DiscordChannelId) && Equals(profileId, s.Profile.ProfileID));

                    if (server is null)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileId));
                    }

                    serverName = server.Profile.ServerName;
                    if (!string.IsNullOrWhiteSpace(server.Profile.ServerIP))
                    {
                        IPAddress.TryParse(server.Profile.ServerIP, out serverIp);
                    }
                    queryPort = server.Profile.QueryPort;
                }).Wait();

                List<string> response = new List<string>();

                try
                {
                    using (var gameServer = ServerQuery.GetServerInstance(EngineType.Source, new IPEndPoint(serverIp, queryPort)))
                    {
                        var info = gameServer?.GetInfo();
                        if (info is null)
                        {
                            response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_InfoFailed"), serverName));
                        }
                        else
                        {
                            var mapName = _globalizer.GetResourceString($"Map_{info.Map}") ?? info.Map;
                            response.Add($"```{info.Name}\n{_globalizer.GetResourceString("DiscordBot_MapLabel")} {mapName}\n{_globalizer.GetResourceString("ServerSettings_PlayersLabel")} {info.Players} / {info.MaxPlayers}```");
                        }
                    }
                }
                catch (Exception)
                {
                    response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_InfoFailed"), serverName));
                }

                return response;
            }
            finally
            {
                _currentProfileCommands.Remove(profileId);
            }
        }

        private static IList<string> GetServerList(string channelId)
        {
            List<string> response = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => Equals(channelId, s.Profile.DiscordChannelId));

                response.Add($"**{_globalizer.GetResourceString("DiscordBot_CountLabel")}** {serverList.Count()}");
                foreach (var server in serverList)
                {
                    response.Add($"```{_globalizer.GetResourceString("ServerSettings_ProfileIdLabel")} {server.Profile.ProfileID}\n{_globalizer.GetResourceString("ServerSettings_ProfileLabel")} {server.Profile.ProfileName}\n{_globalizer.GetResourceString("ServerSettings_ServerNameLabel")} {server.Profile.ServerName}```");
                }
            }).Wait();

            return response;
        }

        private static IList<string> GetServerStatus(string channelId, string profileId)
        {
            List<string> response = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => Equals(channelId, s.Profile.DiscordChannelId) && (string.IsNullOrWhiteSpace(profileId) || Equals(profileId, s.Profile.ProfileID)));

                response.Add($"**{_globalizer.GetResourceString("DiscordBot_CountLabel")}** {serverList.Count()}");
                foreach (var server in serverList)
                {
                    response.Add($"```{_globalizer.GetResourceString("ServerSettings_ProfileLabel")} {server.Profile.ProfileName}\n{_globalizer.GetResourceString("ServerSettings_ServerNameLabel")} {server.Profile.ServerName}\n{_globalizer.GetResourceString("ServerSettings_StatusLabel")} {server.Runtime.StatusString}\n{_globalizer.GetResourceString("ServerSettings_AvailabilityLabel")} {_globalizer.GetResourceString($"ServerSettings_Availability_{server.Runtime.Availability}")}```");
                }
            }).Wait();

            return response;
        }

        private static IList<string> BackupServer(string channelId, string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Backup) };
            }

            // check if another command is being run against the profile
            if (_currentProfileCommands.ContainsKey(profileId))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[profileId], profileId) };
            }
            _currentProfileCommands.Add(profileId, CommandType.Backup);

            ServerProfileSnapshot profile = null;
            Task task = null;

            try
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var server = ServerManager.Instance.Servers.FirstOrDefault(s => Equals(channelId, s.Profile.DiscordChannelId) && Equals(profileId, s.Profile.ProfileID));

                    if (server is null)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileId));
                    }

                    profile = ServerProfileSnapshot.Create(server.Profile);
                }).Wait();

                List<string> response = new List<string>();

                var app = new ServerApp(true)
                {
                    DeleteOldServerBackupFiles = !Config.Default.AutoBackup_EnableBackup,
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = ServerProcessType.Backup,
                };

                task = Task.Run(() =>
                {
                    app.PerformProfileBackup(profile);
                    _currentProfileCommands.Remove(profileId);
                });

                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_BackupRequested"), profile.ServerName));

                return response;
            }
            finally
            {
                if (task is null)
                {
                    _currentProfileCommands.Remove(profileId);
                }
            }
        }

        private static IList<string> ShutdownServer(string channelId, string profileId)
        {
            return new List<string>() { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), CommandType.Shutdown) };
        }

        private static IList<string> StopServer(string channelId, string profileId)
        {
            return new List<string>() { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), CommandType.Stop) };
        }

        private static IList<string> StartServer(string channelId, string profileId)
        {
            return new List<string>() { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), CommandType.Start) };
        }

        private static IList<string> UpdateServer(string channelId, string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Update) };
            }

            // check if another command is being run against the profile
            if (_currentProfileCommands.ContainsKey(profileId))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[profileId], profileId) };
            }
            _currentProfileCommands.Add(profileId, CommandType.Update);

            ServerProfileSnapshot profile = null;
            bool performRestart = false;
            Task task = null;

            try
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var server = ServerManager.Instance.Servers.FirstOrDefault(s => Equals(channelId, s.Profile.DiscordChannelId) && Equals(profileId, s.Profile.ProfileID));

                    if (server is null)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileId));
                    }

                    switch (server.Runtime.Status)
                    {
                        case ServerStatus.Initializing:
                        case ServerStatus.Stopping:
                        case ServerStatus.Unknown:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), profileId, ServerRuntime.GetServerStatusString(ServerStatus.Stopped)));

                        case ServerStatus.Running:
                            performRestart = true;
                            break;

                        case ServerStatus.Updating:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), profileId));
                    }

                    profile = ServerProfileSnapshot.Create(server.Profile);
                }).Wait();

                List<string> response = new List<string>();

                var app = new ServerApp(true)
                {
                    DeleteOldServerBackupFiles = !Config.Default.AutoBackup_EnableBackup,
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = ServerProcessType.Update,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) => 
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => Equals(channelId, s.Profile.DiscordChannelId) && Equals(profileId, s.Profile.ProfileID));
                            server.Runtime.UpdateServerStatus(serverStatus, true);
                        }).Wait();
                    }
                };

                task = Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, performRestart, true, false, CancellationToken.None);
                    _currentProfileCommands.Remove(profileId);
                });

                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_UpdateRequested"), profile.ServerName));

                return response;
            }
            finally
            {
                if (task is null)
                {
                    _currentProfileCommands.Remove(profileId);
                }
            }
        }
    }
}
