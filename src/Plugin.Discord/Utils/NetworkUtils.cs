using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace ServerManagerTool.Plugin.Discord
{
    internal static class NetworkUtils
    {
        public static async Task<IPAddress> DiscoverPublicIPAsync()
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    var publicIP = await webClient.DownloadStringTaskAsync(Config.Default.PublicIPCheckUrl1);
                    if (IPAddress.TryParse(publicIP, out IPAddress address1))
                    {
                        return address1;
                    }

                    publicIP = await webClient.DownloadStringTaskAsync(Config.Default.PublicIPCheckUrl2);
                    if (IPAddress.TryParse(publicIP, out IPAddress address2))
                    {
                        return address2;
                    }

                    return IPAddress.None;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR: {nameof(DiscoverPublicIPAsync)}\r\n{ex.Message}");
                    return IPAddress.None;
                }
            }
        }

        public static async Task<Version> CheckLatestVersionAsync(bool betaEnabled)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    string latestVersion = null;

                    if (betaEnabled)
                        latestVersion = await webClient.DownloadStringTaskAsync(Config.Default.LatestBetaVersionUrl);
                    else
                        latestVersion = await webClient.DownloadStringTaskAsync(Config.Default.LatestVersionUrl);

                    if (Version.TryParse(latestVersion, out Version version))
                        return version;

                    return new Version();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(CheckLatestVersionAsync)}\r\n{ex.Message}");
                return new Version();
            }
        }

        public static bool DownloadLatestVersion(string sourceUrl, string destinationFile)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(sourceUrl, destinationFile);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {nameof(DownloadLatestVersion)}\r\n{ex.Message}");
                return false;
            }
        }

        public static async Task PerformCallToAPIAsync(string pluginCode, IPAddress ipAddress)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var url = string.Format(Config.Default.PluginCallUrlFormat, pluginCode, ipAddress);
                    await client.DownloadStringTaskAsync(url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{nameof(PerformCallToAPIAsync)} - Failed calling API.\r\n{ex.Message}");
            }
        }
    }
}
