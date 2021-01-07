using ConanData.DataAccess.Base;
using ConanData.Database;
using ConanData.Datasets;
using System;
using System.Data;
using System.Text;

namespace ConanData.DataAccess
{
	/// <summary>
	/// Inherited data access class to perform customized Fetches, Deletes, Inserts 
	/// and Updates on table account
	/// </summary>
	internal class AccountDataAccess : AccountDataAccessBase
	{
        public AccountDataAccess(Connection connection)
            : base(connection)
        {
        }

        public AccountDataSet FetchDataSetOnlineOnly()
        {
            Connection connection = Connection;

            StringBuilder sql = new StringBuilder();
            sql.Append(GetSelect());
            sql.AppendFormat("WHERE [{0}].[{1}] <> 0 ", TABLE__NAME, COLUMN__ONLINE);

            AccountDataSet dataSet = new AccountDataSet();
            connection.FillDataSet(sql.ToString(), null, CommandType.Text, 300, dataSet, new String[] { TABLE__NAME });
            return dataSet;
        }
    }
}
