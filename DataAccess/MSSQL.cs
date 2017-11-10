using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using DBLint.DataAccess.DBObjects;
using System.Data.SqlClient;
using DBLint.DataTypes;

namespace DBLint.DataAccess
{
    public class MSSQL : Factory
    {
        public MSSQL(Connection connectionInfo, int timeout)
        {
            var connBuilder = new SqlConnectionStringBuilder();

            if (connectionInfo.AuthenticationMethod == AuthenticationMethod.WindowsAuthentication)
                connBuilder["Integrated Security"] = "SSPI";
            else
            {
                connBuilder["User Id"] = connectionInfo.UserName;
                connBuilder["Password"] = connectionInfo.Password;
            }
            if (!String.IsNullOrEmpty(connectionInfo.Port))
                connBuilder["Data Source"] = String.Format("{0},{1}", connectionInfo.Host, connectionInfo.Port);
            else
                connBuilder["Data Source"] = connectionInfo.Host;
            connBuilder["Initial Catalog"] = connectionInfo.Database;
            connBuilder["Max Pool Size"] = connectionInfo.MaxConnections;
            connBuilder["Timeout"] = timeout;

            this.Setup(connBuilder.ConnectionString, SqlClientFactory.Instance);
        }

        public override DBMSs DBMS
        {
            get { return DBMSs.MSSQL; }
        }

        public override string GetFormatedSelectStatement(string schemaName, string tableName, List<String> columnNames)
        {
            return String.Format("select [{0}] from [{1}].[{2}]", String.Join("], [", columnNames), schemaName, tableName);
        }

