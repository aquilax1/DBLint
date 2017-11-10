using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.DataAccess.DBObjects
{
    public class Schema : SchemaID
    {
        public Schema() { }

        public Schema(string schemaName)
            : base()
        {
            this.SchemaName = schemaName;
        }
    }
}
