using ConanData.DataAccess.Base;
using ConanData.Database;

namespace ConanData.DataAccess
{
    /// <summary>
    /// Inherited data access class to perform customized Fetches, Deletes, Inserts 
    /// and Updates on table character_stats
    /// </summary>
    internal class CharacterStatsDataAccess : CharacterStatsDataAccessBase
	{
        public CharacterStatsDataAccess(Connection connection)
            : base(connection)
        {
        }
    }
}
