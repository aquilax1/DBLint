using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using DBLint.DataAccess;
using DBLint.Model;

namespace DBLint.Model
{
    public class Database : DatabaseID, IDisposable
    {
        private String _friendlyName = null;
        private Factory factory = null;

        internal Database(string databaseName, Factory factory)
            : base(databaseName)
        {
            this.IgnoredTables = new List<Table>();
            this.factory = factory;
        }

        internal Database(string databaseName, Factory factory, string friendlyName)
            : this(databaseName, factory)
        {
            this._friendlyName = friendlyName;
        }

        public string FriendlyName
        {
            get
            {
                if (this._friendlyName == null)
                    return this.DatabaseName;
                return this._friendlyName;
            }
            set { this._friendlyName = value; }
        }

        internal DatabaseDictionary<TableID, Table> tableDictionary = new DatabaseDictionary<TableID, Table>(TableID.GetTableHashCode, TableID.TableEquals);
        internal DatabaseDictionary<ViewID, View> viewDictionary = new DatabaseDictionary<ViewID, View>(ViewID.GetViewHashCode, ViewID.ViewEquals);
        internal DatabaseDictionary<ColumnID, Column> columnDictionary = new DatabaseDictionary<ColumnID, Column>(ColumnID.GetColumnHashCode, ColumnID.ColumnEquals);
        internal DatabaseDictionary<SchemaID, Schema> schemaDictionary = new DatabaseDictionary<SchemaID, Schema>(SchemaID.GetSchemaHashCode, SchemaID.SchemaEquals);

        internal IList<Schema> _schemas = new List<Schema>();
        public IList<Schema> Schemas { get { return _schemas; } }

        public IEscaper Escaper { get; internal set; }

        public IList<Table> Tables { get; internal set; }
        public IList<View> Views { get; internal set; }
        public IList<Table> IgnoredTables = new List<Table>();
        public DBMSs DBMS { get; internal set; }
        public IList<Column> Columns { get; internal set; }
        public IList<ViewColumn> ViewColumns { get; internal set; }
        public IList<StoredProcedure> StoredProcedures { get; internal set; }
        public IList<Function> Functions { get; internal set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _schemas = (from s in _schemas
                        orderby s.SchemaName
                        select s).ToList().AsReadOnly();

            var tables = new List<Table>();
            var columns = new List<Column>();
            var storedProcedures = new List<StoredProcedure>();
            var functions = new List<Function>();
            var views = new List<View>();
            var viewColumns = new List<ViewColumn>();

            foreach (var schema in Schemas)
            {
                tables.AddRange(schema.Tables);
                columns.AddRange(schema.Columns);
                storedProcedures.AddRange(schema.StoredProcedures);
                functions.AddRange(schema.Functions);
                views.AddRange(schema.Views);
                viewColumns.AddRange(schema.ViewColumns);
            }

            Tables = tables.AsReadOnly();
            Columns = columns.AsReadOnly();
            StoredProcedures = storedProcedures.AsReadOnly();
            Functions = functions.AsReadOnly();
            Views = views.AsReadOnly();
            ViewColumns = viewColumns.AsReadOnly();
        }

        public Table GetTable(TableID tid)
        {
            if (!this.tableDictionary.ContainsKey(tid))
                return null;
            var table = this.tableDictionary[tid];
            return table;
        }

        public View GetView(ViewID vid)
        {
            if (!this.viewDictionary.ContainsKey(vid))
                return null;
            return this.viewDictionary[vid];
        }

        /// <summary>
        /// Executes an arbitrary SQL query against the underlying database connection
        /// </summary>
        /// <param name="SQLQuery">SQL Query</param>
        /// <returns>DataTable containing the result of the query</returns>
        public DataTable Query(String SQLQuery)
        {
            return this.factory.Query(SQLQuery);
        }
    }
}
