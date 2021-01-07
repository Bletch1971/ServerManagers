using ConanData.Datasets;
using System.Threading.Tasks;

namespace ConanData
{
    internal partial class Parser
    {
        public static PlayerData ParsePlayer(AccountDataSet.AccountRow accountRow, CharactersDataSet.CharactersRow characterRow)
        {
            if (characterRow == null)
                return new PlayerData();

            return new PlayerData()
            {
                PlayerId = accountRow?.User ?? string.Empty,
                PlayerName = string.Empty,
                CharacterId = characterRow.CharacterId,
                CharacterName = characterRow.CharacterName,
                GuildId = characterRow.IsGuildIdNull() ? (long?)null : characterRow.GuildId,
                Level = characterRow.IsLevelNull() ? (short)1 : characterRow.Level,
                LastOnline = characterRow.IsLastTimeOnlineNull() ? (int?)null : characterRow.LastTimeOnline,
                Online = accountRow?.Online ?? false,
            };
        }

        public static Task<PlayerData> ParsePlayerAsync(AccountDataSet.AccountRow accountRow, CharactersDataSet.CharactersRow characterRow)
        {
            return Task.Run(() => ParsePlayer(accountRow, characterRow));
        }
    }
}
