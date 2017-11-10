using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.DataAccess.DBObjects
{
    public class Table : TableID
    {
        public Table() { }

        public Table(string schemaName, string tableName)
            : base()
        {
            this.SchemaName = schemaName;
            this.TableName = tableName;
        }
    }
}
