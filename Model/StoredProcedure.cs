using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Model
{
    public class StoredProcedure : StoredProcedureID, IDisposable
    {
        public StoredProcedure(string databaseName, string schemaName, string storedProcedureName)
            : base(databaseName, schemaName, storedProcedureName)
        {
        }

        public Database Database { get; internal set; }
        public Schema Schema { get; internal set; }

        internal IList<Parameter> _parameters = new List<Parameter>();
        public IList<Parameter> Parameters { get { return this._parameters; } }

        public void Dispose()
        {
            this._parameters = (from p in _parameters
                                orderby p.OrdinalPosition
                                select p).ToList().AsReadOnly();
        }
    }
}
