using MySql.Data.MySqlClient;

namespace BMRFME.Whitelist.Sql
{
    public struct ResultRow
    {
        private object[] _values;

        public object this[int index]
        {
            get { return _values[index]; }
        }

        public static ResultRow ReadFrom(MySqlDataReader reader)
        {
            var row = new ResultRow
            {
                _values = new object[reader.FieldCount]
            };

            reader.GetValues(row._values);

            return row;
        }
    }
}
