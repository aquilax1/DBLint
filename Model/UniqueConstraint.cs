using System;
using System.Collections.Generic;
using System.Linq;
using DBLint.Model;

namespace DBLint.Model
{
    public class UniqueConstraint : TableID, IDisposable
    {
        internal IList<Column> _columns = new List<Column>();
        public IList<Column> Columns
        {
            get { return _columns; }
        }

        public string ConstraintName { get; internal set; }

        public UniqueConstraint(Table table)
            : base(table.DatabaseName, table.SchemaName, table.TableName)
        { }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _columns = _columns.ToList().AsReadOnly();
        }
    }
}