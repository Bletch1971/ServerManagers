using QueryMaster;
using ServerManagerTool.Common.Extensions;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib;
using System;
using System.Collections.Generic;
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

        public static IList<string> HandleDiscordCommand(CommandType commandType, string serverId, string channelId, string profileIdOrAlias, CancellationToken token)
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
                        return GetServerInfo(channelId, profileIdOrAlias);
                    case CommandType.List:
                        return GetServerList(channelId);
                    case CommandType.Status:
                        return GetServerStatus(channelId, profileIdOrAlias);

                    case CommandType.Backup:
                        if (Config.Default.AllowDiscordBackup)
                            return BackupServer(channelId, profileIdOrAlias, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Restart:
                        if (Config.Default.AllowDiscordRestart)
                            return RestartServer(channelId, profileIdOrAlias, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Shutdown:
                        if (Config.Default.AllowDiscordShutdown)
                            return ShutdownServer(channelId, profileIdOrAlias, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Stop:
                        if (Config.Default.AllowDiscordStop)
                            return StopServer(channelId, profileIdOrAlias, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Start:
                        if (Config.Default.AllowDiscordStart)
                            return StartServer(channelId, profileIdOrAlias, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Update:
                        if (Config.Default.AllowDiscordUpdate)
                            return UpdateServer(channelId, profileIdOrAlias, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };

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

        private static IList<string> GetServerInfo(string channelId, string profileIdOrAlias)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Info) };
            }

            var key = string.Empty;

            try
            {
                var serverName = string.Empty;
                var serverIp = IPAddress.Loopback;
                var queryPort = 0;

                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));

                    if (serverList.IsEmpty())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                    if (!serverList.HasOne())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMultiples"), profileIdOrAlias));
                    }

                    var server = serverList.First();

                    // check if another command is being run against the profile
                    if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                    }

                    key = server.Profile.ProfileID;
                    _currentProfileCommands.Add(key, CommandType.Info);

                    switch (server.Runtime.Status)
                    {
                        case ServerStatus.Initializing:
                        case ServerStatus.Stopping:
                        case ServerStatus.Stopped:
                        case ServerStatus.Uninstalled:
                        case ServerStatus.Unknown:
                        case ServerStatus.Updating:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
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
                            response.Add($"```{info.Name}\n" +
                                $"{_globalizer.GetResourceString("DiscordBot_MapLabel")} {mapName}\n" +
                                $"{_globalizer.GetResourceString("ServerSettings_PlayersLabel")} {info.Players} / {info.MaxPlayers}```");
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
                _currentProfileCommands.Remove(key);
            }
        }

        private static IList<string> GetServerList(string channelId)
        {
            List<string> response = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase));

                response.Add($"**{_globalizer.GetResourceString("DiscordBot_CountLabel")}** {serverList.Count()}");
                foreach (var server in serverList)
                {
                    response.Add($"```{_globalizer.GetResourceString("ServerSettings_ProfileLabel")} {server.Profile.ProfileName}\n" +
                        $"{_globalizer.GetResourceString("ServerSettings_ProfileIdLabel")} {server.Profile.ProfileID}\n" +
                        (string.IsNullOrWhiteSpace(server.Profile.DiscordAlias) ? "" : $"{_globalizer.GetResourceString("ServerSettings_DiscordAliasLabel")} {server.Profile.DiscordAlias}\n") +
                        $"{_globalizer.GetResourceString("ServerSettings_ServerNameLabel")} {server.Profile.ServerName}```");
                }
            }).Wait();

            return response;
        }

        private static IList<string> GetServerStatus(string channelId, string profileIdOrAlias)
        {
            List<string> response = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(profileIdOrAlias)
                    || string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                    || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));

                response.Add($"**{_globalizer.GetResourceString("DiscordBot_CountLabel")}** {serverList.Count()}");
                foreach (var server in serverList)
                {
                    response.Add($"```{_globalizer.GetResourceString("ServerSettings_ProfileLabel")} {server.Profile.ProfileName}\n" +
                        $"{_globalizer.GetResourceString("ServerSettings_ProfileIdLabel")} {server.Profile.ProfileID}\n" +
                        (string.IsNullOrWhiteSpace(server.Profile.DiscordAlias) ? "" : $"{_globalizer.GetResourceString("ServerSettings_DiscordAliasLabel")} {server.Profile.DiscordAlias}\n") +
                        $"{_globalizer.GetResourceString("ServerSettings_ServerNameLabel")} {server.Profile.ServerName}\n" +
                        $"{_globalizer.GetResourceString("ServerSettings_StatusLabel")} {server.Runtime.StatusString}\n" +
                        $"{_globalizer.GetResourceString("ServerSettings_AvailabilityLabel")} {_globalizer.GetResourceString($"ServerSettings_Availability_{server.Runtime.Availability}")}```");
                }
            }).Wait();

            return response;
        }

        private static IList<string> BackupServer(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Backup) };
            }

            var key = string.Empty;

            ServerProfileSnapshot profile = null;
            Task task = null;

            try
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));

                    if (serverList.IsEmpty())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                    if (!serverList.HasOne())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMultiples"), profileIdOrAlias));
                    }

                    var server = serverList.First();

                    if (!server.Profile.AllowDiscordBackup)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Backup, server.Profile.ProfileName));
                    }

                    // check if another command is being run against the profile
                    if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                    }

                    key = server.Profile.ProfileID;
                    _currentProfileCommands.Add(key, CommandType.Backup);

                    switch (server.Runtime.Status)
                    {
                        case ServerStatus.Initializing:
                        case ServerStatus.Stopping:
                        case ServerStatus.Uninstalled:
                        case ServerStatus.Unknown:
                        case ServerStatus.Updating:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
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
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.First(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                                && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                                || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));
                            server.Runtime.UpdateServerStatus(serverStatus, true);
                        }).Wait();
                    }
                };

                task = Task.Run(() =>
                {
                    app.PerformProfileBackup(profile, token);
                    _currentProfileCommands.Remove(key);
                });

                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_BackupRequested"), profile.ServerName));

                return response;
            }
            finally
            {
                if (task is null)
                {
                    _currentProfileCommands.Remove(key);
                }
            }
        }

        private static IList<string> RestartServer(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Restart) };
            }

            var key = string.Empty;

            ServerProfileSnapshot profile = null;
            Task task = null;

            try
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));

                    if (serverList.IsEmpty())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                    if (!serverList.HasOne())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMultiples"), profileIdOrAlias));
                    }

                    var server = serverList.First();

                    if (!server.Profile.AllowDiscordRestart)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Restart, server.Profile.ProfileName));
                    }

                    // check if another command is being run against the profile
                    if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                    }

                    key = server.Profile.ProfileID;
                    _currentProfileCommands.Add(key, CommandType.Restart);

                    switch (server.Runtime.Status)
                    {
                        case ServerStatus.Initializing:
                        case ServerStatus.Stopping:
                        case ServerStatus.Uninstalled:
                        case ServerStatus.Unknown:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));

                        case ServerStatus.Updating:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
                    }

                    profile = ServerProfileSnapshot.Create(server.Profile);
                    profile.AutoRestartIfShutdown = true;
                }).Wait();

                List<string> response = new List<string>();

                var app = new ServerApp(true)
                {
                    DeleteOldServerBackupFiles = !Config.Default.AutoBackup_EnableBackup,
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = ServerProcessType.Restart,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.First(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                                && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                                || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));
                            server.Runtime.UpdateServerStatus(serverStatus, true);
                        }).Wait();
                    }
                };

                task = Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, true, false, false, false, token);
                    _currentProfileCommands.Remove(key);
                });

                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_RestartRequested"), profile.ServerName));

                return response;
            }
            finally
            {
                if (task is null)
                {
                    _currentProfileCommands.Remove(key);
                }
            }
        }

        private static IList<string> ShutdownServer(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Shutdown) };
            }

            var key = string.Empty;

            ServerProfileSnapshot profile = null;
            Task task = null;

            try
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));

                    if (serverList.IsEmpty())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                    if (!serverList.HasOne())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMultiples"), profileIdOrAlias));
                    }

                    var server = serverList.First();

                    if (!server.Profile.AllowDiscordShutdown)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Shutdown, server.Profile.ProfileName));
                    }

                    // check if another command is being run against the profile
                    if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                    }

                    key = server.Profile.ProfileID;
                    _currentProfileCommands.Add(key, CommandType.Shutdown);

                    switch (server.Runtime.Status)
                    {
                        case ServerStatus.Initializing:
                        case ServerStatus.Stopping:
                        case ServerStatus.Stopped:
                        case ServerStatus.Uninstalled:
                        case ServerStatus.Unknown:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));

                        case ServerStatus.Updating:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
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
                    ServerProcess = ServerProcessType.Shutdown,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.First(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                                && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                                || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));
                            server.Runtime.UpdateServerStatus(serverStatus, true);
                        }).Wait();
                    }
                };

                task = Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, false, false, false, false, token);
                    _currentProfileCommands.Remove(key);
                });

                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ShutdownRequested"), profile.ServerName));

                return response;
            }
            finally
            {
                if (task is null)
                {
                    _currentProfileCommands.Remove(key);
                }
            }
        }

        private static IList<string> StopServer(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Stop) };
            }

            var key = string.Empty;

            ServerProfileSnapshot profile = null;
            Task task = null;

            try
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));

                    if (serverList.IsEmpty())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                    if (!serverList.HasOne())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMultiples"), profileIdOrAlias));
                    }

                    var server = serverList.First();

                    if (!server.Profile.AllowDiscordStop)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Stop, server.Profile.ProfileName));
                    }

                    // check if another command is being run against the profile
                    if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                    }

                    key = server.Profile.ProfileID;
                    _currentProfileCommands.Add(key, CommandType.Stop);

                    switch (server.Runtime.Status)
                    {
                        case ServerStatus.Initializing:
                        case ServerStatus.Stopping:
                        case ServerStatus.Stopped:
                        case ServerStatus.Uninstalled:
                        case ServerStatus.Unknown:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));

                        case ServerStatus.Updating:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
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
                    ServerProcess = ServerProcessType.Stop,
                    ShutdownInterval = 0,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.First(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                                && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                                || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));
                            server.Runtime.UpdateServerStatus(serverStatus, true);
                        }).Wait();
                    }
                };

                task = Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, false, false, false, false, token);
                    _currentProfileCommands.Remove(key);
                });

                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_StopRequested"), profile.ServerName));

                return response;
            }
            finally
            {
                if (task is null)
                {
                    _currentProfileCommands.Remove(key);
                }
            }
        }

        private static IList<string> StartServer(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Start) };
            }

            var key = string.Empty;

            ServerProfileSnapshot profile = null;
            Task task = null;

            try
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));

                    if (serverList.IsEmpty())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                    if (!serverList.HasOne())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMultiples"), profileIdOrAlias));
                    }

                    var server = serverList.First();

                    if (!server.Profile.AllowDiscordStart)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Start, server.Profile.ProfileName));
                    }

                    // check if another command is being run against the profile
                    if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                    }

                    key = server.Profile.ProfileID;
                    _currentProfileCommands.Add(key, CommandType.Start);

                    switch (server.Runtime.Status)
                    {
                        case ServerStatus.Initializing:
                        case ServerStatus.Stopping:
                        case ServerStatus.Running:
                        case ServerStatus.Uninstalled:
                        case ServerStatus.Unknown:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));

                        case ServerStatus.Updating:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
                    }

                    profile = ServerProfileSnapshot.Create(server.Profile);
                    profile.AutoRestartIfShutdown = true;
                }).Wait();

                List<string> response = new List<string>();

                var app = new ServerApp(true)
                {
                    DeleteOldServerBackupFiles = !Config.Default.AutoBackup_EnableBackup,
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = ServerProcessType.Restart,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.First(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                                && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                                || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));
                            server.Runtime.UpdateServerStatus(serverStatus, true);
                        }).Wait();
                    }
                };

                task = Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, true, false, false, false, token);
                    _currentProfileCommands.Remove(key);
                });

                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_StartRequested"), profile.ServerName));

                return response;
            }
            finally
            {
                if (task is null)
                {
                    _currentProfileCommands.Remove(key);
                }
            }
        }

        private static IList<string> UpdateServer(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Update) };
            }

            var key = string.Empty;

            ServerProfileSnapshot profile = null;
            bool performRestart = false;
            Task task = null;

            try
            {
                TaskUtils.RunOnUIThreadAsync(() =>
                {
                    var serverList = ServerManager.Instance.Servers.Where(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));

                    if (serverList.IsEmpty())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                    if (!serverList.HasOne())
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMultiples"), profileIdOrAlias));
                    }

                    var server = serverList.First();

                    if (!server.Profile.AllowDiscordUpdate)
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Update, server.Profile.ProfileName));
                    }

                    // check if another command is being run against the profile
                    if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                    {
                        throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                    }

                    key = server.Profile.ProfileID;
                    _currentProfileCommands.Add(key, CommandType.Update);

                    switch (server.Runtime.Status)
                    {
                        case ServerStatus.Initializing:
                        case ServerStatus.Stopping:
                        case ServerStatus.Unknown:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));

                        case ServerStatus.Running:
                            performRestart = true;
                            break;

                        case ServerStatus.Updating:
                            throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
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
                            var server = ServerManager.Instance.Servers.First(s => string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                                && (string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                                || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)));
                            server.Runtime.UpdateServerStatus(serverStatus, true);
                        }).Wait();
                    }
                };

                task = Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, performRestart, true, false, false, token);
                    _currentProfileCommands.Remove(key);
                });

                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_UpdateRequested"), profile.ServerName));

                return response;
            }
            finally
            {
                if (task is null)
                {
                    _currentProfileCommands.Remove(key);
                }
            }
        }
    }
}
