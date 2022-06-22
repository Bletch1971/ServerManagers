using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ArkData
{
    /// <summary>
    /// The container for the data.
    /// </summary>
    public partial class DataContainer
    {
        /// <summary>
        /// Instantiates the DataContainer and parses all the user data files
        /// </summary>
        public static DataContainer Create(string playerFileFolder, string tribeFileFolder)
        {
            IEnumerable<string> playerFiles = null;
            IEnumerable<string> tribeFiles = null;

            if (Directory.Exists(playerFileFolder))
            {
                playerFiles = Directory.GetFiles(playerFileFolder).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(DataFileDetails.PlayerFilePrefix)
                    && Path.GetFileNameWithoutExtension(f).EndsWith(DataFileDetails.PlayerFileSuffix)
                    && Path.GetExtension(f).Equals(DataFileDetails.PlayerFileExtension));
            }
            if (Directory.Exists(tribeFileFolder))
            {
                tribeFiles = Directory.GetFiles(tribeFileFolder).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(DataFileDetails.TribeFilePrefix)
                    && Path.GetFileNameWithoutExtension(f).EndsWith(DataFileDetails.TribeFileSuffix)
                    && Path.GetExtension(f).Equals(DataFileDetails.TribeFileExtension));
            }

            var container = new DataContainer();

            if (playerFiles != null)
            {
                foreach (var file in playerFiles)
                {
                    container.Players.Add(Parser.ParsePlayer(file));
                }
            }

            if (tribeFiles != null)
            {
                foreach (var file in tribeFiles)
                {
                    container.Tribes.Add(Parser.ParseTribe(file));
                }
            }

            container.LinkPlayerTribe();

            return container;
        }

        /// <summary>
        /// Loads the profile data for all users from the steam service
        /// </summary>
        /// <param name="apiKey">The Steam API key</param>
        public DateTime LoadSteam(string apiKey, int steamUpdateInterval = 0)
        {
            // need to make multiple calls of 100 steam id's.
            var lastSteamUpdateUtc = DateTime.UtcNow;
            var startIndex = 0;
            var playerSteamIds = Players
                .Where(p => p.LastPlatformUpdateUtc.AddMinutes(steamUpdateInterval) < DateTime.UtcNow && p.NoUpdateCount < MAX_INVALID_COUNT)
                .Select(p => p.PlayerId)
                .ToArray();

            while (true)
            {
                // check if the start index has exceeded the Players list count.
                if (startIndex >= playerSteamIds.Count()) break;
                // get the number of steam ids to read.
                int steamIdsCount = Math.Min(MAX_STEAM_IDS, playerSteamIds.Count() - startIndex);
                // get a comma delimited list of the steam ids to process
                var builder = string.Join(",", playerSteamIds, startIndex, steamIdsCount);

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://api.steampowered.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = client.GetAsync(string.Format("ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}", apiKey, builder)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        using (var reader = new StreamReader(response.Content.ReadAsStreamAsync().Result))
                        {
                            LinkSteamProfiles(reader.ReadToEnd(), lastSteamUpdateUtc, playerSteamIds);
                        }
                    }
                    else
                    {
                        throw new System.Net.WebException("The Steam API request was unsuccessful. Are you using a valid key?");
                    }
                }

                startIndex += steamIdsCount;
            }

            SteamLoaded = true;
            return lastSteamUpdateUtc;
        }

        /// <summary>
        /// Fetches the player server status. Can only be done after fetching Steam player data.
        /// </summary>
        /// <param name="ipString">The IP of the server.</param>
        /// <param name="port">The port of the server.</param>
        public void LoadOnlinePlayers(string ipString, int port)
        {
            if (SteamLoaded)
            {
                LinkOnlinePlayers(ipString, port);
            }
            else
            {
                throw new Exception("The Steam user data should be loaded before the server status can be checked.");
            }
        }
    }
}
