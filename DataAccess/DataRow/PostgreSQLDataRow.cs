using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;

namespace DBLint.Data
{
    public class PostgreSQLDataRow : DataRow
    {
        private NpgsqlDataReader _reader;

        public PostgreSQLDataRow(DbDataReader reader)
        {
            this._reader = reader as NpgsqlDataReader;
            for (int i = 0; i < reader.FieldCount; i++)
                this.values[this._reader.GetName(i)] = this.GetData(i);
        }

        public override object this[string columnName]
        {
            get { return this.values[columnName]; }
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
