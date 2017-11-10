using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Model
{
    public class View : ViewID
    {
        public View(string databaseName, string schemaName, string viewName)
            : base(databaseName, schemaName, viewName)
        {

        }

        public Schema Schema { get; internal set; }
        public Database Database { get; internal set; }

        internal IList<ViewColumn> _columns = new List<ViewColumn>();
        public IList<ViewColumn> Columns { get { return _columns; } }

        public virtual void Dispose()
        {
            this._columns = (from c in _columns
                             orderby c.OrdinalPosition
                             select c).ToList().AsReadOnly();
        }
    }
}
