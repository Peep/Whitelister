using System;
using System.Text;

namespace BMRFME.Whitelist.Sql
{
    public struct ConnectionAuthInfo
    {
        public string Database;
        public string Hostname;
        public string Password;
        public int Port;
        public string User;

        public string ConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Hostname))
                    throw new ArgumentException("Hostname");

                if (Port == 0)
                    Port = 3306;

                var builder = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(Hostname))
                    builder.AppendFormat("server={0};", Hostname);
                builder.AppendFormat("port={0};", Port);
                if (!string.IsNullOrWhiteSpace(Hostname))
                    builder.AppendFormat("database={0};", Database);
                if (!string.IsNullOrWhiteSpace(Hostname))
                    builder.AppendFormat("uid={0};", User);
                if (!string.IsNullOrWhiteSpace(Hostname))
                    builder.AppendFormat("pwd={0};", Password);


                return builder.ToString();
            }
        }
    }
}
