using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Net;
using System.Globalization;
using System.Threading;

namespace QueryMaster
{
    /// <summary>
    /// Encapsulates a method that has a parameter of type string which is the log message received from server.
    /// Invoked when a log message is received from server.
    /// </summary>
    /// <param name="log">Received log message.</param>
    public delegate void LogCallback(string log);
    /// <summary>
    /// Provides methods to listen to logs and to set up events on desired type of log message.
    /// </summary>
    public class Logs : IDisposable
    {
        Socket UdpSocket;
        private readonly int BufferSize = 1400;
        private LogCallback Callback;
        private byte[] recvData;
        private int Port;
        private int HeaderSize = 0;
        private IPEndPoint ServerEndPoint;
        private Regex LineSplit;
        private Regex RegPlayer = new Regex("^([^\"]+)<([^\"]+)><([^\"]+)><([^\"]*)>$");
        private char[] QuoteSplitPattern = { '\"' };
        /// <summary>
        /// Occurs when Server cvar starts(In TFC, if tfc_clanbattle is 1, this doesn't happen.).
        /// </summary>
        public event EventHandler<LogEventArgs> CvarStartMsg; //001.1
        /// <summary>
        /// Occurs when someone changes a cvar over rcon.
        /// </summary>
        public event EventHandler<CvarEventArgs> ServerCvar;   //001.2
        /// <summary>
        /// Occurs when Server cvar ends(In TFC, if tfc_clanbattle is 0, this doesn't happen.).
        /// </summary>
        public event EventHandler<LogEventArgs> CvarEndMsg;   //001.3
        /// <summary>
        /// Occurs when Logging to file is started.
        /// </summary>
        public event EventHandler<LogStartEventArgs> LogFileStarted;   //002.1
        /// <summary>
        /// Occurs when Log file is closed.
        /// </summary>
        public event EventHandler<LogEventArgs> LogFileClosed;    //002.2
        /// <summary>
        /// Occurs when map is loaded.
        /// </summary>
        public event EventHandler<MapLoadEventArgs> MapLoaded;    //003.1
        /// <summary>
        /// Occurs when Map starts.
        /// </summary>
        public event EventHandler<MapStartEventArgs> MapStarted;   //003.2
        /// <summary>
        /// Occurs when an rcon message is sent to server.
        /// </summary>
        public event EventHandler<RconEventArgs> RconMsg;      //004(1,2)
        /// <summary>
        /// Occurs when server name is displayed.
        /// </summary>
        public event EventHandler<ServerNameEventArgs> ServerName;   //005
        /// <summary>
        /// Occurs when Server says.
        /// </summary>
        public event EventHandler<ServerSayEventArgs> ServerSay;    //006
        /// <summary>
        /// Occurs when a player is connected.
        /// </summary>
        public event EventHandler<ConnectEventArgs> PlayerConnected;  //050
        /// <summary>
        /// Occurs when a player is validated.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerValidated;  //050b
        /// <summary>
        /// Occurs when a player is enters game.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerEnteredGame;    //51
        /// <summary>
        /// Occurs when a player is disconnected.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerDisConnected;   //52
        /// <summary>
        /// Occurs when a player is kicked.
        /// </summary>
        public event EventHandler<KickEventArgs> PlayerKicked;   //052b
        /// <summary>
        /// Occurs when a player commit suicide.
        /// </summary>
        public event EventHandler<SuicideEventArgs> PlayerSuicided;   //053
        /// <summary>
        /// Occurs when a player Join team.
        /// </summary>
        public event EventHandler<TeamSelectionEventArgs> PlayerJoinedTeam;   //054
        /// <summary>
        /// Occurs when a player change role.
        /// </summary>
        public event EventHandler<RoleSelectionEventArgs> PlayerChangedRole;   //055
        /// <summary>
        /// Occurs when a player changes name.
        /// </summary>
        public event EventHandler<NameChangeEventArgs> PlayerChangedName;   //056
        /// <summary>
        /// Occurs when a player is killed.
        /// </summary>
        public event EventHandler<KillEventArgs> PlayerKilled;        //057
        /// <summary>
        /// Occurs when a player is injured.
        /// </summary>
        public event EventHandler<InjureEventArgs> PlayerInjured;       //058
        /// <summary>
        /// Occurs when a player triggers  something on another player(in TFC this event may cover medic healings and infections, sentry gun destruction, spy uncovering.etc).
        /// </summary>
        public event EventHandler<PlayerOnPlayerEventArgs> PlayerOnPLayerTriggered;      //059
        /// <summary>
        ///  Occurs when a player triggers an action.
        /// </summary>
        public event EventHandler<PlayerActionEventArgs> PlayerTriggered;        //060
        /// <summary>
        ///  Occurs when a team triggers an action(eg:team winning).
        /// </summary>
        public event EventHandler<TeamActionEventArgs> TeamTriggered;           //061
        /// <summary>
        ///  Occurs when server triggers an action(eg:roundstart,game events).
        /// </summary>
        public event EventHandler<WorldActionEventArgs> WorldTriggered;          //062
        /// <summary>
        ///  Occurs when a player says. 
        /// </summary>
        public event EventHandler<ChatEventArgs> Say;                     //063.1
        /// <summary>
        ///  Occurs when a player uses teamsay.
        /// </summary>
        public event EventHandler<ChatEventArgs> TeamSay;                 //063.2
        /// <summary>
        ///  Occurs when a team forms alliance with another team.
        /// </summary>
        public event EventHandler<TeamAllianceEventArgs> TeamAlliance;            //064
        /// <summary>
        ///  Occurs when Team Score Report is displayed at round end.
        /// </summary>
        public event EventHandler<TeamScoreReportEventArgs> TeamScoreReport;         //065
        /// <summary>
        /// Occurs when a private message is sent.
        /// </summary>
        public event EventHandler<PrivateChatEventArgs> PrivateChat;             //066
        /// <summary>
        /// Occurs when Player Score Report is displayed at round end.
        /// </summary>
        public event EventHandler<PlayerScoreReportEventArgs> PlayerScoreReport;       //067
        /// <summary>
        /// Occurs when Player selects a weapon.
        /// </summary>
        public event EventHandler<WeaponEventArgs> PlayerSelectedWeapon;    //068
        /// <summary>
        /// Occurs when Player acquires a weapon.
        /// </summary>
        public event EventHandler<WeaponEventArgs> PlayerAcquiredWeapon;    //069
        /// <summary>
        /// Occurs when server shuts down.
        /// </summary>
        public event EventHandler<LogEventArgs> ShutDown;                   //new
        /// <summary>
        /// Occurs when a log message cannot be parsed.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> Exception;

