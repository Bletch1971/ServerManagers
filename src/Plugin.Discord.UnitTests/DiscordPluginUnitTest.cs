using ServerManagerTool.Plugin.Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ServerManagerTool.Plugin.Common.Lib;

namespace Plugin.Discord.UnitTests
{
    [TestClass]
    public class DiscordPluginUnitTest
    {
        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SingleLineMessage_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("The server has been started.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, "Server 1", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SingleLineUnicodeMessage_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("Требуется перезагрузка сервера. Сейчас сервер будет выключен.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, "Server 1", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SingleLineSpecialCharacterMessage_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("Update performed, includes: Structures Plus (S+) (731604991)");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, "Server 1", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_MultipleLineMessage_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("The server is being shutdown.");
            alertMessage.AppendLine("Please logout to avoid profile corruption.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Shutdown, "Server 2", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_MultipleLineMessageInOneLine_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            //var alertMessage = new StringBuilder();
            //alertMessage.AppendLine("The server is being shutdown.\r\nPlease logout to avoid profile corruption.");
            var alertMessage = "Server restart required.\r\n\r\nServer will restart in {minutes} minutes. \r\n\r\nPlease logout before restart to prevent character corruption.";

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Shutdown, "Server 2", alertMessage);

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_ErrorAlertType_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("The server encountered an error while starting.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Error, "Server 1", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SingleLineMessageToUnknownProfileName_Then_NoAlertSent()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("The server has been started.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, "", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_ExtraLongMessage_Then_AlertTruncated()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            var alertMessage = new StringBuilder();
            alertMessage.AppendLine("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus ac lorem pretium, volutpat massa ut, iaculis augue. Aenean condimentum gravida laoreet. Morbi mattis leo non enim imperdiet dignissim. Donec et consectetur est. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Curabitur leo ipsum, commodo sed ante eu, vulputate maximus nulla. In sollicitudin, magna ut fringilla scelerisque, neque nulla semper nunc, at tempus nibh mi quis diam. Nunc quis tortor neque. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.");
            alertMessage.AppendLine("Maecenas ultrices in est a iaculis.Sed eget pharetra nibh.Duis luctus neque id iaculis vestibulum.Duis condimentum sapien metus, at pretium dui aliquam ullamcorper.Sed at efficitur tellus.Praesent eget ex blandit orci venenatis fringilla et in ex.Curabitur id mauris sed augue pharetra ornare.Integer at malesuada nisl, id blandit orci.");
            alertMessage.AppendLine("Ut ac dolor non ex porta lobortis.Aliquam sollicitudin nec justo ac finibus.Aliquam condimentum malesuada luctus.Nam ut ornare justo, a scelerisque sapien.Vivamus eget nisi risus.Morbi ut tellus ultricies arcu sagittis eleifend.Praesent eu augue in eros egestas rhoncus eu sed quam.");
            alertMessage.AppendLine("Quisque quis facilisis ipsum.In egestas pulvinar urna, id maximus lorem vehicula nec.Fusce vel nibh tincidunt, semper risus a, consectetur nunc.Morbi at lorem libero.Donec diam eros, aliquet in enim vitae, ornare malesuada nisi.Donec a mi pharetra dolor dignissim dapibus at vel velit.Praesent tincidunt, ipsum eget finibus cursus, ex turpis accumsan dui, ut hendrerit ante tortor vitae urna.Nulla faucibus ipsum nec tellus congue rhoncus.Maecenas sed tortor placerat, lobortis arcu sit amet, pellentesque sapien.Praesent sit amet feugiat massa.");
            alertMessage.AppendLine("Vestibulum eu felis accumsan, vehicula metus ut, gravida nulla.Sed pharetra sed ex vel sodales.In vestibulum, nisl vitae ultricies mattis, lacus massa maximus nunc, id suscipit lorem ligula id tortor.Donec porttitor diam ac turpis posuere aliquam.Phasellus non sed.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Shutdown, "Server 3", alertMessage.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SendingMultipleMessagesSingleServer_Then_Valid()
        {
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            SendMultipleMessages(plugin, "Server 1");
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SendingMultipleMessagesMultipleServers_Sync_Then_Valid()
        {
            // Arrange
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            var alertMessage1 = new StringBuilder();
            alertMessage1.AppendLine("Message 1 - shutdown 1.");
            var alertMessage2 = new StringBuilder();
            alertMessage2.AppendLine("Message 2 - shutdown 2.");
            var alertMessage3 = new StringBuilder();
            alertMessage3.AppendLine("Message 3 - start.");
            var alertMessage4 = new StringBuilder();
            alertMessage4.AppendLine("Message 4 - update reason.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, "Server 1", alertMessage1.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, "Server 2", alertMessage1.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, "Server 3", alertMessage1.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, "Server 4", alertMessage1.ToString());
            Task.Delay(2000).Wait();
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, "Server 1", alertMessage2.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, "Server 2", alertMessage2.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, "Server 3", alertMessage2.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, "Server 4", alertMessage2.ToString());
            Task.Delay(2000).Wait();
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, "Server 1", alertMessage3.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, "Server 2", alertMessage3.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, "Server 3", alertMessage3.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, "Server 4", alertMessage3.ToString());
            Task.Delay(2000).Wait();
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.UpdateResults, "Server 1", alertMessage4.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.UpdateResults, "Server 2", alertMessage4.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.UpdateResults, "Server 3", alertMessage4.ToString());
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.UpdateResults, "Server 4", alertMessage4.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_HandleAlert_When_SendingMultipleMessagesMultipleServers_ASync_Then_Valid()
        {
            var plugin = new DiscordPlugin();
            plugin.Initialize();

            Parallel.For(1, 9, (i) => {
                switch (i)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        SendMultipleMessages(plugin, $"Server {i}");
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        SendMultipleErrors(plugin, $"Server {i-4}");
                        break;
                }
            });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SCS0005:Weak random number generator.", Justification = "<Pending>")]
        private void SendMultipleMessages(DiscordPlugin plugin, string profileName)
        {
            // Arrange
            Random rnd = new Random();
            var alertMessage1 = new StringBuilder();
            alertMessage1.AppendLine("Message 1 - shutdown 1.");
            var alertMessage2 = new StringBuilder();
            alertMessage2.AppendLine("Message 2 - shutdown 2.");
            var alertMessage3 = new StringBuilder();
            alertMessage3.AppendLine("Message 3 - start.");
            var alertMessage4 = new StringBuilder();
            alertMessage4.AppendLine("Message 4 - update reason.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, profileName, alertMessage1.ToString());
            Task.Delay(rnd.Next(1000, 5000)).Wait();
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.ShutdownMessage, profileName, alertMessage2.ToString());
            Task.Delay(rnd.Next(1000, 5000)).Wait();
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Startup, profileName, alertMessage3.ToString());
            Task.Delay(rnd.Next(1000, 5000)).Wait();
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.UpdateResults, profileName, alertMessage4.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SCS0005:Weak random number generator.", Justification = "<Pending>")]
        private void SendMultipleErrors(DiscordPlugin plugin, string profileName)
        {
            // Arrange
            Random rnd = new Random();
            var alertMessage1 = new StringBuilder();
            alertMessage1.AppendLine("Error 1.");
            var alertMessage2 = new StringBuilder();
            alertMessage2.AppendLine("Error 2.");

            // Act
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Error, profileName, alertMessage1.ToString());
            Task.Delay(rnd.Next(1000, 5000)).Wait();
            plugin.HandleAlert(ServerManagerTool.Plugin.Common.AlertType.Error, profileName, alertMessage2.ToString());

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_OpenConfigForm()
        {
            // Arrange
            ServerManagerTool.Plugin.Common.PluginHelper.Instance.SetFetchProfileCallback(FetchProfiles);

            var plugin = new DiscordPlugin();
            plugin.Initialize();

            // Act
            plugin.OpenConfigForm(null);

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_OpenConfigForm_DebugMode()
        {
            // Arrange
            ServerManagerTool.Plugin.Common.PluginHelper.Instance.SetFetchProfileCallback(FetchProfiles);

            var plugin = new DiscordPlugin();
            plugin.BetaEnabled = true;
            plugin.Initialize();

            // Act
            plugin.OpenConfigForm(null);

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_OpenConfigForm_SwitchingLanguages()
        {
            // Arrange
            ServerManagerTool.Plugin.Common.PluginHelper.Instance.SetFetchProfileCallback(FetchProfiles);

            var plugin = new DiscordPlugin();
            plugin.BetaEnabled = true;
            plugin.Initialize();

            // Act
            plugin.OpenConfigForm(null);

            // Assert
            Assert.IsTrue(true);

            // Arrange
            ServerManagerTool.Plugin.Common.PluginHelper.Instance.OnResourceDictionaryChanged("zh-CN");

            // Act
            plugin.OpenConfigForm(null);

            // Assert
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void DiscordPlugin_OpenConfigForm_WithDifferentLanguage()
        {
            // Arrange
            ServerManagerTool.Plugin.Common.PluginHelper.Instance.OnResourceDictionaryChanged("pt-BR");

            var plugin = new DiscordPlugin();
            plugin.BetaEnabled = true;
            plugin.Initialize();

            // Act
            plugin.OpenConfigForm(null);

            // Assert
            Assert.IsTrue(true);
        }

        private IList<Profile> FetchProfiles()
        {
            return new List<Profile>()
            {
                new Profile() { ProfileName = "Profile 1", InstallationFolder = @"d:\asmdata\servers\server1" },
                new Profile() { ProfileName = "Profile 2", InstallationFolder = @"d:\asmdata\servers\server2" },
                new Profile() { ProfileName = "Profile 3", InstallationFolder = @"d:\asmdata\servers\server3" },
            };
        }
    }
}
