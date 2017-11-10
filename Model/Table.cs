using System;
using System.Collections.Generic;
using System.Linq;
using DBLint.Model;

namespace DBLint.Model
{
    public class Table : TableID, IDisposable
    {
        public Table(string databaseName, string schemaName, string tableName)
            : base(databaseName, schemaName, tableName)
        {
        }
        public Schema Schema { get; internal set; }
        public Database Database { get; internal set; }

        internal IList<Column> _columns = new List<Column>();
        public IList<Column> Columns { get { return _columns; } }

        internal IList<ForeignKey> _foreignKeys = new List<ForeignKey>();
        public IList<ForeignKey> ForeignKeys
        {
            get { return _foreignKeys; }
        }

        internal IList<Table> _referencedBy = new List<Table>();
        public IList<Table> ReferencedBy
        {
            get { return _referencedBy; }
        }

        internal IList<Table> _references = new List<Table>();
        public IList<Table> References { get { return _references; } }

        public PrimaryKey PrimaryKey { get; internal set; }

        internal IList<UniqueConstraint> _uniqueConstraints = new List<UniqueConstraint>();
        public IList<UniqueConstraint> UniqueConstraints
        {
            get { return _uniqueConstraints; }
        }

        internal IList<Index> _indices = new List<Index>();
        public IList<Index> Indices
        {
            get { return _indices; }
        }

        public int Cardinality { get; internal set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public virtual void Dispose()
        {
            this._columns = (from c in _columns
                             orderby c.OrdinalPosition
                             select c).ToList().AsReadOnly();
            this._foreignKeys = (from fk in _foreignKeys
                                 orderby fk.ForeignKeyName
                                 select fk).ToList().AsReadOnly();
            this._referencedBy = this._referencedBy.Distinct().ToList().AsReadOnly();
            this._references = this._references.Distinct().ToList().AsReadOnly();
            _uniqueConstraints = _uniqueConstraints.ToList().AsReadOnly();
            _indices = _indices.ToList().AsReadOnly();
        }
    }
}