        internal Logs(EngineType type, int port, IPEndPoint serverEndPoint)
        {
            ServerEndPoint = serverEndPoint;
            Port = port;
            recvData = new byte[BufferSize];
            LineSplit = new Regex(": ");
            switch (type)
            {
                case EngineType.GoldSource: HeaderSize = 10; break;
                case EngineType.Source: HeaderSize = 7; break;
            }
            UdpSocket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
            UdpSocket.Bind(new IPEndPoint(IPAddress.Any, Port));

            UdpSocket.BeginReceive(recvData, 0, recvData.Length, SocketFlags.None, Recv, null);
        }
        /// <summary>
        /// Listen to logs sent by the server
        /// </summary>
        /// <param name="callback">Called when a log message is received</param>
        public void Listen(LogCallback callback)
        {
            Callback = callback;
        }

        private void Recv(IAsyncResult res)
        {
            int bytesRecv = 0;
            try
            {
                bytesRecv = UdpSocket.EndReceive(res);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            if(bytesRecv>HeaderSize)
                ThreadPool.QueueUserWorkItem(ProcessLog, Encoding.UTF8.GetString(recvData, HeaderSize, bytesRecv - HeaderSize));
            UdpSocket.BeginReceive(recvData, 0, recvData.Length, SocketFlags.None, Recv, null);
        }

        private void ProcessLog(object line)
        {
            string logLine = (string)line;
            DateTime Timestamp;
            string info;
            try
            {
                string[] data = LineSplit.Split(logLine, 2);
                Timestamp = DateTime.ParseExact(data[0], "MM/dd/yyyy - HH:mm:ss", CultureInfo.InvariantCulture);
                info = data[1].Remove(data[1].Length - 2);
            }
            catch (Exception e)
            {
                e.Data.Add("ReceivedData", Util.StringToBytes(logLine));
                throw;
            }
            if (info.StartsWith("//"))
                return;
            if (Callback != null)
                Callback(logLine);
            string[] result = info.Split(QuoteSplitPattern, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                if (info[0] == '\"')
                {
                    switch (result[1])
                    {
                        case " connected, address ": OnConnection(Timestamp, result); break;    // 50
                        case " STEAM USERID validated": OnValidation(Timestamp, result); break;           //50b
                        case " entered the game": OnEnterGame(Timestamp, result); break;           //51
                        case " disconnected": OnDisconnection(Timestamp, result); break;                   //52
                        case " committed suicide with ": OnSuicide(Timestamp, result); break;             //53
                        case " joined team ": OnTeamSelection(Timestamp, result); break;                    //54
                        case " changed role to ": OnRoleSelection(Timestamp, result); break;                  //55
                        case " changed name to ": OnNameChange(Timestamp, result); break;                //56
                        case " killed ": OnKill(Timestamp, result); break;                          //57
                        case " attacked ": OnInjure(Timestamp, result); break;                      //58
                        case " triggered ":                                                         //59 ,60
                            {
                                if (result.Length > 3 && result[3] == " against ")
                                    OnPlayer_PlayerAction(Timestamp, result);
                                else
                                    OnPlayerAction(Timestamp, result);
                                break;
                            }
                        case " say ": OnSay(Timestamp, result); break;                                //63a
                        case " say_team ": OnTeamSay(Timestamp, result); break;                          //63b
                        case " tell ": OnPrivateChat(Timestamp, result); break;                                //66
                        case " selected weapon ": OnWeaponSelection(Timestamp, result); break;            //68
                        case " acquired weapon ": OnWeaponPickup(Timestamp, result); break;          //69
                        default: OnException(Timestamp, info); break;

                    }
                }
                else
                {
                    switch (result[0])
                    {
                        case "Server cvars start": OnCvarStart(Timestamp); break;             //001.1
                        case "Server cvar ": OnServerCvar(Timestamp, result); break;            //001.2
                        case "Server cvars end": OnCvarEnd(Timestamp); break;               //001.3
                        case "Log file started (file ": OnLogFileStart(Timestamp, result); break; //002.1
                        case "Log file closed": OnLogFileClose(Timestamp); break;           //002.2
                        case "Loading map ": OnMapLoading(Timestamp, result); break;                //003.1
                        case "Started map ": OnMapStart(Timestamp, result); break;                  //003.2
                        case "Rcon: ": OnRconMsg(Timestamp, result); break;                         //004.1
                        case "Bad Rcon: ": OnRconMsg(Timestamp, result); break;                     //004.2
                        case "Server name is ": OnserverName(Timestamp, result); break;             //005
                        case "Server say ": OnServerSay(Timestamp, result); break;                  //006
                        case "Kick: ": OnKick(Timestamp, result); break;                            //0052b
                        case "Team ":
                            {
                                switch (result[2])
                                {
                                    case " triggered ": OnTeamAction(Timestamp, result); break;     //061
                                    case " formed alliance with team ": OnTeamAlliance(Timestamp, result); break;   //064
                                    case " scored ": OnTeamScoreReport(Timestamp, result); break;        //065
                                }
                                break;

                            }
                        case "World triggered ": OnWorldAction(Timestamp, result); break;       //062
                        case "Player ": OnPlayerAction(Timestamp, result); break;               //60
                        case "Server shutdown": OnShutDown(Timestamp); break;                   //new
                        default: OnException(Timestamp, info); break;
                    }
                }
            }
            catch (Exception)
            {
                Exception.Fire(ServerEndPoint, new ExceptionEventArgs() { Timestamp = Timestamp, LogLine = info });
            }



        }
        /// <summary>
        /// Disposes the resources used by log instance
        /// </summary>
        public void Dispose()
        {
            if (UdpSocket != null)
                UdpSocket.Close();
            CvarStartMsg = null;
            ServerCvar = null;
            CvarEndMsg = null;
            LogFileStarted = null;
            LogFileClosed = null;
            MapLoaded = null;
            MapStarted = null;
            RconMsg = null;
            ServerName = null;
            ServerSay = null;
            PlayerConnected = null;
            PlayerValidated = null;
            PlayerEnteredGame = null;
            PlayerDisConnected = null;
            PlayerKicked = null;
            PlayerSuicided = null;
            PlayerJoinedTeam = null;
            PlayerChangedRole = null;
            PlayerChangedName = null;
            PlayerKilled = null;
            PlayerInjured = null;
            PlayerOnPLayerTriggered = null;
            PlayerTriggered = null;
            TeamTriggered = null;
            WorldTriggered = null;
            Say = null;
            TeamSay = null;
            TeamAlliance = null;
            TeamScoreReport = null;
            PrivateChat = null;
            PlayerScoreReport = null;
            PlayerSelectedWeapon = null;
            PlayerAcquiredWeapon = null;
            ShutDown = null;
            Exception = null;
        }


        //Server cvars start    [001.1]
        protected virtual void OnCvarStart(DateTime Timestamp)
        {
            CvarStartMsg.Fire(ServerEndPoint, new LogEventArgs() { Timestamp = Timestamp });
        }

        //Server cvar "var" = "value"   [001.2]
        protected virtual void OnServerCvar(DateTime Timestamp, string[] info)
        {
            CvarEventArgs eventArgs = new CvarEventArgs()
            {
                Timestamp = Timestamp,
                Cvar = info[1],
                Value = info[3]
            };
            ServerCvar.Fire(ServerEndPoint, eventArgs);
        }


        //Server cvars end  [001.3]
        protected virtual void OnCvarEnd(DateTime Timestamp)
        {
            CvarEndMsg.Fire(ServerEndPoint, new LogEventArgs() { Timestamp = Timestamp });
        }

        //Log file started (file "filename") (game "game") (version "protocol/release/build")   [002.1]
        protected virtual void OnLogFileStart(DateTime Timestamp, string[] info)
        {
            string[] tmp = info[5].Split('/');
            LogStartEventArgs eventArgs = new LogStartEventArgs()
            {
                Timestamp = Timestamp,
                FileName = info[1],
                Game = info[3],
                Protocol = tmp[0],
                Release = tmp[1],
                Build = tmp[2]
            };
            LogFileStarted.Fire(ServerEndPoint, eventArgs);
        }

        //Log file closed   [002.2]
        protected virtual void OnLogFileClose(DateTime Timestamp)
        {
            LogFileClosed.Fire(ServerEndPoint, new LogEventArgs() { Timestamp = Timestamp });
        }

        //Loading map "map"  [003.1]
        protected virtual void OnMapLoading(DateTime Timestamp, string[] info)
        {
            MapLoadEventArgs eventArgs = new MapLoadEventArgs()
            {
                Timestamp = Timestamp,
                MapName = info[1]
            };
            MapLoaded.Fire(ServerEndPoint, eventArgs);
        }
        //Started map "map" (CRC "crc") [003.2]
        protected virtual void OnMapStart(DateTime Timestamp, string[] info)
        {
            MapStartEventArgs eventArgs = new MapStartEventArgs()
            {
                Timestamp = Timestamp,
                MapName = info[1],
                MapCRC = info[3]
            };
            MapStarted.Fire(ServerEndPoint, eventArgs);
        }
        //Rcon: "rcon challenge "password" command" from "ip:port"  [004.1]
        //Bad Rcon: "rcon challenge "password" command" from "ip:port"  [004.2]
        protected virtual void OnRconMsg(DateTime Timestamp, string[] info)
        {
            string[] s = info[5].Split(':');
            RconEventArgs eventArgs = new RconEventArgs()
            {
                Timestamp = Timestamp,
                IsValid = info[0] == "Rcon: " ? true : false,
                Challenge = info[1].Split(' ')[1],
                Password = info[2],
                Command = info[3],
                Ip = s[0],
                Port = ushort.Parse(s[1])

            };
            RconMsg.Fire(ServerEndPoint, eventArgs);
        }

        //Server name is "hostname" [005]
        protected virtual void OnserverName(DateTime Timestamp, string[] info)
        {
            ServerNameEventArgs eventArgs = new ServerNameEventArgs()
            {
                Timestamp = Timestamp,
                Name = info[1]
            };
            ServerName.Fire(ServerEndPoint, eventArgs);
        }
        //Server say "message"  [006]
        protected virtual void OnServerSay(DateTime Timestamp, string[] info)
        {
            ServerSayEventArgs eventArgs = new ServerSayEventArgs()
            {
                Timestamp = Timestamp,
                Message = info[1]
            };
            ServerSay.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><>" connected, address "ip:port"  [50]
        protected virtual void OnConnection(DateTime Timestamp, string[] info)
        {
            string[] s = info[2].Split(':');
            ConnectEventArgs eventArgs = new ConnectEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Ip = s[0],
                Port = ushort.Parse(s[1])
            };
            PlayerConnected.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><>" STEAM USERID validated   [50b]
        protected virtual void OnValidation(DateTime Timestamp, string[] info)
        {
            PlayerEventArgs eventArgs = new PlayerEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
            };
            PlayerValidated.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><>" entered the game [51]
        protected virtual void OnEnterGame(DateTime Timestamp, string[] info)
        {
            PlayerEventArgs eventArgs = new PlayerEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
            };
            PlayerEnteredGame.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" disconnected [52]
        protected virtual void OnDisconnection(DateTime Timestamp, string[] info)
        {
            PlayerEventArgs eventArgs = new PlayerEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
            };
            PlayerDisConnected.Fire(ServerEndPoint, eventArgs);
        }
        //Kick: "Name<uid><wonid><>" was kicked by "Console" (message "") [52b]
        protected virtual void OnKick(DateTime Timestamp, string[] info)
        {
            KickEventArgs eventArgs = new KickEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[1]),
                Kicker = info[3],
                Message = info.Length == 7 ? info[5] : string.Empty
            };
            PlayerKicked.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" committed suicide with "weapon" [53]
        protected virtual void OnSuicide(DateTime Timestamp, string[] info)
        {
            SuicideEventArgs eventArgs = new SuicideEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Weapon = info[2]
            };
            PlayerSuicided.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" joined team "team"  [54]
        protected virtual void OnTeamSelection(DateTime Timestamp, string[] info)
        {
            TeamSelectionEventArgs eventArgs = new TeamSelectionEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Team = info[2]
            };
            PlayerJoinedTeam.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" changed role to "role"    [55]
        protected virtual void OnRoleSelection(DateTime Timestamp, string[] info)
        {
            RoleSelectionEventArgs eventArgs = new RoleSelectionEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Role = info[2]
            };
            PlayerChangedRole.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" changed name to "Name" [56]
        protected virtual void OnNameChange(DateTime Timestamp, string[] info)
        {
            NameChangeEventArgs eventArgs = new NameChangeEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                NewName = info[2]
            };
            PlayerChangedName.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" killed "Name<uid><wonid><team>" with "weapon" [57]
        protected virtual void OnKill(DateTime Timestamp, string[] info)
        {
            KillEventArgs eventArgs = new KillEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Victim = GetPlayerInfo(info[2]),
                Weapon = info[4]
            };
            PlayerKilled.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" attacked "Name<uid><wonid><team>" with "weapon" (damage "damage") [58]
        protected virtual void OnInjure(DateTime Timestamp, string[] info)
        {
            InjureEventArgs eventArgs = new InjureEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Victim = GetPlayerInfo(info[2]),
                Weapon = info[4],
                Damage = info[6]
            };
            PlayerInjured.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" triggered "action" against "Name<uid><wonid><team>" [59]
        protected virtual void OnPlayer_PlayerAction(DateTime Timestamp, string[] info)
        {
            PlayerOnPlayerEventArgs eventArgs = new PlayerOnPlayerEventArgs()
            {
                Timestamp = Timestamp,
                Source = GetPlayerInfo(info[0]),
                Action = info[2],
                Target = GetPlayerInfo(info[4])
            };
            PlayerOnPLayerTriggered.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" triggered "action" [60]
        protected virtual void OnPlayerAction(DateTime Timestamp, string[] info)
        {
            string s = string.Empty;
            if (info.Length > 3)
            {
                for (int i = 3; i < info.Length; i++)
                    s += info[i];
            }
            PlayerActionEventArgs eventArgs = new PlayerActionEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Action = info[2],
                ExtraInfo = s
            };
            PlayerTriggered.Fire(ServerEndPoint, eventArgs);
        }

