using System;
using System.Collections.Generic;
using System.Linq;
using DBLint.Model;

namespace DBLint.Model
{
    public class Index : IndexID, IDisposable
    {
        public Index(Table table, String IndexName)
            : base(table.DatabaseName, table.SchemaName, table.TableName, IndexName)
        {
            Table = table;
        }

        public Table Table { get; internal set; }

        internal IList<Column> _columns = new List<Column>();
        public IList<Column> Columns
        {
            get { return _columns; }
        }

        public bool IsMultiColumn { get { return this._columns.Count > 1; } }

        public Column Column
        {
            get
            {
                if (IsMultiColumn)
                    throw new FieldAccessException("Cannot access column in multicolumn index");
                return _columns.First();
            }
        }

        public bool IsUnique { get; internal set; }

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