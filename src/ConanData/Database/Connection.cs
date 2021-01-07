using System;
using System.Data;
using System.Data.Common;

namespace ConanData.Database
{
    public sealed class Connection : IDisposable
    {
        #region Structures, Constants and Enums
        public const int DEFAULT_COMMAND_TIMEOUT = 300;
        #endregion

        #region Constructors, Destructors and Dispose
        private Connection(string connectionString)
        {
            ConnectionString = connectionString;
        }
        ~Connection()
        {
            Dispose();
        }
        public void Dispose()
        {
            DeregisterConnection();
        }
        #endregion

        #region Properties
        public string ConnectionString { get; set; } = string.Empty;
        public System.Data.SQLite.SQLiteConnection DBConnection { get; set; } = null;
        #endregion

        #region Methods

        #region Connection
        public static Connection CreateConnection(string connectionString, bool openConnection = false)
        {
            var connection = new Connection(connectionString);
            if (openConnection)
            {
                connection.RegisterConnection();
            }
            return connection;
        }
        public void DeregisterConnection()
        {
            if (DBConnection != null && DBConnection.State != ConnectionState.Closed)
            {
                DBConnection?.Close();
                DBConnection?.Dispose();
            }
            DBConnection = null;
        }
        private DbConnection GetConnection()
        {
            if (DBConnection == null || DBConnection.State == ConnectionState.Closed)
            {
                throw new InvalidOperationException("You must register a connection to the Database.");
            }

            return DBConnection;
        }
        public void RegisterConnection()
        {
            if (DBConnection == null || DBConnection.State == ConnectionState.Closed)
            {
                DBConnection = new System.Data.SQLite.SQLiteConnection(ConnectionString);
                DBConnection.Open();
            }
        }
        #endregion

        #region Command
        public DbCommand GetCommand()
        {
            DbConnection connection = GetConnection();
            DbCommand command = connection.CreateCommand();
            return command;
        }
        private static void SetCommandTimeout(DbCommand command, int commandTimeout)
        {
            if (command == null)
            {
                return;
            }
            commandTimeout = Math.Max(0, commandTimeout);

            try
            {
                command.CommandTimeout = commandTimeout;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region DataAdapter
        private DbDataAdapter GetNewDataAdapter()
        {
            return new System.Data.SQLite.SQLiteDataAdapter();
        }
        #endregion

        #region DataReader
        public DbDataReader ExecuteReader(string commandText, DbParameter[] parameters, CommandType commandType)
        {
            return ExecuteReader(commandText, parameters, commandType, DEFAULT_COMMAND_TIMEOUT, CommandBehavior.Default);
        }
        public DbDataReader ExecuteReader(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout)
        {
            return ExecuteReader(commandText, parameters, commandType, commandTimeout, CommandBehavior.Default);
        }
        public DbDataReader ExecuteReader(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, CommandBehavior commandBehavior)
        {
            DbCommand command = null;
            DbDataReader reader = null;

            try
            {
                command = GetCommand();
                command.CommandText = commandText;
                command.CommandType = commandType;
                SetCommandTimeout(command, commandTimeout >= 0 ? commandTimeout : DEFAULT_COMMAND_TIMEOUT);

                if (parameters != null)
                {
                    ValidateNullParameters(parameters);
                    foreach (DbParameter parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }

                reader = command.ExecuteReader(commandBehavior);
            }
            catch
            {
                if (reader != null && reader.IsClosed == false)
                {
                    reader.Close();
                }
                if (command != null)
                {
                    command.Dispose();
                }
                throw;
            }

            return reader;
        }
        #endregion

        #region DataSet
        public DataSet ExecuteDataSet(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout)
        {
            DataSet dataSet = new DataSet();
            string[] tableNames = null;
            FillDataSet(commandText, parameters, commandType, commandTimeout, dataSet, tableNames);
            return dataSet;
        }
        public DataSet ExecuteDataSet(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, string[] tableNames)
        {
            DataSet dataSet = new DataSet();
            FillDataSet(commandText, parameters, commandType, commandTimeout, dataSet, tableNames);
            return dataSet;
        }
        public void ExecuteDataSet(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, DataSet dataSet)
        {
            string[] tableNames = null;
            FillDataSet(commandText, parameters, commandType, commandTimeout, dataSet, tableNames);
        }
        public void ExecuteDataSet(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, DataSet dataSet, string[] tableNames)
        {
            FillDataSet(commandText, parameters, commandType, commandTimeout, dataSet, tableNames);
        }
        public void FillDataSet(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, DataSet dataSet, string[] tableNames)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentNullException("commandText", "The parameter 'commandText' cannot be null");
            }

            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet", "The parameter 'dataSet' cannot be null");
            }

            // Initialise command object
            DbCommand command = GetCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;
            SetCommandTimeout(command, commandTimeout >= 0 ? commandTimeout : DEFAULT_COMMAND_TIMEOUT);

            if (parameters != null)
            {
                ValidateNullParameters(parameters);

                // Initialise command object parameters
                foreach (DbParameter parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            FillDataSet(command, dataSet, tableNames);

            command.Dispose();
        }
        public void FillDataSet(DbCommand selectCommand, DataSet dataSet, string[] tableNames)
        {
            if (selectCommand == null)
            {
                throw new ArgumentNullException("selectCommand", "The parameter 'selectCommand' cannot be null");
            }

            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet", "The parameter 'dataSet' cannot be null");
            }

            // Create the DataAdapter
            DbDataAdapter dataAdapter = GetNewDataAdapter();

            // Add the table mappings specified by the user
            if (tableNames != null && tableNames.Length > 0)
            {
                for (int i = 0; i < tableNames.Length; i++)
                {
                    string tableName = (i == 0) ? "Table" : String.Format("Table{0}", i);
                    if (string.IsNullOrWhiteSpace(tableNames[i]))
                    {
                        throw new ArgumentException("The parameter 'tableNames' must contain a list of tables - a value was provided as null or empty string.", "tableNames");
                    }
                    dataAdapter.TableMappings.Add(tableName, tableNames[i]);
                }
            }

            dataAdapter.SelectCommand = selectCommand;
            if (dataAdapter.SelectCommand.Connection == null)
            {
                dataAdapter.SelectCommand.Connection = GetConnection();
            }
            dataAdapter.Fill(dataSet);
        }
        #endregion

        #region DataTable
        public DataTable ExecuteDataTable(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout)
        {
            DataTable dataTable = new DataTable();
            FillDataTable(commandText, parameters, commandType, commandTimeout, dataTable, null);
            return dataTable;
        }
        public DataTable ExecuteDataTable(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, string tableName)
        {
            DataTable dataTable = new DataTable();
            FillDataTable(commandText, parameters, commandType, commandTimeout, dataTable, tableName);
            return dataTable;
        }
        public void ExecuteDataTable(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, DataTable dataTable)
        {
            FillDataTable(commandText, parameters, commandType, commandTimeout, dataTable, null);
        }
        public void ExecuteDataTable(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, DataTable dataTable, string tableName)
        {
            FillDataTable(commandText, parameters, commandType, commandTimeout, dataTable, tableName);
        }
        public void FillDataTable(string commandText, DbParameter[] parameters, CommandType commandType, int commandTimeout, DataTable dataTable, string tableName)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentNullException("commandText", "The parameter 'commandText' cannot be null");
            }

            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable", "The parameter 'dataTable' cannot be null");
            }

