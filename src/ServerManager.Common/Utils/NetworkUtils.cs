using Newtonsoft.Json.Linq;
using NLog;
using ServerManagerTool.Common.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ServerManagerTool.Common.Utils
{
    public static class NetworkUtils
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static List<NetworkAdapterEntry> GetAvailableIPV4NetworkAdapters()
        {
            List<NetworkAdapterEntry> adapters = new List<NetworkAdapterEntry>();
            
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach(var ifc in interfaces)
            {
                var ipProperties = ifc.GetIPProperties();
                if (ipProperties != null)
                {
                    adapters.AddRange(ipProperties.UnicastAddresses.Select(a => a.Address)
                                                                   .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                                                                   .Select(a => new NetworkAdapterEntry(a, ifc.Description)));
                }
            }

            return adapters;
        }

        public static async Task<Version> GetLatestServerManagerVersion(string url)
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    string latestVersion = await webClient.DownloadStringTaskAsync(url);
                    return Version.Parse(latestVersion);
                }
                catch (Exception ex)
                {
                    _logger.Error($"{nameof(GetLatestServerManagerVersion)} - Exception checking for Server Manager version. {ex.Message}");
                    return new Version();
                }
            }
        }

        public static NetworkAdapterEntry GetPreferredIP(IEnumerable<NetworkAdapterEntry> adapters)
        {
            //
            // Try for a 192.168. address first
            //
            var preferredIp = adapters.FirstOrDefault(a => a.IPAddress.StartsWith("192.168."));
            if (preferredIp == null)
            {
                //
                // Try a 10.0 address next
                //
                preferredIp = adapters.FirstOrDefault(a => a.IPAddress.StartsWith("10.0."));
                if (preferredIp == null)
                {
                    // 
                    // Sad.  Just take the first.
                    //
                    preferredIp = adapters.FirstOrDefault();
                }
            }

            return preferredIp;
        }

        public static string DiscoverPublicIP()
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    var publicIP = webClient.DownloadString(CommonConfig.Default.PublicIPCheckUrl1);
                    if (IPAddress.TryParse(publicIP, out IPAddress address1))
                    {
                        return publicIP;
                    }

                    publicIP = webClient.DownloadString(CommonConfig.Default.PublicIPCheckUrl2);
                    if (IPAddress.TryParse(publicIP, out IPAddress address2))
                    {
                        return publicIP;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"{nameof(DiscoverPublicIP)} - Exception checking for public ip. {ex.Message}");
                }

                return String.Empty;
            }
        }

        public static async Task<string> DiscoverPublicIPAsync()
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    var publicIP = await webClient.DownloadStringTaskAsync(CommonConfig.Default.PublicIPCheckUrl1);
                    if (IPAddress.TryParse(publicIP, out IPAddress address1))
                    {
                        return publicIP;
                    }

                    publicIP = await webClient.DownloadStringTaskAsync(CommonConfig.Default.PublicIPCheckUrl2);
                    if (IPAddress.TryParse(publicIP, out IPAddress address2))
                    {
                        return publicIP;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"{nameof(DiscoverPublicIPAsync)} - Exception checking for public ip. {ex.Message}");
                }

                return String.Empty;
            }
        }

        public static async Task<bool> CheckServerStatusViaAPI(Uri uri, IPEndPoint endpoint)
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
                    _logger.Debug($"Server info request returned null string for {endpoint.Address}:{endpoint.Port}");
                    return false;
                }

                JObject query = JObject.Parse(jsonString);
                if (query == null)
                {
                    _logger.Debug($"Server info request failed to parse for {endpoint.Address}:{endpoint.Port} - '{jsonString}'");
                    return false;
                }

                var available = query.SelectToken("available");
                if (available == null)
                {
                    _logger.Debug($"Server at {endpoint.Address}:{endpoint.Port} returned no availability.");
                    return false;
                }

                return (bool)available;
            }
            catch (Exception ex)
            {
                _logger.Debug($"{nameof(CheckServerStatusViaAPI)} - Failed checking status via API for: {endpoint.Address}:{endpoint.Port}. {ex.Message}");
                return false;
            }
        }

        public static async Task PerformServerCallToAPI(Uri uri, IPEndPoint endpoint)
        {
            try
            {
                using (var client = new WebClient())
                {
                    await client.DownloadStringTaskAsync(uri);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"{nameof(PerformServerCallToAPI)} - Failed calling API for: {endpoint.Address}:{endpoint.Port}. {ex.Message}");
            }
        }
    }
}