        //Team "team" triggered "action" [61]
        protected virtual void OnTeamAction(DateTime Timestamp, string[] info)
        {
            TeamActionEventArgs eventArgs = new TeamActionEventArgs()
            {
                Timestamp = Timestamp,
                Team = info[1],
                Action = info[3]
            };
            TeamTriggered.Fire(ServerEndPoint, eventArgs);
        }

        //World triggered "action" [62]
        protected virtual void OnWorldAction(DateTime Timestamp, string[] info)
        {
            WorldActionEventArgs eventArgs = new WorldActionEventArgs()
            {
                Timestamp = Timestamp,
                Action = info[1]
            };
            WorldTriggered.Fire(ServerEndPoint, eventArgs);
        }
        //"Name<uid><wonid><team>" say "message" [63.1]
        protected virtual void OnSay(DateTime Timestamp, string[] info)
        {
            ChatEventArgs eventArgs = new ChatEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Message = info.Length == 3 ? info[2] : string.Empty
            };
            Say.Fire(ServerEndPoint, eventArgs);
        }

        // "Name<uid><wonid><team>" say_team "message" [63.2]
        protected virtual void OnTeamSay(DateTime Timestamp, string[] info)
        {
            ChatEventArgs eventArgs = new ChatEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Message = info.Length == 3 ? info[2] : string.Empty
            };
            TeamSay.Fire(ServerEndPoint, eventArgs);
        }

        //Team "team" formed alliance with team "team"   [64]
        protected virtual void OnTeamAlliance(DateTime Timestamp, string[] info)
        {
            TeamAllianceEventArgs eventArgs = new TeamAllianceEventArgs()
            {
                Timestamp = Timestamp,
                Team1 = info[1],
                Team2 = info[3]
            };
            TeamAlliance.Fire(ServerEndPoint, eventArgs);
        }

        //Team "team" scored "score" with "numplayers" players + extra info  [65]
        protected virtual void OnTeamScoreReport(DateTime Timestamp, string[] info)
        {
            string details = string.Empty;
            if (info.Length > 6)
            {
                for (int i = 6; i < info.Length; i++)
                    details += info[i];
            }
            TeamScoreReportEventArgs eventArgs = new TeamScoreReportEventArgs()
            {
                Timestamp = Timestamp,
                Team = info[1],
                Score = info[3],
                PlayerCount = info[5],
                ExtraInfo = details
            };
            TeamScoreReport.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" tell "Name<uid><wonid><team>" message "message"   [66]
        protected virtual void OnPrivateChat(DateTime Timestamp, string[] info)
        {
            PrivateChatEventArgs eventArgs = new PrivateChatEventArgs()
            {
                Timestamp = Timestamp,
                Sender = GetPlayerInfo(info[0]),
                Receiver = GetPlayerInfo(info[2]),
                Message = info.Length == 5 ? info[4] : string.Empty
            };
            PrivateChat.Fire(ServerEndPoint, eventArgs);
        }

        //Player "Name<uid><wonid><team>" scored "score" + extra info   [67]
        protected virtual void OnPlayerScoreReport(DateTime Timestamp, string[] info)
        {
            string details = string.Empty;
            if (info.Length > 4)
            {
                for (int i = 4; i < info.Length; i++)
                    details += info[i];
            }
            PlayerScoreReportEventArgs eventArgs = new PlayerScoreReportEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[1]),
                Score = info[3],
                ExtraInfo = details

            };
            PlayerScoreReport.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" selected weapon "weapon"     [68]
        protected virtual void OnWeaponSelection(DateTime Timestamp, string[] info)
        {
            WeaponEventArgs eventArgs = new WeaponEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Weapon = info[2]
            };
            PlayerSelectedWeapon.Fire(ServerEndPoint, eventArgs);
        }

        //"Name<uid><wonid><team>" acquired weapon "weapon"  [69]
        protected virtual void OnWeaponPickup(DateTime Timestamp, string[] info)
        {
            WeaponEventArgs eventArgs = new WeaponEventArgs()
            {
                Timestamp = Timestamp,
                Player = GetPlayerInfo(info[0]),
                Weapon = info[2]
            };
            PlayerAcquiredWeapon.Fire(ServerEndPoint, eventArgs);
        }

        protected virtual void OnShutDown(DateTime Timestamp)
        {
            ShutDown.Fire(ServerEndPoint, new LogEventArgs() { Timestamp = Timestamp });
        }
        protected virtual void OnException(DateTime Timestamp, string info)
        {
            ExceptionEventArgs eventArgs = new ExceptionEventArgs()
            {
                Timestamp = Timestamp,
                LogLine = info
            };
            Exception.Fire(ServerEndPoint, eventArgs);

        }

        //Name<uid><wonid><team>
        private PlayerInfo GetPlayerInfo(string s)
        {
            Match match = RegPlayer.Match(s);
            PlayerInfo info = new PlayerInfo()
            {
                Name = match.Groups[1].Value,
                Uid = match.Groups[2].Value,
                WonId = match.Groups[3].Value,
                Team = match.Groups[4].Value,
            };
            return info;
        }

    }
}
