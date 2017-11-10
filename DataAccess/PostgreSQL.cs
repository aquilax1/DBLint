using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using DBLint.DataAccess.DBObjects;
using DBLint.DataAccess.Utils;
using DBLint.DataTypes;
using Npgsql;

namespace DBLint.DataAccess
{
    public class PostgreSQL : Factory
    {
        public PostgreSQL(Connection connectionInfo, int timeout)
        {
            var connStr = String.Format("Server={0}; Database={1}; User Id={2}; Password={3}; Port={4}; MaxPoolSize={5}; Timeout={6}",
                connectionInfo.Host,
                connectionInfo.Database,
                connectionInfo.UserName,
                connectionInfo.Password,
                connectionInfo.Port,
                connectionInfo.MaxConnections,
                timeout);
            this.Setup(connStr, Npgsql.NpgsqlFactory.Instance);
        }

        public override DBMSs DBMS
        {
            get { return DBMSs.POSTGRESQL; }
        }

        public override string GetFormatedSelectStatement(string schemaName, string tableName, List<String> columnNames)
        {
            return String.Format("select \"{0}\" from \"{1}\".\"{2}\"", String.Join("\", \"", columnNames), schemaName, tableName);
        }

        public override Data.DataRow GetDataRow(System.Data.Common.DbDataReader reader)
        {
            return new Data.PostgreSQLDataRow(reader);
        }

        public override List<Schema> GetSchemas()
        {
            var stmt = String.Format("select schema_name from information_schema.schemata");
            var ds = this.ExecuteSelect(stmt);
            var result = new List<Schema>();
            foreach (DataRow dr in ds.Tables[0].Rows)
                result.Add(new Schema(dr["schema_name"].ToString()));
            return result;
        }

