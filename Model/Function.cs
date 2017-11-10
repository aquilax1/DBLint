using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Model
{
    public class Function : FunctionID, IDisposable
    {
        public Function(string databaseName, string schemaName, string functionName)
            : base(databaseName, schemaName, functionName)
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
