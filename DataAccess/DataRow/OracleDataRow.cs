using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace DBLint.Data
{
    public class OracleDataRow : DataRow
    {
        private OracleDataReader _reader;

        public OracleDataRow(DbDataReader reader)
        {
            this._reader = reader as OracleDataReader;
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
            catch (ArithmeticException)
            {
                try
                {
                    if (this._reader.GetFieldType(columnIndex) == typeof(decimal))
                    {
                        var sql = this._reader.GetOracleDecimal(columnIndex);
                        var newPrecision = 28;
                        return OracleDecimal.SetPrecision(sql, newPrecision).Value;
                    }
                    else
                        return DBNull.Value;
                }
                catch
                {
                    return DBNull.Value;
                }
            }
            catch (Exception)
            {
                return DBNull.Value;
            }
        }
    }
}
