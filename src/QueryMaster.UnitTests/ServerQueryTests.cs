using System;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QueryMaster.UnitTests
{
    [TestClass]
    public class ServerQueryTests
    {
        private static readonly char[] rconLineSplitChars = new char[] { '\n' };

        [DataTestMethod]
        [DataRow("127.0.0.1", 27015)]
        [DataRow("192.168.0.1", 27015)]
        [DataRow("101.188.183.193", 27015)]
        public void ServerQuery_PlayerCount(string ipAddressString, int queryPort)
        {
            var ipAddress = IPAddress.Parse(ipAddressString);
            var endPoint = new IPEndPoint(ipAddress, queryPort);

            using (var gameServer = ServerQuery.GetServerInstance(EngineType.Source, endPoint))
            {
                Assert.IsNotNull(gameServer);

                var serverInfo = gameServer.GetInfo();
                Assert.IsNotNull(serverInfo);

                var playerCount1 = serverInfo.Players;

                var players = gameServer.GetPlayers();
                Assert.IsNotNull(players);

                var validPlayers = players.Where(p => !string.IsNullOrWhiteSpace(p.Name?.Trim()));
                var playerCount2 = validPlayers.Count();

                Assert.AreEqual(playerCount1, playerCount2);
            }
        }

        [DataTestMethod]
        [DataRow("127.0.0.1", 32330, "p@ssword")]
        [DataRow("192.168.0.1", 32330, "p@ssword")]
        [DataRow("101.188.183.193", 32330, "p@ssword")]
        public void ServerQuery_RconListPlayers(string ipAddressString, int queryPort, string rconPassword)
        {
            var ipAddress = IPAddress.Parse(ipAddressString);
            var endPoint = new IPEndPoint(ipAddress, queryPort);

            using (var gameServer = ServerQuery.GetServerInstance(EngineType.Source, endPoint))
            {
                Assert.IsNotNull(gameServer);

                using (var rconConsole = gameServer.GetControl(rconPassword))
                {
                    Assert.IsNotNull(rconConsole);

                    var result = rconConsole.SendCommand("listplayers");
                    Assert.IsNotNull(result);

                    var lines = result.Split(rconLineSplitChars, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToArray();
                    Assert.IsNotNull(lines);
                }
            }
        }
    }
}
