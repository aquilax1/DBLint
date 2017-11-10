using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace DBLint.Data
{
    public class MySQLDataRow : DataRow
    {
        private MySqlDataReader _reader;

        public MySQLDataRow(DbDataReader reader)
        {
            this._reader = reader as MySqlDataReader;
            for (int i = 0; i < reader.FieldCount; i++)
                this.values[this._reader.GetName(i)] = this.GetData(i);
        }

        public override object this[string columnName]
        {
            get
            {
                return this.values[columnName];
            }
        }

        private object GetData(int columnIndex)
        {
            try
            {
                return this._reader[columnIndex];
            }
            catch
            {
                return DBNull.Value;
            }
        }
    }
}
