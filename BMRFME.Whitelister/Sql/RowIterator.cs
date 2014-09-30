using System;
using System.Collections;
using System.Collections.Generic;

namespace BMRFME.Whitelist.Sql
{
    /// <summary>
    /// Handles iteration through the rows a result set
    /// </summary>
    public class RowIterater
        : IEnumerator<RowIterater>
    {
        private readonly ResultSet _owningSet;
        private int _currentRow;

        internal RowIterater(ResultSet owningSet)
        {
            _owningSet = owningSet;
            _currentRow = -1;
        }

        public int CurrentRow
        {
            get { return _currentRow; }
            set { Seek(value); }
        }

        /// <summary>
        /// Returns the value of the current row in column columnName
        /// </summary>
        /// <param name="columnName">Name of the column to return data from</param>
        /// <returns></returns>
        public object this[string columnName]
        {
            get { return _owningSet._rows[_currentRow][_owningSet._columnIndexs[columnName].Item1]; }
        }

        /// <summary>
        /// Returns the value of the current row in the column in columnIndex position
        /// </summary>
        /// <param name="columnIndex">The 0 based index of the column to return data from</param>
        /// <returns></returns>
        public object this[int columnIndex]
        {
            get { return _owningSet._rows[_currentRow][columnIndex]; }
        }

        #region IEnumerator<RowIterater> Members

        public bool MoveNext()
        {
            _currentRow++;
            return _currentRow < _owningSet.NumRows;
        }

        public void Reset()
        {
            _currentRow = -1;
        }

        public RowIterater Current
        {
            get { return this; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {
        }

        #endregion

        public void Seek(int rowNumber)
        {
            if (rowNumber < _owningSet.NumRows)
                _currentRow = rowNumber;
        }

        /// <summary>
        /// Returns the current stored value casted as type T
        /// </summary>
        /// <typeparam name="T">Type to cast as. Must be exact</typeparam>
        /// <param name="columnName">Name of the column to return data from</param>
        /// <returns>Value of columnName casted to T</returns>
        public T V<T>(string columnName)
        {
            if (typeof(T)
                != _owningSet._columnIndexs[columnName].Item2)
                throw new InvalidCastException(string.Format("Invalid Cast From {0} to {1}", typeof(T).Name,
                                                             _owningSet._columnIndexs[columnName].Item2.Name));

            return (T)this[columnName];
        }
    }
}
