using ConanData.DataAccess.Base;
using ConanData.Database;

namespace ConanData.DataAccess
{
    /// <summary>
    /// Inherited data access class to perform customized Fetches, Deletes, Inserts 
    /// and Updates on table guilds
    /// </summary>
    internal class GuildsDataAccess : GuildsDataAccessBase
	{
        public GuildsDataAccess(Connection connection)
            : base(connection)
        {
        }
    }
}
