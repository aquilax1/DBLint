using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using IBM.Data.DB2;
using IBM.Data.DB2Types;

namespace DBLint.Data
{
    public class DB2DataRow : DataRow
    {
        private DB2DataReader _reader;

        public DB2DataRow(DbDataReader reader)
        {
            this._reader = reader as DB2DataReader;
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

        // TODO: There is no special handling rigth now
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
                            var sql = this._reader.GetDB2Decimal(columnIndex);
                            if (sql.IsNull)
                                return DBNull.Value;
                            return sql.Value;
                        }
                        catch (Exception tex)
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
