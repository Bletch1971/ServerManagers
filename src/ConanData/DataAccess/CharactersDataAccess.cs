using ConanData.DataAccess.Base;
using ConanData.Database;

namespace ConanData.DataAccess
{
    /// <summary>
    /// Inherited data access class to perform customized Fetches, Deletes, Inserts 
    /// and Updates on table characters
    /// </summary>
    internal class CharactersDataAccess : CharactersDataAccessBase
	{
        public CharactersDataAccess(Connection connection)
            : base(connection)
        {
        }
    }
}