        public override List<Table> GetTables(string schemaName)
        {
            var stmt = String.Format(
                @"select
                    table_schema as schema_name,
                    table_name
                from information_schema.tables
                where table_schema = '{0}'
                    and table_type = 'BASE TABLE'",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var result = new List<Table>();
            foreach (DataRow dr in ds.Tables[0].Rows)
                result.Add(new Table(dr["schema_name"].ToString(), dr["table_name"].ToString()));
            return result;
        }

        private Dictionary<String, List<Column>> _columns = null;

        public override List<Column> GetColumns(string schemaName)
        {
            this._columns = new Dictionary<string, List<Column>>();
            var stmt = String.Format(
                @"select
                    col.table_schema as schema_name,
                    col.table_name,
                    col.column_name,
                    col.ordinal_position,
                    col.is_nullable,
                    col.udt_name as data_type,
                    col.character_maximum_length as character_max_length,
                    col.numeric_precision,
                    col.numeric_scale,
                    col.column_default as default_value
                from information_schema.columns as col
                where  table_schema = '{0}' 
                and exists (select 1 from information_schema.tables as tab 
				where table_type = 'BASE TABLE'
				and col.table_name = tab.table_name 
				and col.table_schema = tab.table_schema)",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var result = new List<Column>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var tmp = new Column(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["column_name"].ToString());
                tmp.OrdinalPosition = Convert.ToInt32(dr["ordinal_position"]);
                tmp.IsNullable = this.ParseBoolean(dr["is_nullable"].ToString());
                tmp.DataType = this.ParseDataType(dr["data_type"].ToString());
                if (dr["character_max_length"] != DBNull.Value)
                    tmp.CharacterMaxLength = Convert.ToInt64(dr["character_max_length"]);
                if (dr["numeric_precision"] != DBNull.Value)
                    tmp.NumericPrecision = Convert.ToInt32(dr["numeric_precision"]);
                if (dr["numeric_scale"] != DBNull.Value)
                    tmp.NumericScale = Convert.ToInt32(dr["numeric_scale"]);
                if (dr["default_value"] != DBNull.Value)
                {
                    bool isSequence;
                    tmp.DefaultValue = this.ParseDefaultValue(dr["default_value"].ToString(), out isSequence);
                    tmp.IsSequence = isSequence;
                }
                result.Add(tmp);

                if (!this._columns.ContainsKey(tmp.TableName))
                    this._columns.Add(tmp.TableName, new List<Column>());
                this._columns[tmp.TableName].Add(tmp);
            }
            return result;
        }

        public override List<Unique> GetUniqueConstraints(string schemaName)
        {
            var stmt = String.Format(
                @"select 
	                ky.table_schema as schema_name,
                    ky.table_name,
	                ky.constraint_name as key_name,
	                ky.ordinal_position,
	                ky.column_name
                from information_schema.key_column_usage as ky
                inner join information_schema.table_constraints as co
                on ky.constraint_name = co.constraint_name
                    and ky.table_schema = co.table_schema
                    and ky.table_name = co.table_name
                where co.constraint_schema = '{0}'
                    and co.constraint_type = 'UNIQUE'
                order by ky.constraint_name, ky.ordinal_position",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<Unique>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var uq = new Unique(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["key_name"].ToString());
                uq.AddColumn(Convert.ToInt32(dr["ordinal_position"]), dr["column_name"].ToString());
                Unique tmp;
                if (!cList.Add(uq, out tmp))
                    tmp.AddCoumns(uq.IndexOrderdColumnNames);
            }
            return cList.ToList();
        }

        public override List<PrimaryKey> GetPrimaryKeys(string schemaName)
        {
            var stmt = String.Format(
                @"select 
	                ky.table_schema as schema_name,
                    ky.table_name,
	                ky.constraint_name as key_name,
	                ky.ordinal_position,
	                ky.column_name
                from information_schema.key_column_usage as ky
                inner join information_schema.table_constraints as co
                on ky.constraint_name = co.constraint_name
                where co.constraint_schema = '{0}'
                    and co.constraint_type = 'PRIMARY KEY'
                order by ky.constraint_name, ky.ordinal_position",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<PrimaryKey>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var pk = new PrimaryKey(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["key_name"].ToString());
                pk.AddColumn(Convert.ToInt32(dr["ordinal_position"]), dr["column_name"].ToString());
                PrimaryKey tmp;
                if (!cList.Add(pk, out tmp))
                    tmp.AddCoumns(pk.IndexOrderdColumnNames);
            }
            return cList.ToList();
        }

        /// <summary>
        /// TODO: The extraction will ignore foreign-keys between schemas
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        public override List<ForeignKey> GetForeignKeys(string schemaName)
        {
            if (this._columns == null)
                this.GetColumns(schemaName);
            var stmt = String.Format(
                @"select
	                    conname as key_name,
	                    condeferrable as is_deferrable,
	                    condeferred as initially_deferred,
	                    confupdtype as update_rule,
	                    confdeltype as delete_rule,
	                    conkey as f_column_position,
	                    confkey as p_column_position,
	                    f.relname as table_name,
	                    p.relname as pri_table_name
                    from (select * from pg_catalog.pg_constraint where connamespace = (select oid from pg_namespace where nspname = '{0}') and contype = 'f') as fky
                    inner join pg_class as f on fky.conrelid = f.relfilenode
                    inner join pg_class as p on fky.confrelid = p.relfilenode", schemaName);
            string f_table, p_table;
            var ds = this.ExecuteSelect(stmt);
            var result = new List<ForeignKey>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                f_table = dr["table_name"].ToString();
                p_table = dr["pri_table_name"].ToString();
                if (!this._columns.ContainsKey(p_table))
                    continue;
                var fk = new ForeignKey(schemaName, f_table, dr["key_name"].ToString(), schemaName, p_table);
                fk.UpdateRule = this.ParseUpdateRule(dr["update_rule"].ToString());
                fk.DeleteRule = this.ParseDeleteRule(dr["delete_rule"].ToString());
                fk.Deferrability = (dr["is_deferrable"].ToString().ToLower() == "t") ? Deferrability.Deferrable : Deferrability.NotDeferrable;
                fk.InitialMode = (dr["initially_deferred"].ToString().ToLower() == "t") ? InitialMode.InitiallyDeferred : InitialMode.InitiallyImmediate;

                var fk_columns = dr["f_column_position"] as Int16[];
                var pk_columns = dr["p_column_position"] as Int16[];

                for (int i = 0; i < fk_columns.Length; i++)
                {
                    fk.AddColumn(i,
                        this._columns[f_table].Single(c => c.OrdinalPosition == fk_columns[i]).ColumnName,
                        this._columns[p_table].Single(c => c.OrdinalPosition == pk_columns[i]).ColumnName);
                }
                result.Add(fk);
            }
            return result;
        }

        public override List<TableCardinality> GetTableCardinalities(string schemaName)
        {
            var query = @"select nspname as SchemaName, relname as TableName, reltuples as RowCount 
                          from pg_class 
                          join pg_namespace as ns 
                            on relnamespace::oid = ns.oid
			              WHERE nspname = '{0}'
                          ORDER BY TableName";

            query = string.Format(query, schemaName);

            var res = ExecuteSelect(query);
            var tableRes = res.Tables[0];
            var result = new List<TableCardinality>(tableRes.Rows.Count + 10);
            foreach (var row in tableRes.Rows)
            {
                var r = (DataRow)row;
                result.Add(new TableCardinality { SchemaName = r["SchemaName"].ToString(), TableName = r["TableName"].ToString(), Cardinality = r["RowCount"].ToString() });
            }
            return result;
        }

        public override List<Index> GetIndices(string schemaName)
        {
            var stmt = String.Format(
                @"select
	                ixs.schemaname as schema_name,
	                ixs.tablename as table_name,
	                ixs.indexname as index_name,
	                jo.attname as column_name,
	                jo.attnum as column_index,
	                jo.indkey as index_order,
	                jo.indisunique as is_unique
                from pg_catalog.pg_indexes as ixs
                inner join (
	                select a.attname, a.attnum, ix.indkey, ix.indisunique, i.relname
	                from pg_catalog.pg_class as t,
		                pg_catalog.pg_class as i,
		                pg_catalog.pg_index as ix,
		                pg_catalog.pg_attribute as a
	                where a.attnum > 0
                    and t.oid = ix.indrelid
	                and i.oid = ix.indexrelid
	                and a.attrelid = t.oid
	                and a.attnum = ANY(ix.indkey)) as jo
                on ixs.indexname = jo.relname
                where ixs.schemaname = '{0}'",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<Index>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var index = new Index(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["index_name"].ToString());
                index.IsUnique = Convert.ToBoolean(dr["is_unique"]);
                var indexPos = dr["index_order"].ToString().Split(' ').ToArray();
                for (int i = 0; i < indexPos.Length; i++)
                {
                    if (Convert.ToInt32(dr["column_index"].ToString()) == Convert.ToInt32(indexPos[i]))
                    {
                        index.AddColumn(i + 1, dr["column_name"].ToString());
                        break;
                    }
                }
                Index tmp;
                if (!cList.Add(index, out tmp))
                    tmp.AddCoumns(index.IndexOrderdColumnNames);
            }
            return cList.ToList();
        }

        private DataType ParseDataType(string type)
        {
            var result = DataType.DEFAULT;
            switch (type.ToLower())
            {
                case "int8": result = DataType.BIGINT; break;
                case "bit": result = DataType.BIT; break;
                case "bool": result = DataType.BOOLEAN; break;
                case "varchar": result = DataType.VARCHAR; break;
                case "bpchar": result = DataType.CHAR; break;
                case "date": result = DataType.DATE; break;
                case "float8": result = DataType.FLOAT; break;
                case "int": result = DataType.INTEGER; break;
                case "int4": result = DataType.INTEGER; break;
                case "numeric": result = DataType.NUMERIC; break;
                case "float4": result = DataType.REAL; break;
                case "int2": result = DataType.SMALLINT; break;
                case "text": result = DataType.TEXT; break;
                case "time": result = DataType.TIME; break;
                case "timetz": result = DataType.TIME; break;
                case "timestamp": result = DataType.TIMESTAMP; break;
                case "timestamptz": result = DataType.TIMESTAMP; break;
                case "xml": result = DataType.XML; break;
            }
            return result;
        }

        private UpdateRule ParseUpdateRule(string rule)
        {
            var result = UpdateRule.NOACTION;
            switch (rule.ToLower())
            {
                case "a": result = UpdateRule.NOACTION; break;
                case "r": result = UpdateRule.RESTRICT; break;
                case "c": result = UpdateRule.CASCADE; break;
                case "n": result = UpdateRule.NULL; break;
                case "d": result = UpdateRule.DEFAULT; break;
            }
            return result;
        }

        private DeleteRule ParseDeleteRule(string rule)
        {
            var result = DeleteRule.NOACTION;
            switch (rule.ToLower())
            {
                case "a": result = DeleteRule.NOACTION; break;
                case "r": result = DeleteRule.RESTRICT; break;
                case "c": result = DeleteRule.CASCADE; break;
                case "n": result = DeleteRule.NULL; break;
                case "d": result = DeleteRule.DEFAULT; break;
            }
            return result;
        }

        private Regex defSeq = new Regex(@"^nextval\('(.+)'::[a-z ]+\)");
        private Regex defStr = new Regex(@"^'(.*)'::[a-z ]+");
        private Regex defNull = new Regex(@"^NULL::[a-z ]+");

        private String ParseDefaultValue(string value, out bool sequence)
        {
            sequence = false;
            if (value.Equals(""))
                return null;
            var match = defStr.Match(value);
            if (match.Success)
                return match.Groups[1].ToString();
            var seqMatch = defSeq.Match(value);
            if (seqMatch.Success)
            {
                sequence = true;
                return String.Format("nextval({0})", seqMatch.Groups[1]);
            }
            var nullMatch = defNull.Match(value);
            if (nullMatch.Success)
                return null;
            return value;
        }

        private bool ParseBoolean(string boolean)
        {
            return (boolean.ToLower() == "yes");
        }

        public override object QueryTable(string query)
        {
            return this.ExecuteSelect(query).Tables[0].Rows[0][0];
        }

        public override decimal GetDecimal(string query)
        {
            var value = this.ExecuteSingleValue(query);
            return Convert.ToDecimal(value);
        }

        public override List<StoredProcedure> GetStoredProcedures(string schemaName)
        {
            return new List<StoredProcedure>();
        }

        public override List<Function> GetFunctions(string schemaName)
        {
            return new List<Function>();
        }

        public override List<Parameter> GetStoredProceduresParameters(string schemaName)
        {
            return new List<Parameter>();
        }

        public override List<Parameter> GetFunctionsParameters(string schemaName)
        {
            return new List<Parameter>();
        }

        public override List<View> GetViews(string schemaName)
        {
            return new List<View>();
        }

        public override List<ViewColumn> GetViewColumns(string schemaName)
        {
            return new List<ViewColumn>();
        }
    }
}
