using System;
using System.Collections.Generic;
using System.Linq;
using DBLint.Model;

namespace DBLint.Model
{
    public class Schema : SchemaID, IDisposable
    {
        public Schema(string databaseName, string schemaName)
            : base(databaseName, schemaName)
        { }

        public Database Database { get; internal set; }

        internal IList<Table> _tables = new List<Table>();
        internal IList<View> _views = new List<View>();
        internal IList<Column> _columns = new List<Column>();
        internal IList<ViewColumn> _viewColumns = new List<ViewColumn>();
        internal IList<Function> _functions = new List<Function>();
        internal IList<StoredProcedure> _storedProcedures = new List<StoredProcedure>();

        internal void Sort()
        {
            _tables = (from t in _tables
                       orderby t.TableName
                       select t).ToList().AsReadOnly();

            _views = (from v in _views
                      orderby v.ViewName
                      select v).ToList().AsReadOnly();

            _columns = (from c in _columns
                        orderby c.TableName, c.OrdinalPosition
                        select c).ToList().AsReadOnly();

            _viewColumns = (from c in _viewColumns
                            orderby c.ViewName, c.OrdinalPosition
                            select c).ToList().AsReadOnly();

            _functions = (from f in _functions
                          orderby f.FunctionName
                          select f).ToList().AsReadOnly();

            _storedProcedures = (from s in _storedProcedures
                                 orderby s.StoredProcedureName
                                 select s).ToList().AsReadOnly();
        }

        public IList<Column> Columns
        {
            get
            {
                return _columns;
            }
        }

        public IList<Table> Tables
        {
            get { return _tables; }
        }

        public IList<Function> Functions
        {
            get
            {
                return this._functions;
            }
        }

        public IList<StoredProcedure> StoredProcedures
        {
            get { return this._storedProcedures; }
        }

        public IList<View> Views
        {
            get { return this._views; }
        }

        public IList<ViewColumn> ViewColumns
        {
            get { return this._viewColumns; }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            foreach (var table in this.Tables)
            {
                foreach (var column in table.Columns)
                    _columns.Add(column);
            }
            foreach (var view in this.Views)
            {
                foreach (var column in view.Columns)
                    _viewColumns.Add(column);
            }
            Sort();
        }
    }
}
