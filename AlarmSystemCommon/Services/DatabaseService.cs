using System;
using System.Data;
using AlarmSystem.Common.Logging;
using AlarmSystem.Common.Logging.Material;
using MySql.Data.MySqlClient;

namespace AlarmSystem.Common.Services
{
    /// <summary>
    /// Handles a MySQL connection to be used by all the plugins.
    /// </summary>
    public class DatabaseService
    {
        private MySqlConnection _connection;

        private string _host;
        private string _user;
        private string _password;
        private string _dbName;

        /// <summary>
        /// Gets a value indicating whether this <see cref="DatabaseService"/> is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public bool Connected
        {
            get { return (_connection != null) && (_connection.State == ConnectionState.Open || _connection.State == ConnectionState.Fetching || _connection.State == ConnectionState.Executing); }
        }

        /// <summary>
        /// Gets the MySQL-Connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public MySqlConnection Connection
        {
            get { return _connection; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseService"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="database">The database name.</param>
        public DatabaseService(string host, string user, string password, string database)
        {
            _host = host;
            _user = user;
            _password = password;
            _dbName = database;
            Log.Add(LogLevel.Debug, "MySQL", String.Format("Initialising with User {0} on Host {1} and database {2}", user, host, database));
        }

        /// <summary>
        /// Opens the connection with the given parameters.
        /// </summary>
        /// <returns>true if opening was successful; false otherwise.</returns>
        public bool Open()
        {
            try
            {
                Log.Add(LogLevel.Info, "MySQL", "Opening connection to database...");
                string dbConnStr =String.Format(
                  "Server={0};" +
                  "Database={1};" +
                  "User ID={2};" +
                  "Password={3};" +
                  "Pooling=false", _host, _dbName, _user, _password);

                _connection = new MySqlConnection(dbConnStr);

                _connection.Open();

                return true;
            }
            catch (Exception ex)
            {
                Log.AddException("MySQL", ex);
                return false;
            }
        }

        /// <summary>
        /// Closes the database connections.
        /// </summary>
        public void Close()
        {
            if (Connected)
            {
                Log.Add(LogLevel.Info, "MySQL", "Closing connection to database.");
                Connection.Close();
            }
        }

        /// <summary>
        /// Tries the create a MySQLDbCommand. If the connection is not open for some reason, it tries to open it and then creates the command.
        /// If opening fails, a error log is thrown and null is returned.
        /// </summary>
        /// <returns>A valid MySqlCommand or null.</returns>
        public MySqlCommand TryCreateCommand()
        {
            if (Connected)
            {
                try
                {
                    return Connection.CreateCommand();
                }
                catch (Exception ex)
                {
                    Log.AddException("MySQL", ex);
                    return null;
                }
            }
            else
            {
                try
                {
                    Open();
                    return Connection.CreateCommand();
                }
                catch (Exception ex)
                {
                    Log.AddException("MySQL", ex);
                    return null;
                }
            }
        }
    }
}
