using NeXt.Vdf;
using NLog;
using ServerManagerTool.Common.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ServerManagerTool.Common.Utils
{
    public static class SteamUtils
    {
        private const string KEYWORK_QUIT = "+quit";

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static WorkshopFileDetailResponse GetSteamModDetails(string appId)
        {
            const int MAX_IDS = 100;

            var totalRequests = 0;
            var requestIndex = 1;

            var response = new WorkshopFileDetailResponse();
            if (string.IsNullOrWhiteSpace(SteamWebApiKey))
                return response;

            try
            {
                do
                {
                    var httpRequest = WebRequest.Create($"https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?key={SteamWebApiKey}&format=json&query_type=1&page={requestIndex}&numperpage={MAX_IDS}&appid={appId}&match_all_tags=0&include_recent_votes_only=0&totalonly=0&return_vote_data=0&return_tags=0&return_kv_tags=0&return_previews=0&return_children=0&return_short_description=0&return_for_sale_data=0&return_metadata=1");
                    httpRequest.Timeout = 30000;
                    var httpResponse = httpRequest.GetResponse();
                    var responseString = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();

                    var result = JsonUtils.Deserialize<WorkshopFileDetailResult>(responseString);
                    if (result == null || result.response == null)
                        break;

                    if (totalRequests == 0)
                    {
                        totalRequests = 1;
                        response = result.response;

                        if (response.total > MAX_IDS)
                        {
                            int remainder;
                            totalRequests = Math.DivRem(response.total, MAX_IDS, out remainder);
                            if (remainder > 0)
                                totalRequests++;
                        }
                    }
                    else
                    {
                        if (result.response.publishedfiledetails != null)
                            response.publishedfiledetails.AddRange(result.response.publishedfiledetails);
                    }

                    requestIndex++;
                } while (requestIndex <= totalRequests);

                return response;
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(GetSteamModDetails)}. {ex.Message}\r\n{ex.StackTrace}");
                Debug.WriteLine($"ERROR: {nameof(GetSteamModDetails)}\r\n{ex.Message}");
                return null;
            }
        }

        public static PublishedFileDetailsResponse GetSteamModDetails(List<string> modIdList)
        {
            return GetSteamModDetails(new List<(string AppId, List<string> ModIdList)>()
            {
                ("", modIdList),
            });
        }

        public static PublishedFileDetailsResponse GetSteamModDetails(List<(string AppId, List<string> ModIdList)> appMods)
        {
            const int MAX_IDS = 20;

            PublishedFileDetailsResponse response = null;
            if (string.IsNullOrWhiteSpace(SteamWebApiKey))
                return response;

            try
            {
                if (appMods == null || appMods.Count == 0)
                    return new PublishedFileDetailsResponse();

                foreach (var appMod in appMods)
                {
                    if (appMod.ModIdList.Count == 0)
                        continue;

                    int remainder;
                    var totalRequests = Math.DivRem(appMod.ModIdList.Count, MAX_IDS, out remainder);
                    if (remainder > 0)
                        totalRequests++;

                    var requestIndex = 0;
                    while (requestIndex < totalRequests)
                    {
                        var count = 0;
                        var postData = "";
                        for (var index = requestIndex * MAX_IDS; count < MAX_IDS && index < appMod.ModIdList.Count; index++)
                        {
                            postData += $"&publishedfileids[{count}]={appMod.ModIdList[index]}";
                            count++;
                        }

                        postData = $"key={SteamWebApiKey}&format=json&itemcount={count}{postData}";

                        var data = Encoding.ASCII.GetBytes(postData);

                        var httpRequest = WebRequest.Create("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/");
                        httpRequest.Timeout = 30000;
                        httpRequest.Method = "POST";
                        httpRequest.ContentType = "application/x-www-form-urlencoded";
                        httpRequest.ContentLength = data.Length;

                        using (var stream = httpRequest.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                        }

                        var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                        var responseString = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();

                        var result = JsonUtils.Deserialize<PublishedFileDetailsResult>(responseString);
                        if (result != null && result.response != null)
                        {
                            if (response == null)
                                response = result.response;
                            else
                            {
                                response.resultcount += result.response.resultcount;
                                response.publishedfiledetails.AddRange(result.response.publishedfiledetails);
                            }
                        }

                        requestIndex++;
                    };
                }

                return response ?? new PublishedFileDetailsResponse();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(GetSteamModDetails)}. {ex.Message}\r\n{ex.StackTrace}");
                Debug.WriteLine($"ERROR: {nameof(GetSteamModDetails)}\r\n{ex.Message}");
                return null;
            }
        }

        public static SteamUserDetailResponse GetSteamUserDetails(List<string> steamIdList)
        {
            const int MAX_IDS = 100;

            SteamUserDetailResponse response = null;
            if (string.IsNullOrWhiteSpace(SteamWebApiKey))
                return response;

            try
            {
                if (steamIdList.Count == 0)
                    return new SteamUserDetailResponse();

                steamIdList = steamIdList.Distinct().ToList();
                steamIdList = steamIdList.Where(i => long.TryParse(i, out long id)).ToList();

                if (steamIdList.Count == 0)
                    return new SteamUserDetailResponse();

                int remainder;
                var totalRequests = Math.DivRem(steamIdList.Count, MAX_IDS, out remainder);
                if (remainder > 0)
                    totalRequests++;

                var requestIndex = 0;
                while (requestIndex < totalRequests)
                {
                    var count = 0;
                    var postData = "";
                    var delimiter = "";
                    for (var index = requestIndex * MAX_IDS; count < MAX_IDS && index < steamIdList.Count; index++)
                    {
                        postData += $"{delimiter}{steamIdList[index]}";
                        delimiter = ",";
                        count++;
                    }

                    var httpRequest = WebRequest.Create($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={SteamWebApiKey}&format=json&steamids={postData}");
                    httpRequest.Timeout = 30000;
                    var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    var responseString = new StreamReader(httpResponse.GetResponseStream()).ReadToEnd();

                    var result = JsonUtils.Deserialize<SteamUserDetailResult>(responseString);
                    if (result != null && result.response != null)
                    {
                        if (response == null)
                            response = result.response;
                        else
                        {
                            response.players.AddRange(result.response.players);
                        }
                    }

                    requestIndex++;
                }

                return response ?? new SteamUserDetailResponse();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(GetSteamUserDetails)}. {ex.Message}\r\n{ex.StackTrace}");
                Debug.WriteLine($"ERROR: {nameof(GetSteamUserDetails)}\r\n{ex.Message}");
                return null;
            }
        }

        public static SteamCmdAppManifest ReadSteamCmdAppManifestFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            var vdfSerializer = VdfDeserializer.FromFile(file);
            var vdf = vdfSerializer.Deserialize();

            return SteamCmdManifestDetailsResult.Deserialize(vdf);
        }

        public static SteamCmdAppWorkshop ReadSteamCmdAppWorkshopFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            var vdfSerializer = VdfDeserializer.FromFile(file);
            var vdf = vdfSerializer.Deserialize();

            return SteamCmdWorkshopDetailsResult.Deserialize(vdf);
        }

        public static string SteamWebApiKey
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CommonConfig.Default.SteamAPIKey))
                    return CommonConfig.Default.SteamAPIKey;
                return CommonConfig.Default.DefaultSteamAPIKey;
            }
        }

        public static Process GetSteamProcess()
        {
            if (string.IsNullOrWhiteSpace(CommonConfig.Default.SteamClientFile) || !File.Exists(CommonConfig.Default.SteamClientFile))
                return null;

            // Find the server process.
            var expectedPath = IOUtils.NormalizePath(CommonConfig.Default.SteamClientFile);
            var runningProcesses = Process.GetProcessesByName(CommonConfig.Default.SteamProcessName);

            Process process = null;
            foreach (var runningProcess in runningProcesses)
            {
                var runningPath = ProcessUtils.GetMainModuleFilepath(runningProcess.Id);
                if (string.Equals(expectedPath, runningPath, StringComparison.OrdinalIgnoreCase))
                {
                    process = runningProcess;
                    break;
                }
            }

            return process;
        }

        public static string BuildSteamCmdArguments(bool removeQuit, string argumentString)
        {
            if (string.IsNullOrWhiteSpace(argumentString))
                return argumentString;

            var newArgumentString = argumentString.TrimEnd(' ');

            if (newArgumentString.ToLower().EndsWith(KEYWORK_QUIT) && removeQuit)
                return newArgumentString.Substring(0, newArgumentString.Length - KEYWORK_QUIT.Length);
            else if (!newArgumentString.ToLower().EndsWith(KEYWORK_QUIT) && !removeQuit)
                return newArgumentString += $" {KEYWORK_QUIT}";

            return newArgumentString.TrimEnd(' ');
        }

        public static string BuildSteamCmdArguments(bool removeQuit, string argumentFormatString, params string[] argumentValues)
        {
            if (string.IsNullOrWhiteSpace(argumentFormatString) || argumentValues == null || argumentValues.Length == 0)
                return argumentFormatString;

            var argumentString = string.Format(argumentFormatString, argumentValues);
            return BuildSteamCmdArguments(removeQuit, argumentString);
        }

        public static List<int> GetExitStatusList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<int>();
            }

            return new List<int>(Array.ConvertAll(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), int.Parse));
        }
    }
}
