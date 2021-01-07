using ConanData.Datasets;
using System.Threading.Tasks;

namespace ConanData
{
    internal partial class Parser
    {
        public static GuildData ParseGuild(GuildsDataSet.GuildsRow guildRow)
        {
            return new GuildData()
            {
                GuildId = guildRow.GuildId,
                GuildName = guildRow.Name,
                OwnerId = guildRow.OwnerId,
            };
        }

        public static Task<GuildData> ParseGuildAsync(GuildsDataSet.GuildsRow guildRow)
        {
            return Task.Run(() => ParseGuild(guildRow));
        }
    }
}
