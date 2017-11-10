using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.DataAccess.DBObjects
{
    public class TableCardinality
    {
        public string SchemaName { get; internal set; }
        public string TableName { get; internal set; }
        public string Cardinality { get; internal set; }
    }
}
