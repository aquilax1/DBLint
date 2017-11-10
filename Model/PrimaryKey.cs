using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.Model
{
    public class PrimaryKey : TableID, IDisposable
    {
        internal IList<Column> _columns = new List<Column>();
        public IList<Column> Columns { get { return this._columns; } }
        public PrimaryKey(Table tbl, string primaryKeyName)
            : base(tbl.DatabaseName, tbl.SchemaName, tbl.TableName)
        {
            Table = tbl;
            this.PrimaryKeyName = primaryKeyName;
        }
        public bool IsMulticolumn { get { return this._columns.Count > 1; } }
        public string PrimaryKeyName { get; internal set; }
        public Table Table { get; internal set; }
        public Schema Schema { get; internal set; }
        public Database Database { get; internal set; }
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
