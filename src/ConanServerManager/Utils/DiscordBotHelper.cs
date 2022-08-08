using QueryMaster;
using ServerManagerTool.Common.Enums;
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
                        return GetServerInfo(channelId, profileIdOrAlias, token);
                    case CommandType.List:
                        return GetServerList(channelId, token);
                    case CommandType.Status:
                        return GetServerStatus(channelId, profileIdOrAlias, token);

                    case CommandType.Backup:
                        if (Config.Default.AllowDiscordBackup)
                            return BackupServer(channelId, profileIdOrAlias, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Restart:
                        if (Config.Default.AllowDiscordRestart)
                            return StartServer(channelId, profileIdOrAlias, restart: true, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Shutdown:
                        if (Config.Default.AllowDiscordShutdown)
                            return StopServer(channelId, profileIdOrAlias, shutdown: true, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Start:
                        if (Config.Default.AllowDiscordStart)
                            return StartServer(channelId, profileIdOrAlias, restart: false, token);
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandNotEnabled"), commandType) };
                    case CommandType.Stop:
                        if (Config.Default.AllowDiscordStop)
                            return StopServer(channelId, profileIdOrAlias, shutdown: false, token);
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

        private static IList<string> GetServerInfo(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Info) };
            }

            var profileList = new List<ServerProfileSnapshot>();
            var responseList = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s =>
                    string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                    && (
                        string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase)
                    )
                );

                if (serverList.IsEmpty())
                {
                    if (!string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        responseList.Add(_globalizer.GetResourceString("DiscordBot_NoChannelProfiles"));
                    }
                    else
                    {
                        responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                }
                else
                {
                    foreach (var server in serverList)
                    {
                        // check if another command is being run against the profile
                        if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                            continue;
                        }

                        switch (server.Runtime.Status)
                        {
                            case ServerStatus.Initializing:
                            case ServerStatus.Stopping:
                            case ServerStatus.Stopped:
                            case ServerStatus.Uninstalled:
                            case ServerStatus.Unknown:
                            case ServerStatus.Updating:
                                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                                continue;
                        }

                        _currentProfileCommands.Add(server.Profile.ProfileID, CommandType.Info);
                        profileList.Add(ServerProfileSnapshot.Create(server.Profile));
                    }
                }
            }).Wait(token);

            foreach (var profile in profileList)
            {
                try
                {
                    using (var gameServer = ServerQuery.GetServerInstance(EngineType.Source, new IPEndPoint(profile.ServerIPAddress, profile.QueryPort)))
                    {
                        var info = gameServer?.GetInfo();
                        if (info is null)
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_InfoFailed"), profile.ServerName));
                        }
                        else
                        {
                            var mapName = _globalizer.GetResourceString($"Map_{info.Map}") ?? info.Map;
                            responseList.Add($"```{info.Name}\n" +
                                $"{_globalizer.GetResourceString("DiscordBot_MapLabel")} {mapName}\n" +
                                $"{_globalizer.GetResourceString("ServerSettings_PlayersLabel")} {info.Players} / {info.MaxPlayers}```");
                        }
                    }
                }
                catch (Exception)
                {
                    responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_InfoFailed"), profile.ServerName));
                }

                _currentProfileCommands.Remove(profile.ProfileId);
            }

            return responseList;
        }

        private static IList<string> GetServerList(string channelId, CancellationToken token)
        {
            var responseList = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => 
                    string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                );

                if (serverList.IsEmpty())
                {
                    responseList.Add(_globalizer.GetResourceString("DiscordBot_NoChannelProfiles"));
                }
                else
                {
                    responseList.Add($"**{_globalizer.GetResourceString("DiscordBot_CountLabel")}** {serverList.Count()}");

                    foreach (var server in serverList)
                    {
                        responseList.Add($"```{_globalizer.GetResourceString("ServerSettings_ProfileLabel")} {server.Profile.ProfileName}\n" +
                            $"{_globalizer.GetResourceString("ServerSettings_ProfileIdLabel")} {server.Profile.ProfileID}\n" +
                            (string.IsNullOrWhiteSpace(server.Profile.DiscordAlias) ? "" : $"{_globalizer.GetResourceString("ServerSettings_DiscordAliasLabel")} {server.Profile.DiscordAlias}\n") +
                            $"{_globalizer.GetResourceString("ServerSettings_ServerNameLabel")} {server.Profile.ServerName}```");
                    }
                }
            }).Wait(token);

            return responseList;
        }

        private static IList<string> GetServerStatus(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Info) };
            }

            var responseList = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => 
                    string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                    && (
                        string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase)
                    )
                );

                if (serverList.IsEmpty())
                {
                    if (!string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        responseList.Add(_globalizer.GetResourceString("DiscordBot_NoChannelProfiles"));
                    }
                    else
                    {
                        responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                }
                else
                {
                    responseList.Add($"**{_globalizer.GetResourceString("DiscordBot_CountLabel")}** {serverList.Count()}");

                    foreach (var server in serverList)
                    {
                        responseList.Add($"```{_globalizer.GetResourceString("ServerSettings_ProfileLabel")} {server.Profile.ProfileName}\n" +
                            $"{_globalizer.GetResourceString("ServerSettings_ProfileIdLabel")} {server.Profile.ProfileID}\n" +
                            (string.IsNullOrWhiteSpace(server.Profile.DiscordAlias) ? "" : $"{_globalizer.GetResourceString("ServerSettings_DiscordAliasLabel")} {server.Profile.DiscordAlias}\n") +
                            $"{_globalizer.GetResourceString("ServerSettings_ServerNameLabel")} {server.Profile.ServerName}\n" +
                            $"{_globalizer.GetResourceString("ServerSettings_StatusLabel")} {server.Runtime.StatusString}\n" +
                            $"{_globalizer.GetResourceString("ServerSettings_AvailabilityLabel")} {_globalizer.GetResourceString($"ServerSettings_Availability_{server.Runtime.Availability}")}```");
                    }
                }
            }).Wait(token);

            return responseList;
        }

        private static IList<string> BackupServer(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Backup) };
            }

            var profileList = new List<ServerProfileSnapshot>();
            var responseList = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => 
                    string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                    && (
                        string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase)
                    )
                );

                if (serverList.IsEmpty())
                {
                    if (!string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        responseList.Add(_globalizer.GetResourceString("DiscordBot_NoChannelProfiles"));
                    }
                    else
                    {
                        responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                }
                else
                {
                    foreach (var server in serverList)
                    {
                        if (!server.Profile.AllowDiscordBackup)
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Backup, server.Profile.ProfileName));
                            continue;
                        }

                        // check if another command is being run against the profile
                        if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                            continue;
                        }

                        switch (server.Runtime.Status)
                        {
                            case ServerStatus.Initializing:
                            case ServerStatus.Stopping:
                            case ServerStatus.Uninstalled:
                            case ServerStatus.Unknown:
                            case ServerStatus.Updating:
                                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                                continue;
                        }

                        _currentProfileCommands.Add(server.Profile.ProfileID, CommandType.Backup);
                        profileList.Add(ServerProfileSnapshot.Create(server.Profile));
                    }
                }
            }).Wait(token);

            foreach (var profile in profileList)
            {
                var app = new ServerApp(true)
                {
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = ServerProcessType.Backup,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => string.Equals(profile.ProfileId, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase));
                            if (server != null)
                            {
                                server.Runtime.UpdateServerStatus(serverStatus, serverStatus != ServerStatus.Unknown);
                            }
                        }).Wait(token);
                    }
                };

                Task.Run(() =>
                {
                    app.PerformProfileBackup(profile, token);
                    _currentProfileCommands.Remove(profile.ProfileId);
                }, token);

                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_BackupRequested"), profile.ServerName));
            }

            return responseList;
        }

        private static IList<string> StartServer(string channelId, string profileIdOrAlias, bool restart, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), restart ? CommandType.Restart : CommandType.Start) };
            }

            var profileList = new List<ServerProfileSnapshot>();
            var responseList = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s =>
                    string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                    && (
                        string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase)
                    )
                );

                if (serverList.IsEmpty())
                {
                    if (!string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        responseList.Add(_globalizer.GetResourceString("DiscordBot_NoChannelProfiles"));
                    }
                    else
                    {
                        responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                }
                else
                {
                    foreach (var server in serverList)
                    {
                        if (restart && !server.Profile.AllowDiscordRestart)
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Restart, server.Profile.ProfileName));
                            continue;
                        }
                        if (!restart && !server.Profile.AllowDiscordStart)
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Start, server.Profile.ProfileName));
                            continue;
                        }

                        // check if another command is being run against the profile
                        if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                            continue;
                        }

                        switch (server.Runtime.Status)
                        {
                            case ServerStatus.Initializing:
                            case ServerStatus.Stopping:
                            case ServerStatus.Uninstalled:
                            case ServerStatus.Unknown:
                                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                                continue;

                            case ServerStatus.Running:
                                if (!restart)
                                {
                                    responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                                    continue;
                                }
                                break;

                            case ServerStatus.Updating:
                                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
                                continue;
                        }

                        _currentProfileCommands.Add(server.Profile.ProfileID, restart ? CommandType.Restart : CommandType.Start);
                        var profile = ServerProfileSnapshot.Create(server.Profile);
                        profileList.Add(profile);
                    }
                }
            }).Wait(token);

            foreach (var profile in profileList)
            {
                var app = new ServerApp(true)
                {
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = ServerProcessType.Restart,
                    RestartIfShutdown = true,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => string.Equals(profile.ProfileId, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase));
                            if (server != null)
                            {
                                server.Runtime.UpdateServerStatus(serverStatus, serverStatus != ServerStatus.Unknown);
                            }
                        }).Wait(token);
                    }
                };

                Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, true, ServerUpdateType.None, false, false, token);
                    _currentProfileCommands.Remove(profile.ProfileId);
                }, token);

                if (restart)
                    responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_RestartRequested"), profile.ServerName));
                else
                    responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_StartRequested"), profile.ServerName));
            }

            return responseList;
        }

        private static IList<string> StopServer(string channelId, string profileIdOrAlias, bool shutdown, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), shutdown ? CommandType.Shutdown : CommandType.Stop) };
            }

            var profileList = new List<ServerProfileSnapshot>();
            var responseList = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s =>
                    string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                    && (
                        string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase)
                    )
                );

                if (serverList.IsEmpty())
                {
                    if (!string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        responseList.Add(_globalizer.GetResourceString("DiscordBot_NoChannelProfiles"));
                    }
                    else
                    {
                        responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                }
                else
                {
                    foreach (var server in serverList)
                    {
                        if (shutdown && !server.Profile.AllowDiscordShutdown)
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Shutdown, server.Profile.ProfileName));
                            continue;
                        }
                        if (!shutdown && !server.Profile.AllowDiscordStop)
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Stop, server.Profile.ProfileName));
                            continue;
                        }

                        // check if another command is being run against the profile
                        if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                            continue;
                        }

                        switch (server.Runtime.Status)
                        {
                            case ServerStatus.Initializing:
                            case ServerStatus.Stopping:
                            case ServerStatus.Stopped:
                            case ServerStatus.Uninstalled:
                            case ServerStatus.Unknown:
                                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                                continue;

                            case ServerStatus.Updating:
                                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
                                continue;
                        }

                        _currentProfileCommands.Add(server.Profile.ProfileID, shutdown ? CommandType.Shutdown : CommandType.Stop);
                        profileList.Add(ServerProfileSnapshot.Create(server.Profile));
                    }
                }
            }).Wait(token);

            foreach (var profile in profileList)
            {
                var app = new ServerApp(true)
                {
                    BackupWorldFile = shutdown,
                    OutputLogs = false,
                    PerformWorldSave = shutdown,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = shutdown ? ServerProcessType.Shutdown : ServerProcessType.Stop,
                    RestartIfShutdown = false,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => string.Equals(profile.ProfileId, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase));
                            if (server != null)
                            {
                                server.Runtime.UpdateServerStatus(serverStatus, serverStatus != ServerStatus.Unknown);
                            }
                        }).Wait(token);
                    }
                };

                if (!shutdown)
                    app.ShutdownInterval = 0;

                Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, false, ServerUpdateType.None, false, false, token);
                    _currentProfileCommands.Remove(profile.ProfileId);
                }, token);

                if (shutdown)
                    responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ShutdownRequested"), profile.ServerName));
                else
                    responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_StopRequested"), profile.ServerName));
            }

            return responseList;
        }

        private static IList<string> UpdateServer(string channelId, string profileIdOrAlias, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(profileIdOrAlias))
            {
                return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_ProfileMissing"), CommandType.Update) };
            }

            var profileList = new List<ServerProfileSnapshot>();
            var responseList = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s =>
                    string.Equals(channelId, s.Profile.DiscordChannelId, StringComparison.OrdinalIgnoreCase)
                    && (
                        string.Equals(profileIdOrAlias, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(s.Profile.DiscordAlias) && string.Equals(profileIdOrAlias, s.Profile.DiscordAlias, StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase)
                    )
                );

                if (serverList.IsEmpty())
                {
                    if (!string.IsNullOrWhiteSpace(Config.Default.DiscordBotAllServersKeyword) && string.Equals(profileIdOrAlias, Config.Default.DiscordBotAllServersKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        responseList.Add(_globalizer.GetResourceString("DiscordBot_NoChannelProfiles"));
                    }
                    else
                    {
                        responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileNotFound"), profileIdOrAlias));
                    }
                }
                else
                {
                    foreach (var server in serverList)
                    {
                        if (!server.Profile.AllowDiscordUpdate)
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandDisabledProfile"), CommandType.Update, server.Profile.ProfileName));
                            continue;
                        }

                        // check if another command is being run against the profile
                        if (_currentProfileCommands.ContainsKey(server.Profile.ProfileID))
                        {
                            responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandRunningProfile"), _currentProfileCommands[server.Profile.ProfileID], server.Profile.ProfileName));
                            continue;
                        }

                        switch (server.Runtime.Status)
                        {
                            case ServerStatus.Running:
                                break;

                            case ServerStatus.Initializing:
                            case ServerStatus.Stopping:
                            case ServerStatus.Unknown:
                                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileBadStatus"), server.Profile.ProfileName, server.Runtime.StatusString));
                                continue;

                            case ServerStatus.Updating:
                                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_ProfileUpdating"), server.Profile.ProfileName));
                                continue;
                        }

                        _currentProfileCommands.Add(server.Profile.ProfileID, CommandType.Update);
                        var profile = ServerProfileSnapshot.Create(server.Profile);
                        profileList.Add(profile);
                    }
                }
            }).Wait(token);

            foreach (var profile in profileList)
            {
                var app = new ServerApp(true)
                {
                    OutputLogs = false,
                    SendAlerts = true,
                    SendEmails = false,
                    ServerProcess = ServerProcessType.Update,
                    RestartIfShutdown = false,
                    ServerStatusChangeCallback = (ServerStatus serverStatus) =>
                    {
                        TaskUtils.RunOnUIThreadAsync(() =>
                        {
                            var server = ServerManager.Instance.Servers.FirstOrDefault(s => string.Equals(profile.ProfileId, s.Profile.ProfileID, StringComparison.OrdinalIgnoreCase));
                            if (server != null)
                            {
                                server.Runtime.UpdateServerStatus(serverStatus, serverStatus != ServerStatus.Unknown);
                            }
                        }).Wait(token);
                    }
                };

                Task.Run(() =>
                {
                    app.PerformProfileShutdown(profile, true, ServerUpdateType.ServerAndMods, false, false, token);
                    _currentProfileCommands.Remove(profile.ProfileId);
                }, token);

                responseList.Add(string.Format(_globalizer.GetResourceString("DiscordBot_UpdateRequested"), profile.ServerName));
            }

            return responseList;
        }
    }
}
