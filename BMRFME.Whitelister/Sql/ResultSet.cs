using System;
using System.Collections;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace BMRFME.Whitelist.Sql
{
    public class ResultSet
        : IEnumerable<RowIterater>
    {
        internal Dictionary<string, Tuple<int, Type>> _columnIndexs;
        internal RowIterater _iter;
        internal int _naffect;
        internal int _ncol;
        internal int _nrows;
        internal List<ResultRow> _rows;

        public ResultSet()
        {
            _iter = new RowIterater(this);
            _columnIndexs = new Dictionary<string, Tuple<int, Type>>();
            _rows = new List<ResultRow>();
        }

        public int NumRows
        {
            get { return _nrows; }
        }

        public int NumCols
        {
            get { return _ncol; }
        }

        public int NumAffect
        {
            get { return _naffect; }
        }

        public RowIterater this[int index]
        {
            get
            {
                _iter.Seek(index);
                return _iter;
            }
        }

        #region IEnumerable<RowIterater> Members

        public IEnumerator<RowIterater> GetEnumerator()
        {
            return new RowIterater(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public void Fill(MySqlDataReader reader)
        {
            _ncol = reader.FieldCount;
            _nrows = 0;
            if (reader.HasRows)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                    _columnIndexs.Add(reader.GetName(i), new Tuple<int, Type>(i, reader.GetFieldType(i)));

                while (reader.Read())
                {
                    _rows.Add(ResultRow.ReadFrom(reader));
                }
                _nrows = _rows.Count;
            }
            _naffect = reader.RecordsAffected;
        }
    }
}