        public override Data.DataRow GetDataRow(System.Data.Common.DbDataReader reader)
        {
            return new Data.MSSQLDataRow(reader);
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

        public override List<Column> GetColumns(string schemaName)
        {
            var sequenceColumns = this.GetSequenceColumns(schemaName);
            var stmt = String.Format(
                @"select
                    col.table_schema as schema_name,
                    col.table_name,
                    col.column_name,
                    col.ordinal_position,
                    col.is_nullable,
                    col.data_type,
                    col.character_maximum_length as character_max_length,
                    col.numeric_precision,
                    col.numeric_scale,
                    col.column_default as default_value
                from information_schema.columns as col
                inner join information_schema.tables as tab
                on col.table_name = tab.table_name
                    and col.table_schema = tab.table_schema
                where tab.table_schema = '{0}'
                    and tab.table_type = 'BASE TABLE'",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var result = new List<Column>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var column = new Column(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["column_name"].ToString());
                column.OrdinalPosition = Convert.ToInt32(dr["ordinal_position"]);
                column.IsNullable = this.ParseBoolean(dr["is_nullable"].ToString());
                column.DataType = this.ParseDataType(dr["data_type"].ToString());
                if (dr["character_max_length"] != DBNull.Value)
                    column.CharacterMaxLength = Convert.ToInt64(dr["character_max_length"]);
                if (dr["numeric_precision"] != DBNull.Value)
                    column.NumericPrecision = Convert.ToInt32(dr["numeric_precision"]);
                if (dr["numeric_scale"] != DBNull.Value)
                    column.NumericScale = Convert.ToInt32(dr["numeric_scale"]);
                if (dr["default_value"] != DBNull.Value)
                {
                    bool isFunction;
                    column.DefaultValue = this.ParseDefaultValue(dr["default_value"].ToString(), column.DataType, out isFunction);
                    column.DefaultIsFunction = isFunction;
                }
                if (sequenceColumns.ContainsKey(column.TableName))
                    column.IsSequence = sequenceColumns[column.TableName].Contains(column.ColumnName);
                result.Add(column);
            }
            return result;
        }

        private Dictionary<string, HashSet<string>> GetSequenceColumns(string schemaName)
        {
            var stmt = String.Format(@"select
                                           tb.name as table_name,
                                           co.name as column_name
                                       from
                                           sys.schemas as sh,
                                           sys.tables as tb,
                                           sys.identity_columns as co
                                       where sh.name = '{0}'
                                           and tb.schema_id = sh.schema_id
                                           and co.object_id = tb.object_id", schemaName);
            var ds = this.ExecuteSelect(stmt);
            var values = new Dictionary<string, HashSet<string>>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var tableName = dr["table_name"].ToString();
                if (!values.ContainsKey(tableName))
                    values.Add(tableName, new HashSet<string>());
                values[tableName].Add(dr["column_name"].ToString());
            }
            return values;
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

        public override List<TableCardinality> GetTableCardinalities(string schemaName)
        {
            var query = @"SELECT  tbl.TABLE_SCHEMA AS [SchemaName] ,
                                  tbl.TABLE_NAME AS [TableName] ,
                                  rowcnt AS [RowCount]
                          FROM    INFORMATION_SCHEMA.TABLES AS tbl
                                  JOIN ( SELECT   id ,
                                                  rowcnt ,
                                                  indid
                                         FROM     sysindexes
                         ) AS indices ON OBJECT_ID(tbl.TABLE_SCHEMA + '.' + tbl.TABLE_NAME) = indices.id
                         WHERE   indid < 2
                         AND tbl.[TABLE_SCHEMA] = '{0}'";
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


        public override List<ForeignKey> GetForeignKeys(string schemaName)
        {
            var stmt = String.Format(
            @"select
	                fky.table_schema as schema_name,
	                fky.table_name,
	                fky.constraint_name as key_name,
	                fky.column_name,
	                pky.table_schema as pri_schema_name,
	                pky.table_name as pri_table_name,
	                pky.column_name as pri_column_name,
	                fky.ordinal_position as ordinal_position,
	                rco.update_rule,
	                rco.delete_rule,
	                co.is_deferrable,
                    co.initially_deferred
                from information_schema.key_column_usage as fky
                inner join information_schema.table_constraints as co
                on fky.constraint_name = co.constraint_name
                inner join information_schema.referential_constraints as rco
                on rco.constraint_name = co.constraint_name
                inner join information_schema.key_column_usage as pky
                on pky.constraint_name = rco.unique_constraint_name
	                and fky.ordinal_position = pky.ordinal_position
                where co.constraint_schema = '{0}'
	                and co.constraint_type = 'FOREIGN KEY'
                order by table_name, key_name, ordinal_position",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<ForeignKey>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var fk = new ForeignKey(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["key_name"].ToString(),
                    dr["pri_schema_name"].ToString(), dr["pri_table_name"].ToString());
                fk.UpdateRule = this.ParseUpdateRule(dr["update_rule"].ToString());
                fk.DeleteRule = this.ParseDeleteRule(dr["delete_rule"].ToString());
                fk.Deferrability = this.ParseBoolean(dr["is_deferrable"].ToString()) ? Deferrability.Deferrable : Deferrability.NotDeferrable;
                fk.InitialMode = this.ParseBoolean(dr["initially_deferred"].ToString()) ? InitialMode.InitiallyDeferred : InitialMode.InitiallyImmediate;
                fk.AddColumn(Convert.ToInt32(dr["ordinal_position"]), dr["column_name"].ToString(), dr["pri_column_name"].ToString());
                ForeignKey tmp;
                if (!cList.Add(fk, out tmp))
                    tmp.AddColumns(fk.IndexOrderdColumnNames);
            }
            return cList.ToList();
        }

        public override List<Index> GetIndices(string schemaName)
        {
            var stmt = String.Format(
                @"select
	                '{0}' as schema_name,
	                tab.name as table_name,
	                inx.name as index_name,
	                col.name as column_name,
	                ixcol.key_ordinal as column_index,
	                inx.is_unique
                from sys.objects as obj
                inner join sys.indexes as inx
                on obj.object_id = inx.object_id
                inner join sys.index_columns as ixcol
                on inx.object_id = ixcol.object_id
	                and inx.index_id = ixcol.index_id
                inner join sys.columns as col
                on obj.object_id = col.object_id
	                and ixcol.column_id = col.column_id
                inner join sys.tables as tab
                on obj.object_id = tab.object_id
                where obj.schema_id = (select schema_id from sys.schemas where name = '{0}')
	                and inx.is_disabled = 0
	                and inx.name is not Null
                    and ixcol.is_included_column = 0",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<Index>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var index = new Index(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["index_name"].ToString());
                index.IsUnique = Convert.ToBoolean(dr["is_unique"]);
                index.AddColumn(Convert.ToInt32(dr["column_index"]), dr["column_name"].ToString());
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
                case "bigint": result = DataType.BIGINT; break;
                case "smallint": result = DataType.SMALLINT; break;
                case "tinyint": result = DataType.TINYINT; break;
                case "int": result = DataType.INTEGER; break;
                case "numeric": result = DataType.NUMERIC; break;
                case "real": result = DataType.REAL; break;
                case "float": result = DataType.FLOAT; break;
                case "decimal": result = DataType.DECIMAL; break;
                case "bit": result = DataType.BIT; break;
                // case "": result = DataType.BOOLEAN; break;
                case "varchar": result = DataType.VARCHAR; break;
                case "nvarchar": result = DataType.NVARCHAR; break;
                case "char": result = DataType.CHAR; break;
                case "nchar": result = DataType.NCHAR; break;
                case "varbinary": result = DataType.VARBINARY; break;
                case "text": result = DataType.TEXT; break;
                case "date": result = DataType.DATE; break;
                case "datetime": result = DataType.DATETIME; break;
                case "time": result = DataType.TIME; break;
                case "timestamp": result = DataType.TIMESTAMP; break;
                case "image": result = DataType.IMAGE; break;
                case "xml": result = DataType.XML; break;
            }
            return result;
        }

        private UpdateRule ParseUpdateRule(string rule)
        {
            var result = UpdateRule.NOACTION;
            switch (rule.ToLower())
            {
                case "no action": result = UpdateRule.NOACTION; break;
                case "restrict": result = UpdateRule.RESTRICT; break;
                case "cascade": result = UpdateRule.CASCADE; break;
                case "set null": result = UpdateRule.NULL; break;
                case "set default": result = UpdateRule.DEFAULT; break;
            }
            return result;
        }

        private DeleteRule ParseDeleteRule(string rule)
        {
            var result = DeleteRule.NOACTION;
            switch (rule.ToLower())
            {
                case "no action": result = DeleteRule.NOACTION; break;
                case "restrict": result = DeleteRule.RESTRICT; break;
                case "cascade": result = DeleteRule.CASCADE; break;
                case "set null": result = DeleteRule.NULL; break;
                case "set default": result = DeleteRule.DEFAULT; break;
            }
            return result;
        }

        private Regex defFunc = new Regex(@"^\((.*)\(\)\)");
        private Regex defStr = new Regex(@"^\('(.*)'\)");
        private Regex defCus = new Regex(@"^\((.*)\)");
        private Regex defNum = new Regex(@"^\(\((.*)\)\)");
        private Regex defDat = new Regex(@"^\(\(\(([0-9]+)\)\-\(([0-9]+)\)\)\-\(([0-9]+)\)\)");

        private string ParseDefaultValue(string value, DataType type, out bool isFunction)
        {
            Match match = defFunc.Match(value);
            isFunction = false;
            if (match.Success)
            {
                isFunction = true;
                return match.Groups[1].ToString();
            }
            if (DataTypes.DataTypesLists.NumericTypes().Contains(type) || type == DataType.BIT)
            {
                match = defNum.Match(value);
                if (match.Success)
                    return match.Groups[1].ToString();
            }
            else if (DataTypes.DataTypesLists.DateTypes().Contains(type))
            {
                match = defDat.Match(value);
                if (match.Success)
                    return String.Format("{0}-{1}-{2}", match.Groups[1], match.Groups[2], match.Groups[3]);
            }
            match = defStr.Match(value);
            if (match.Success)
                return match.Groups[1].ToString();
            match = defCus.Match(value);
            if (match.Success)
                return match.Groups[1].ToString();
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
