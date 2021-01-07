using ConanData.DataAccess;
using ConanData.Database;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ConanData
{
    /// <summary>
    /// The container for the Game Server data.
    /// </summary>
    public partial class DataContainer
    {
        /// <summary>
        /// Instantiates the DataContainer and parses all the tables
        /// </summary>
        /// <returns>The async task context containing the resulting container.</returns>
        public static async Task<DataContainer> CreateAsync(string dataFile)
        {
            if (string.IsNullOrWhiteSpace(dataFile) || !File.Exists(dataFile))
                return new DataContainer();

            var connectionString = $"Data Source={dataFile};Version=3;Read Only=True;";
            var connection = Connection.CreateConnection(connectionString, true);

            var accountDataAccess = new AccountDataAccess(connection);
            var accountData = accountDataAccess.FetchDataSetAll();

            var charactersDataAccess = new CharactersDataAccess(connection);
            var charactersData = charactersDataAccess.FetchDataSetAll();

            var guildsDataAccess = new GuildsDataAccess(connection);
            var guildsData = guildsDataAccess.FetchDataSetAll();

            connection.DeregisterConnection();

            var container = new DataContainer();

            foreach (var character in charactersData.Characters)
            {
                var account = accountData.Account.FirstOrDefault(a => a.AccountId == character.AccountId);
                container.Players.Add(await Parser.ParsePlayerAsync(account, character));
            }

            foreach (var guild in guildsData.Guilds)
            {
                container.Guilds.Add(await Parser.ParseGuildAsync(guild));
            }

            container.LinkPlayerGuild();

            return container;
        }

        /// <summary>
        /// Instantiates the DataContainer and parses the characters table
        /// </summary>
        /// <returns>The async task context containing the resulting container.</returns>
        public static async Task<int> GetOnlinePlayerCountAsync(string dataFile)
        {
            if (string.IsNullOrWhiteSpace(dataFile) || !File.Exists(dataFile))
                return 0;

            var connectionString = $"Data Source={dataFile};Version=3;Read Only=True;";
            var connection = Connection.CreateConnection(connectionString, true);
            var result = 0;

            await Task.Run(() =>
            {
                var accountDataAccess = new AccountDataAccess(connection);
                var accountData = accountDataAccess.FetchDataSetOnlineOnly();

                result = accountData?.Account.Count ?? 0;
            });

            connection.DeregisterConnection();

            return result;
        }
    }
}
