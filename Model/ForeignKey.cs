using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.DataTypes;

namespace DBLint.Model
{
    public class ForeignKeyPair
    {
        public Column FKColumn { get; internal set; }
        public Column PKColumn { get; internal set; }
        public int OrdinalNumber { get; internal set; }

        public ForeignKeyPair(Column fkColumn, Column pkColumn, int ordinal)
        {
            this.FKColumn = fkColumn;
            this.PKColumn = pkColumn;
            this.OrdinalNumber = ordinal;
        }
    }

    public class ForeignKey : SchemaID, IDisposable
    {
        public ForeignKey(string databaseName, string schemaName, string foreignKeyName)
            : base(databaseName, schemaName)
        {
            this.ForeignKeyName = foreignKeyName;
        }

        public string ForeignKeyName { get; internal set; }
        public Database Database { get; internal set; }
        public Schema Schema { get; internal set; }
        public Table PKTable { get; internal set; }
        public Table FKTable { get; internal set; }
        public UpdateRule UpdateRule { get; internal set; }
        public DeleteRule DeleteRule { get; internal set; }
        public Deferrability Deferrability { get; internal set; }
        public InitialMode InitialMode { get; internal set; }

        public bool IsSingleColumn { get { return (this.ColumnPairs.Count == 1); } }

        public Column FKColumn
        {
            get
            {
                if (!IsSingleColumn)
                    throw new FieldAccessException("Cannot access FKColumn in multicolumn foreign key");
                return this.ColumnPairs.First().FKColumn;
            }
        }
        public Column PKColumn
        {
            get
            {
                if (!IsSingleColumn)
                    throw new FieldAccessException("Cannot access FKColumn in multicolumn foreign key");
                return this.ColumnPairs.First().PKColumn;
            }
        }

        internal IList<ForeignKeyPair> _columnPairs = new List<ForeignKeyPair>();

        public IList<ForeignKeyPair> ColumnPairs
        {
            get { return _columnPairs; }

        }

        

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _columnPairs = (from cp in _columnPairs
                            orderby cp.OrdinalNumber
                            select cp).ToList().AsReadOnly();
        }
    }
}
