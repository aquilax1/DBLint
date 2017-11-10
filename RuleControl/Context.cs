using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    public enum ContextLocation { Database, Schema, Table, Column };

    public abstract class IssueContext
    {
        public ContextLocation Location { get; set; }
        public virtual IEnumerable<TableID> GetTables()
        {
            return new List<TableID> { };
        }

        public bool IsContext(Table table)
        {
            return this.GetTables().Contains(table);
        }

        public static IssueContext Create(DatabaseID context)
        {
            if (context is ColumnID)
                return new ColumnContext((ColumnID)context);
            else if (context is TableID)
                return new TableContext((TableID)context);
            else if (context is SchemaID)
                return new SchemaContext((SchemaID)context);
            if (context is DatabaseID)
                return new DatabaseContext((DatabaseID)context);
            else
                throw new Exception(String.Format("{0} cannot be used as an issue context", context.ToString()));
        }

        public static IssueContext Create(IEnumerable<DatabaseID> context)
        {
            if (context is IEnumerable<ColumnID>)
            {
                //If columns are from different tables, its a schema context, otherwise its a table context
                var columns = (IEnumerable<ColumnID>)context;
                var tid = (TableID)columns.First();
                return new TableContext(tid, columns);
                /*var tableCount = columns.GroupBy(c => c.TableName).Count();
                if (tableCount == 1)
                {
                    var tid = (TableID)columns.First();
                    return new TableContext(tid, columns);
                }
                else 
                { 
                    var sid = (SchemaID)columns.First();
                    return new SchemaContext(sid, (IEnumerable<TableID>)columns);
                }*/
            }
            else if (context is IEnumerable<TableID>)
            {
                var c = (IEnumerable<TableID>)context;
                var sid = (SchemaID)c.First();
                return new SchemaContext(sid, c);
            }
            else if (context is IEnumerable<SchemaID>)
            {
                var c = (IEnumerable<SchemaID>)context;
                var did = (DatabaseID)c.First();
                return new DatabaseContext(did, c);
            }
            else
            {
                throw new Exception(String.Format("{0} cannot be used as an issue context", context.ToString()));
            }
        }
    }

    public class ColumnContext : IssueContext
    {
        public ColumnID Column { get; private set; }
        public ColumnContext(ColumnID column)
        {
            this.Location = ContextLocation.Column;
            this.Column = column;
        }

        public override IEnumerable<TableID> GetTables()
        {
            return new List<TableID> { new TableID(Column) };
        }
    }

    public class DatabaseContext : IssueContext
    {
        public DatabaseID Database { get; private set; }
        public IEnumerable<SchemaID> Schemas { get; private set; }
        public DatabaseContext(DatabaseID db)
        {
            this.Location = ContextLocation.Database;
            this.Database = db;
        }
        public DatabaseContext(DatabaseID db, IEnumerable<SchemaID> schemas) : this(db)
        {
            this.Schemas = schemas.Select(s => new SchemaID(s));
        }
    }

    public class SchemaContext : IssueContext
    {
        public SchemaID Schema { get; private set; }
        public IEnumerable<TableID> Tables { get; private set; }
        public SchemaContext(SchemaID schema)
        {
            this.Location = ContextLocation.Schema;
            this.Schema = schema;
        }
        public SchemaContext(SchemaID schema, IEnumerable<TableID> tables) : this(schema)
        {
            this.Tables = tables.Select(t => new TableID(t));
        }

        public override IEnumerable<TableID> GetTables()
        {
            if (this.Tables != null)
                return this.Tables;
            return new List<TableID>();
        }
    }
    
    public class TableContext : IssueContext
    {
        public TableID Table { get; private set; }
        public IEnumerable<ColumnID> Columns { get; private set; }
        private List<TableID> tables = new List<TableID>();
        public TableContext(TableID table)
        {
            this.Location = ContextLocation.Table;
            this.Table = table;
        }
        public TableContext(TableID table, IEnumerable<ColumnID> columns) : this(table)
        {
            this.Columns = columns;
            foreach(var g in columns.GroupBy(c => c.TableName))
            {
                this.tables.Add(g.First());
            }
        }

        public override IEnumerable<TableID> GetTables()
        {
            if (this.tables.Count > 0)
                return this.tables;
            else
                return new List<TableID>() { this.Table };
        }
    }
}
