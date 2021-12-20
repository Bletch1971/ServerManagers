using ServerManagerTool.Plugin.Common;
using ServerManagerTool.Plugin.Discord.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;

namespace ServerManagerTool.Plugin.Discord
{
    public sealed class DiscordPlugin : IAlertPlugin, IBeta
    {
        private const int MAX_MESSAGE_LENGTH = 1980; // 2000 minus some formatting characters

        private Object lockObject = new Object();

        public DiscordPlugin()
        {
            BetaEnabled = false;
        }

        private DiscordPluginConfig PluginConfig
        {
            get;
            set;
        }

        public bool BetaEnabled
        {
            get;
            set;
        }

        public bool Enabled => true;

        public string ConfigFile => Path.Combine(PluginHelper.PluginFolder, Config.Default.ConfigFile);

        public string PluginCode => Config.Default.PluginCode;

        public string PluginName => Config.Default.PluginName;

        public Version PluginVersion
        {
            get
            {
                try
                {
                    return Assembly.GetExecutingAssembly().GetName().Version;
                }
                catch
                {
                    return new Version();
                }
            }
        }

        public bool HasConfigForm => true;

        public string LanguageCode
        {
            get
            {
                var assembly = typeof(PluginHelper).Assembly;
                if (assembly != null)
                {
                    var pluginHelperType = assembly.GetType(typeof(PluginHelper).FullName, false, true);
                    if (pluginHelperType != null)
                    {
                        var property = pluginHelperType.GetProperty(nameof(LanguageCode));
                        if (property != null)
                        {
                            return property.GetValue(PluginHelper.Instance).ToString();
                        }
                    }
                }

                return "en-US";
            }
        }

        private async Task CallHomeAsync()
        {
            try
            {
                var publicIP = await NetworkUtils.DiscoverPublicIPAsync();
                await NetworkUtils.PerformCallToAPIAsync(PluginCode, publicIP);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed calling home to API.\r\n{ex.Message}");
            }
        }

        public void HandleAlert(AlertType alertType, string profileName, string alertMessage)
        {
            if (string.IsNullOrWhiteSpace(alertMessage))
                return;

            lock (lockObject)
            {
                var configProfiles = PluginConfig.ConfigProfiles.Where(cp => cp.IsEnabled
                                                                    && cp.AlertTypes.Any(pn => pn.Value.Equals(alertType))
                                                                    && cp.ProfileNames.Any(pn => pn.Value.Equals(profileName, StringComparison.OrdinalIgnoreCase))
                                                                    && !string.IsNullOrWhiteSpace(cp.DiscordWebhookUrl));
                if (configProfiles == null || configProfiles.IsEmpty())
                {
                    return;
                }

                foreach (var configProfile in configProfiles)
                {
                    HandleAlert(configProfile, alertType, profileName, alertMessage);
                }
            }
        }

        internal void HandleAlert(ConfigProfile configProfile, AlertType alertType, string profileName, string alertMessage)
        {
            if (configProfile == null || string.IsNullOrWhiteSpace(configProfile.DiscordWebhookUrl) || string.IsNullOrWhiteSpace(alertMessage))
                return;

            // remove any bad characters
            var formattedProfileName = profileName?.Replace("&", "_") ?? string.Empty;
            var formattedAlertMessage = alertMessage?.Replace("&", "_") ?? string.Empty;

            // check if we need to add the profile name to the message
            if (configProfile.PrefixMessageWithProfileName && !string.IsNullOrWhiteSpace(formattedProfileName))
                formattedAlertMessage = $"({formattedProfileName}) {formattedAlertMessage}";

            // check if the message is too long
            if (formattedAlertMessage.Length > MAX_MESSAGE_LENGTH)
                formattedAlertMessage = $"{formattedAlertMessage.Substring(0, MAX_MESSAGE_LENGTH - 3)}...";

            // check if we need to apply any styles to the message
            if (configProfile.MessageCodeBlock)
                formattedAlertMessage = $"```{formattedAlertMessage}```";
            if (configProfile.MessageBold)
                formattedAlertMessage = $"**{formattedAlertMessage}**";
            if (configProfile.MessageItalic)
                formattedAlertMessage = $"*{formattedAlertMessage}*";
            if (configProfile.MessageUnderlined)
                formattedAlertMessage = $"__{formattedAlertMessage}__";
            formattedAlertMessage = HttpUtility.UrlEncode(formattedAlertMessage);

            var postData = string.Empty;

            if (configProfile.DiscordUseTTS)
                postData += $"&tts={configProfile.DiscordUseTTS}";
            if (!string.IsNullOrWhiteSpace(configProfile.DiscordBotName))
                postData += $"&username={configProfile.DiscordBotName.Replace("&", "_")}";
            postData += $"&content={formattedAlertMessage}";

            try
            {
                var data = Encoding.UTF8.GetBytes(postData);

                var url = configProfile.DiscordWebhookUrl;
                url = url.Trim();
                if (url.EndsWith("/"))
                    url = url.Substring(0, url.Length - 1);

                var httpRequest = WebRequest.Create($"{url}?wait=true");
                httpRequest.Timeout = Config.Default.RequestTimeout;
                httpRequest.Method = "POST";
                httpRequest.ContentType = "application/x-www-form-urlencoded";
                httpRequest.ContentLength = data.Length;

                using (var stream = httpRequest.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                var responseString = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    Debug.WriteLine($"{nameof(HandleAlert)}\r\nResponse: {responseString}");
                }
                else
                {
                    Debug.WriteLine($"{nameof(HandleAlert)}\r\n{httpResponse.StatusCode}: {responseString}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(HandleAlert)}\r\n{ex.Message}");
            }
        }

        public void Initialize()
        {
            LoadConfig();

            if (PluginConfig.PluginCallUrlLast.AddHours(Config.Default.PluginCallUrlDelay) < DateTime.Now)
            {
                CallHomeAsync().DoNotWait();

                PluginConfig.PluginCallUrlLast = DateTime.Now;
                SaveConfig();
            }
        }

        internal void BackupConfig()
        {
            if (!File.Exists(ConfigFile))
                return;

            var backupFile = Path.ChangeExtension(ConfigFile, "bak");
            File.Copy(ConfigFile, backupFile, true);
        }

        internal void LoadConfig()
        {
            PluginConfig = null;

            if (File.Exists(ConfigFile))
            {
                PluginConfig = JsonUtils.DeserializeFromFile<DiscordPluginConfig>(ConfigFile);
            }
            else
            {
                PluginConfig = new DiscordPluginConfig();
                SaveConfig();
            }

            if (PluginConfig is null)
            {
                throw new Exception("Profile config file could not be loaded.");
            }

            PluginConfig.CommitChanges();
        }

        public void OpenConfigForm(Window owner)
        {
            var window = new ConfigWindow(this, PluginConfig);
            window.Owner = owner;
            window.ShowDialog();

            LoadConfig();
        }

        internal void SaveConfig()
        {
            JsonUtils.SerializeToFile(PluginConfig, ConfigFile);
            PluginConfig.CommitChanges();
        }
    }
}
