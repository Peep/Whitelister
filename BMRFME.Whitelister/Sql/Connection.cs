using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace BMRFME.Whitelist.Sql
{
    //Wrapper class.
    public class Connection
        : IDisposable
    {
        private readonly ConnectionAuthInfo _connectionAuthInfo;
        private MySqlConnection _connection;

        public Connection(string host, int port = 3306, string database = "", string user = "", string pass = "",
                          bool connect = true)
            : this(
                new ConnectionAuthInfo
                {
                    Database = database,
                    Hostname = host,
                    Port = port,
                    User = user,
                    Password = pass,
                }, connect
                )
        {
        }

        public Connection(ConnectionAuthInfo info, bool connect = true)
        {
            _connectionAuthInfo = info;

            _connection = new MySqlConnection(_connectionAuthInfo.ConnectionString);

            if (connect)
                Connect();
        }

        public MySqlConnection InternalConnection
        {
            get { return _connection; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            _connection.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public void Connect()
        {
            _connection.Open();
        }

        public void Disconnect()
        {
            _connection.Close();
        }

        public ResultSet Query(string query)
        {
            MySqlCommand cmd = _connection.CreateCommand();
            cmd.CommandText = query;
            return Query(cmd);
        }

        public ResultSet Query(MySqlCommand query)
        {
            query.Connection = this;
            MySqlDataReader reader = query.ExecuteReader(CommandBehavior.KeyInfo);
            var set = new ResultSet();
            set.Fill(reader);
            reader.Close();
            return set;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                _connection.Close();
            }
            _connection = null;
        }

        public static implicit operator MySqlConnection(Connection c)
        {
            return c._connection;
        }
    }
}
