using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;

namespace DBLint.Data
{
    public class FirebirdDataRow : DataRow
    {
        private FbDataReader _reader;

        public FirebirdDataRow(DbDataReader reader)
        {
            this._reader = reader as FbDataReader;
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
