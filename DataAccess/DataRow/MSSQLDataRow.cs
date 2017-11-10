using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;

namespace DBLint.Data
{
    public class MSSQLDataRow : DataRow
    {
        private SqlDataReader _reader;

        public MSSQLDataRow(DbDataReader reader)
        {
            this._reader = reader as SqlDataReader;
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
                if (this._reader.GetDataTypeName(columnIndex) == "decimal")
                {
                    try
                    {
                        return this._reader.GetDecimal(columnIndex);
                    }
                    catch
                    {
                        try
                        {
                            var sql = this._reader.GetSqlDecimal(columnIndex);
                            if (sql.IsNull)
                                return DBNull.Value;
                            var newPrecision = 28;
                            var newScale = newPrecision - Math.Min(newPrecision, (sql.Precision - sql.Scale));
                            return SqlDecimal.ConvertToPrecScale(sql, newPrecision, newScale).Value;
                        }
                        catch (SqlTruncateException tex)
                        {
                            return DBNull.Value;
                        }
                    }
                }
                return this._reader[columnIndex];
            }
            catch
            {
                return DBNull.Value;
            }
        }
    }
}
