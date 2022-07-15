using Newtonsoft.Json.Linq;
using NLog;
using QueryMaster;
using ServerManagerTool.Common.Enums;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ServerManagerTool.Lib
{
    public class ServerStatusWatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly List<ServerStatusUpdateRegistration> _serverRegistrations = new List<ServerStatusUpdateRegistration>();
        private readonly ActionBlock<Func<Task>> _eventQueue;
        private readonly Dictionary<string, DateTime> _nextExternalStatusQuery = new Dictionary<string, DateTime>();

        static ServerStatusWatcher()
        {
            Instance = new ServerStatusWatcher();
        }

        private ServerStatusWatcher()
        {
            _eventQueue = new ActionBlock<Func<Task>>(async f => await f.Invoke(), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
            _eventQueue.Post(DoUpdateAsync);
        }

        public static ServerStatusWatcher Instance
        {
            get;
            private set;
        }

        public IAsyncDisposable RegisterForUpdates(string installDirectory, string profileId, IPEndPoint localEndpoint, IPEndPoint steamEndpoint, Action<IAsyncDisposable, ServerStatusUpdate> updateCallback)
        {
            var registration = new ServerStatusUpdateRegistration 
            { 
                InstallDirectory = installDirectory,
                ProfileId = profileId,
                LocalEndpoint = localEndpoint, 
                PublicEndpoint = steamEndpoint, 
                UpdateCallback = updateCallback,
            };

            registration.UnregisterAction = async () => 
                {
                    var tcs = new TaskCompletionSource<bool>();
                    _eventQueue.Post(() => 
                    {
                        if(_serverRegistrations.Contains(registration))
                        {
                            Logger.Debug($"{nameof(RegisterForUpdates)} Removing registration for L:{registration.LocalEndpoint} P:{registration.PublicEndpoint}");
                            _serverRegistrations.Remove(registration);
                        }
                        tcs.TrySetResult(true);
                        return Task.FromResult(true);
                    });

                    await tcs.Task;
                };

            _eventQueue.Post(() =>
                {
                    if (!_serverRegistrations.Contains(registration))
                    {
                        Logger.Debug($"{nameof(RegisterForUpdates)} Adding registration for L:{registration.LocalEndpoint} P:{registration.PublicEndpoint}");
                        _serverRegistrations.Add(registration);

                        var registrationKey = registration.PublicEndpoint.ToString();
                        _nextExternalStatusQuery[registrationKey] = DateTime.MinValue;
                    }
                    return Task.FromResult(true);
                }
            );

            return registration;
        }

        private async Task DoUpdateAsync()
        {
            try
            {
                foreach (var registration in _serverRegistrations)
                {
                    var statusUpdate = new ServerStatusUpdate();

                    try
                    {
                        Logger.Info($"{nameof(DoUpdateAsync)} Start: {registration.LocalEndpoint}, {registration.PublicEndpoint}");
                        statusUpdate = await DoServerStatusUpdateAsync(registration);
                        
                        PostServerStatusUpdate(registration, statusUpdate);
                    }
                    catch (Exception ex)
                    {
                        // We don't want to stop other registration queries or break the ActionBlock
                        Logger.Error($"{nameof(DoUpdateAsync)} - Exception in local update. {ex.Message}\r\n{ex.StackTrace}");
                        Debugger.Break();
                    }
                    finally
                    {
                        Logger.Info($"{nameof(DoUpdateAsync)} End: {registration.LocalEndpoint}, {registration.PublicEndpoint}, Status: {statusUpdate.Status}");
                    }
                }
            }
            finally
            {
                Task.Delay(Config.Default.ServerStatusWatcher_LocalStatusQueryDelay)
                    .ContinueWith(_ => _eventQueue.Post(DoUpdateAsync))
                    .DoNotWait();
            }
        }

        private async Task<ServerStatusUpdate> DoServerStatusUpdateAsync(ServerStatusUpdateRegistration registration)
        {
            var registrationKey = registration.PublicEndpoint.ToString();

            //
            // First check the process status
            //
            var processStatus = GetServerProcessStatus(registration, out Process process);
            switch (processStatus)
            {
                case ServerProcessStatus.NotInstalled:
                    return new ServerStatusUpdate { Status = WatcherServerStatus.NotInstalled };

                case ServerProcessStatus.Stopped:
                    return new ServerStatusUpdate { Status = WatcherServerStatus.Stopped };

                case ServerProcessStatus.Unknown:
                    return new ServerStatusUpdate { Status = WatcherServerStatus.Unknown };

                case ServerProcessStatus.Running:
                    break;

                default:
                    Debugger.Break();
                    break;
            }

            var currentStatus = WatcherServerStatus.Initializing;

            Logger.Info($"{nameof(DoServerStatusUpdateAsync)} Checking server local (directly) status at {registration.LocalEndpoint}");

            // get the server information direct from the server using local endpoint.
            var serverStatus = GetLocalNetworkStatus(registration.LocalEndpoint, out ServerInfo localInfo, out int onlinePlayerCount);

            if (serverStatus)
            {
                currentStatus = WatcherServerStatus.LocalSuccess;

                Logger.Info($"{nameof(DoServerStatusUpdateAsync)} Checking server public (directly) status at {registration.PublicEndpoint}");

                // get the server information direct from the server using public endpoint.
                serverStatus = GetPublicNetworkStatusDirectly(registration.PublicEndpoint);

                if (serverStatus)
                {
                    _nextExternalStatusQuery[registrationKey] = DateTime.MinValue;

                    currentStatus = WatcherServerStatus.Published;
                }
                else if (!string.IsNullOrWhiteSpace(Config.Default.ServerStatusUrlFormat))
                {
                    var nextExternalStatusQuery = _nextExternalStatusQuery.ContainsKey(registrationKey) ? _nextExternalStatusQuery[registrationKey] : DateTime.MinValue;
                    if (DateTime.Now >= nextExternalStatusQuery)
                    {
                        Logger.Info($"{nameof(DoServerStatusUpdateAsync)} Checking server public (externally) status at {registration.PublicEndpoint}");

                        // get the server information direct from the server using external endpoint.
                        var url = string.Format(Config.Default.ServerStatusUrlFormat, Config.Default.ServerManagerCode, App.Instance.Version, registration.PublicEndpoint.Address, registration.PublicEndpoint.Port);
                        var uri = new Uri(url);
                        serverStatus = await GetPublicNetworkStatusViaAPIAsync(uri, registration.PublicEndpoint);

                        var remoteStatusQueryDelay = serverStatus 
                            ? Config.Default.ServerStatusWatcher_RemoteStatusQueryDelay 
                            : 15000;
                        _nextExternalStatusQuery[registrationKey] = DateTime.Now.AddMilliseconds(remoteStatusQueryDelay);

                        if (serverStatus)
                        {
                            currentStatus = WatcherServerStatus.ExternalSuccess;
                        }
                    }
                    else
                    {
                        currentStatus = WatcherServerStatus.ExternalSkipped;
                    }
                }
            }

            var statusUpdate = new ServerStatusUpdate
            {
                Process = process,
                Status = currentStatus,
                ServerInfo = localInfo,
                OnlinePlayerCount = onlinePlayerCount,
            };

            return await Task.FromResult(statusUpdate);
        }

        private static ServerProcessStatus GetServerProcessStatus(ServerStatusUpdateRegistration updateContext, out Process serverProcess)
        {
            serverProcess = null;
            if (string.IsNullOrWhiteSpace(updateContext.InstallDirectory))
            {
                return ServerProcessStatus.NotInstalled;
            }

            var serverExePath = Path.Combine(updateContext.InstallDirectory, Config.Default.ServerBinaryRelativePath, Config.Default.ServerExe);
            if(!File.Exists(serverExePath))
            {
                return ServerProcessStatus.NotInstalled;
            }

            //
            // The server appears to be installed, now determine if it is running or stopped.
            //
            try
            {
                foreach (var process in Process.GetProcessesByName(Config.Default.ServerProcessName))
                {
                    var commandLine = ProcessUtils.GetCommandLineForProcess(process.Id)?.ToLower();

                    if (commandLine != null &&
                        (commandLine.StartsWith(serverExePath, StringComparison.OrdinalIgnoreCase) || commandLine.StartsWith($"\"{serverExePath}\"", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Does this match our server exe and port?
                        var serverArgMatch = string.Format(Config.Default.ServerCommandLineArgsMatchFormat, updateContext.LocalEndpoint.Port).ToLower();
                        if (commandLine.Contains(serverArgMatch))
                        {
                            // Was an IP set on it?
                            var anyIpArgMatch = string.Format(Config.Default.ServerCommandLineArgsIPMatchFormat, string.Empty).ToLower();
                            if (commandLine.Contains(anyIpArgMatch))
                            {
                                // If we have a specific IP, check for it.
                                var ipArgMatch = string.Format(Config.Default.ServerCommandLineArgsIPMatchFormat, updateContext.LocalEndpoint.Address.ToString()).ToLower();
                                if (!commandLine.Contains(ipArgMatch))
                                {
                                    // Specific IP set didn't match
                                    continue;
                                }

                                // Specific IP matched
                            }

                            // Either specific IP matched or no specific IP was set and we will claim this is ours.

                            process.EnableRaisingEvents = true;
                            if (process.HasExited)
                            {
                                return ServerProcessStatus.Stopped;
                            }

                            serverProcess = process;
                            return ServerProcessStatus.Running;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Error($"{nameof(GetServerProcessStatus)}. {ex.Message}\r\n{ex.StackTrace}");
            }

            return ServerProcessStatus.Stopped;
        }

        private static bool GetLocalNetworkStatus(IPEndPoint endpoint, out ServerInfo serverInfo, out int onlinePlayerCount)
        {
            serverInfo = null;
            onlinePlayerCount = 0;

            try
            {
                using (var server = ServerQuery.GetServerInstance(EngineType.Source, endpoint))
                {
                    try
                    {
                        serverInfo = server?.GetInfo();
                    }
                    catch (Exception)
                    {
                        serverInfo = null;
                    }

                    try
                    {
                        var playerInfo = server?.GetPlayers()?.Where(p => !string.IsNullOrWhiteSpace(p.Name?.Trim()));
                        onlinePlayerCount = playerInfo?.Count() ?? 0;
                    }
                    catch (Exception)
                    {
                        onlinePlayerCount = 0;
                    }
                }

                return serverInfo != null;
            }
            catch (SocketException ex)
            {
                // Common when the server is unreachable.  Ignore it.
                Logger.Debug($"{nameof(GetLocalNetworkStatus)} - Failed checking local status for: {endpoint.Address}:{endpoint.Port}. {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Debug($"{nameof(GetLocalNetworkStatus)} - Failed checking local status for: {endpoint.Address}:{endpoint.Port}. {ex.Message}");
            }

            return false;
        }

        private static bool GetPublicNetworkStatusDirectly(IPEndPoint endpoint)
        {
            ServerInfo serverInfo;

            try
            {
                using (var server = ServerQuery.GetServerInstance(EngineType.Source, endpoint))
                {
                    serverInfo = server.GetInfo();
                }

                return serverInfo != null;
            }
            catch (Exception ex)
            {
                Logger.Debug($"{nameof(GetPublicNetworkStatusDirectly)} - Failed checking public status for: {endpoint.Address}:{endpoint.Port}. {ex.Message}");
            }

            return false;
        }

        public static async Task<bool> GetPublicNetworkStatusViaAPIAsync(Uri uri, IPEndPoint endpoint)
        {
            try
            {
                string jsonString;
                using (var client = new WebClient())
                {
                    jsonString = await client.DownloadStringTaskAsync(uri);
                }

                if (jsonString == null)
                {
                    Logger.Debug($"Server info request returned null string for {endpoint.Address}:{endpoint.Port}");
                    return false;
                }

                JObject query = JObject.Parse(jsonString);
                if (query == null)
                {
                    Logger.Debug($"Server info request failed to parse for {endpoint.Address}:{endpoint.Port} - '{jsonString}'");
                    return false;
                }

                var available = query.SelectToken("available");
                if (available == null)
                {
                    Logger.Debug($"Server at {endpoint.Address}:{endpoint.Port} returned no availability.");
                    return false;
                }

                return (bool)available;
            }
            catch (Exception ex)
            {
                Logger.Debug($"{nameof(GetPublicNetworkStatusViaAPIAsync)} - Failed checking public status for: {endpoint.Address}:{endpoint.Port}. {ex.Message}");
            }

            return false;
        }

        private void PostServerStatusUpdate(ServerStatusUpdateRegistration registration, ServerStatusUpdate statusUpdate)
        {
            _eventQueue.Post(() =>
            {
                if (_serverRegistrations.Contains(registration))
                {
                    try
                    {
                        registration.UpdateCallback(registration, statusUpdate);
                    }
                    catch (Exception ex)
                    {
                        DebugUtils.WriteFormatThreadSafeAsync("Exception during server status update callback: {0}\n{1}", ex.Message, ex.StackTrace).DoNotWait();
                    }
                }
                return TaskUtils.FinishedTask;
            });
        }
    }
}