            // Initialise command object
            DbCommand command = GetCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;
            SetCommandTimeout(command, commandTimeout >= 0 ? commandTimeout : DEFAULT_COMMAND_TIMEOUT);

            if (parameters != null)
            {
                ValidateNullParameters(parameters);

                // Initialise command object parameters
                foreach (DbParameter parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            FillDataTable(command, dataTable, tableName);

            command.Dispose();
        }
        public void FillDataTable(DbCommand selectCommand, DataTable dataTable, string tableName)
        {
            if (selectCommand == null)
            {
                throw new ArgumentNullException("selectCommand", "The parameter 'selectCommand' cannot be null");
            }

            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable", "The parameter 'dataTable' cannot be null");
            }

            // Create the DataAdapter
            DbDataAdapter dataAdapter = GetNewDataAdapter();

            // Add the table mappings specified by the user
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                dataAdapter.TableMappings.Add("Table", tableName);
            }

            dataAdapter.SelectCommand = selectCommand;
            if (dataAdapter.SelectCommand.Connection == null)
            {
                dataAdapter.SelectCommand.Connection = GetConnection();
            }
            dataAdapter.Fill(dataTable);
        }
        #endregion

        #region Parameter
        public DbParameter CreateParameter(string parameterName, ParameterDirection direction)
        {
            DbParameter parameter = new System.Data.SQLite.SQLiteParameter();
            parameter.ParameterName = parameterName;
            parameter.Direction = direction;
            return parameter;
        }
        public DbParameter CreateParameter(string parameterName, ParameterDirection direction, object value)
        {
            DbParameter parameter = CreateParameter(parameterName, direction);
            parameter.Value = value;
            return parameter;
        }
        public DbParameter CreateParameter(string parameterName, ParameterDirection direction, string sourceColumn, DataRowVersion sourceVersion)
        {
            DbParameter parameter = CreateParameter(parameterName, direction);
            parameter.SourceColumn = sourceColumn;
            parameter.SourceVersion = sourceVersion;
            return parameter;
        }
        public static void ValidateNullParameters(DbParameter[] parameters)
        {
            foreach (DbParameter param in parameters)
            {
                if (param.Value == null)
                {
                    param.Value = DBNull.Value;
                }
            }
        }
        #endregion

        #endregion
    }
}
