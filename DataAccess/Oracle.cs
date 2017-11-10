using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataAccess.DBObjects;
using Oracle.DataAccess.Client;
using DBLint.DataTypes;
using System.IO;

namespace DBLint.DataAccess
{
    public class Oracle : Factory
    {
        public Oracle(Connection connectionInfo, int timeout)
        {
            if (DBLint.Settings.IsNormalContext)
            {
                var path = Directory.GetCurrentDirectory() + "\\x86";
                Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("ORACLE_HOME", path, EnvironmentVariableTarget.Process);

                this.SetupLogDirectory(path);
            }

            var connStr = String.Format("User Id={0};Password={1};Data Source={2}:{3}/{4};Max Pool Size={5}; Connect Timeout={6};",
                connectionInfo.UserName,
                connectionInfo.Password,
                connectionInfo.Host,
                connectionInfo.Port,
                connectionInfo.Database,
                connectionInfo.MaxConnections,
                timeout);
            this.Setup(connStr, OracleClientFactory.Instance);
        }

        private void SetupLogDirectory(string path)
        {
            var log = String.Format("{0}//log", path);
            var diag = String.Format("{0}//diag", log);
            var clients = String.Format("{0}//clients", diag);
            if (!Directory.Exists(log))
                Directory.CreateDirectory(log);
            if (!Directory.Exists(diag))
                Directory.CreateDirectory(diag);
            if (!Directory.Exists(clients))
                Directory.CreateDirectory(clients);
        }

        public override DBMSs DBMS
        {
            get { return DBMSs.ORACLE; }
        }

        public override string GetFormatedSelectStatement(string schemaName, string tableName, List<string> columnNames)
        {
            return String.Format("select \"{0}\" from \"{1}\".\"{2}\"", String.Join("\", \"", columnNames), schemaName, tableName);
        }

        public override Data.DataRow GetDataRow(System.Data.Common.DbDataReader reader)
        {
            return new Data.OracleDataRow(reader);
        }

        public override List<Schema> GetSchemas()
        {
            var stmt = String.Format("select username as schema_name from all_users");
            using (var ds = this.ExecuteSelect(stmt))
            {
                var result = new List<Schema>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                    result.Add(new Schema(dr["schema_name"].ToString()));
                return result;
            }
        }

        public override List<Table> GetTables(string schemaName)
        {
            var stmt = String.Format(
                @"select
                    owner as schema_name,
                    table_name
                from   all_tables
                where temporary = 'N'
                    and owner = '{0}'", schemaName);
            using (var ds = this.ExecuteSelect(stmt))
            {
                var result = new List<Table>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                    result.Add(new Table(dr["schema_name"].ToString(), dr["table_name"].ToString()));
                return result;
            }
        }

        public override List<Column> GetColumns(string schemaName)
        {
            var stmt = String.Format(
                @"select
                    owner as schema_name,
                    table_name,
                    column_name,
                    data_type,
                    char_length,
                    data_precision,
                    data_scale,
                    nullable,
                    column_id,
                    data_default
                from all_tab_columns col
                inner join (select owner, table_name
                            from all_tables
                            where owner = '{0}'
                            and temporary = 'N'
                ) tab using (owner, table_name)",
                schemaName);
            using (var ds = this.ExecuteSelect(stmt))
            {
                var result = new List<Column>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                    result.Add(ConvertRowToColumn(dr));
                return result;
            }
        }

        private Column ConvertRowToColumn(DataRow dr)
        {
            var tmp = new Column(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["column_name"].ToString());
            tmp.OrdinalPosition = Convert.ToInt32(dr["column_id"]);
            tmp.IsNullable = this.ParseBoolean(dr["nullable"].ToString());
            tmp.DataType = this.ParseDataType(dr["data_type"].ToString());
            if (dr["char_length"] != DBNull.Value)
                tmp.CharacterMaxLength = Convert.ToInt64(dr["char_length"]);
            if (dr["data_precision"] != DBNull.Value)
                tmp.NumericPrecision = Convert.ToInt32(dr["data_precision"]);
            if (dr["data_scale"] != DBNull.Value)
                tmp.NumericScale = Convert.ToInt32(dr["data_scale"]);
            if (dr["data_default"] != DBNull.Value)
                tmp.DefaultValue = dr["data_default"].ToString();
            return tmp;
        }

        public override List<Unique> GetUniqueConstraints(string schemaName)
        {
            var stmt = String.Format(
                @"select ucc.owner as schema_name,
                    ucc.table_name,
                    ucc.constraint_name,
                    ucc.position,
                    ucc.column_name
                  from   all_cons_columns ucc, all_constraints uc
                  where  uc.owner           = '{0}'
                  and    uc.owner           = ucc.owner
                  and    uc.constraint_name = ucc.constraint_name
                  and    uc.constraint_type = 'U'
                  order  by ucc.table_name, ucc.position",
                  schemaName);
            using (var ds = this.ExecuteSelect(stmt))
            {
                var cList = new Utils.ChainedList<Unique>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var uq = new Unique(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["constraint_name"].ToString());
                    uq.AddColumn(Convert.ToInt32(dr["position"]), dr["column_name"].ToString());
                    Unique tmp;
                    if (!cList.Add(uq, out tmp))
                        tmp.AddCoumns(uq.IndexOrderdColumnNames);
                }
                return cList.ToList();
            }
        }

        public override List<PrimaryKey> GetPrimaryKeys(string schemaName)
        {
            var stmt = String.Format(
                @"select ucc.owner as schema_name,
                    ucc.table_name,
                    ucc.constraint_name,
                    ucc.position,
                    ucc.column_name
                  from   all_cons_columns ucc, all_constraints uc
                  where  uc.owner           = '{0}'
                  and    uc.owner           = ucc.owner
                  and    uc.constraint_name = ucc.constraint_name
                  and    uc.constraint_type = 'P'
                  order  by ucc.table_name, ucc.position",
                  schemaName);
            using (var ds = this.ExecuteSelect(stmt))
            {
                var cList = new Utils.ChainedList<PrimaryKey>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var pk = new PrimaryKey(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["constraint_name"].ToString());
                    pk.AddColumn(Convert.ToInt32(dr["position"]), dr["column_name"].ToString());
                    PrimaryKey tmp;
                    if (!cList.Add(pk, out tmp))
                        tmp.AddCoumns(pk.IndexOrderdColumnNames);
                }
                return cList.ToList();
            }
        }
        public override List<ForeignKey> GetForeignKeys(string schemaName)
        {
            var stmt = String.Format(
                @"select
                    c.owner as schema_name,
                    c.constraint_name as key_name,
                    c.delete_rule,
                    c.deferrable,
                    c.deferred,
                    source.table_name as table_name,
                    source.column_name as column_name,
                    source.position,
                    target.owner as pri_schema_name,
                    target.table_name as pri_table_name,
                    target.column_name as pri_column_name
                from
                    all_constraints c,
                    all_cons_columns source,
                    all_cons_columns target
                where c.owner = '{0}'
                and   c.constraint_type = 'R'
                and   c.constraint_name = source.constraint_name
                and   c.r_constraint_name = target.constraint_name
                and   source.position = target.position
                and   source.owner = '{0}'
                and   target.owner = '{0}'", schemaName);
            using (var ds = this.ExecuteSelect(stmt))
            {
                var cList = new Utils.ChainedList<ForeignKey>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var fk = new ForeignKey(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["key_name"].ToString(),
                        dr["pri_schema_name"].ToString(), dr["pri_table_name"].ToString());
                    // TODO missing update rule
                    fk.DeleteRule = this.ParseDeleteRule(dr["delete_rule"].ToString());
                    fk.Deferrability = this.ParseDeferrability(dr["deferrable"].ToString());
                    fk.InitialMode = this.ParseInitialMode(dr["deferred"].ToString());
                    fk.AddColumn(Convert.ToInt32(dr["position"]), dr["column_name"].ToString(), dr["pri_column_name"].ToString());
                    ForeignKey tmp;
                    if (!cList.Add(fk, out tmp))
                        tmp.AddColumns(fk.IndexOrderdColumnNames);
                }
                return cList.ToList();
            }
        }

        public override List<TableCardinality> GetTableCardinalities(string schemaName)
        {
            var stmt = String.Format("select owner as schema_name, table_name, num_rows from all_tables where owner = '{0}'", schemaName);
            using (var ds = this.ExecuteSelect(stmt))
            {
                var result = new List<TableCardinality>(ds.Tables[0].Rows.Count + 10);
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    result.Add(new TableCardinality { SchemaName = dr["schema_name"].ToString(), TableName = dr["table_name"].ToString(), Cardinality = dr["num_rows"].ToString() });
                }
                return result;
            }
        }

        public override List<Index> GetIndices(string schemaName)
        {
            var stmt = String.Format(
                @"select
                    owner,
                    index_name,
                    ind.table_name,
                    ind.uniqueness,
                    col.column_name,
                    col.column_position
                from all_indexes ind
                inner join (select * from all_ind_columns where index_owner = '{0}') col using (index_name)
                where ind.owner = '{0}'
                and ind.table_type = 'TABLE'
                order by index_name, column_position", schemaName);
            using (var ds = this.ExecuteSelect(stmt))
            {
                var cList = new Utils.ChainedList<Index>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var index = new Index(dr["owner"].ToString(), dr["table_name"].ToString(), dr["index_name"].ToString());
                    index.IsUnique = (dr["uniqueness"].ToString() == "UNIQUE");
                    index.AddColumn(Convert.ToInt32(dr["column_position"]), dr["column_name"].ToString());
                    Index tmp;
                    if (!cList.Add(index, out tmp))
                        tmp.AddCoumns(index.IndexOrderdColumnNames);
                }
                return cList.ToList();
            }
        }

        public override object QueryTable(string query)
        {
            using (var ds = this.ExecuteSelect(query))
            {
                return ds.Tables[0].Rows[0][0];
            }
        }

        private DataType ParseDataType(string type)
        {
            var result = DataType.DEFAULT;
            switch (type.ToLower())
            {
                //case "bigint": result = DataType.BIGINT; break;
                //case "smallint": result = DataType.SMALLINT; break;
                //case "tinyint": result = DataType.TINYINT; break;
                //case "int": result = DataType.INTEGER; break;
                //case "mediumint": result = DataType.INTEGER; break;
                //case "float": result = DataType.FLOAT; break;
                case "dec": result = DataType.DECIMAL; break;
                case "decimal": result = DataType.DECIMAL; break;
                case "number": result = DataType.NUMBER; break;
                case "numeric": result = DataType.NUMERIC; break;
                //case "double": result = DataType.DOUBLE; break;
                case "varchar2": result = DataType.VARCHAR; break;
                case "nvarchar2": result = DataType.NVARCHAR; break;
                case "char": result = DataType.CHAR; break;
                case "nchar": result = DataType.NCHAR; break;
                case "long": result = DataType.LONGVARCHAR; break;
                //case "mediumtext": result = DataType.TEXT; break;
                //case "longtext": result = DataType.TEXT; break;
                //case "enum": result = DataType.ENUM; break;
                case "blob": result = DataType.BLOB; break;
                case "cblob": result = DataType.CLOB; break;
                case "nclob": result = DataType.NCLOB; break;
                case "date": result = DataType.DATE; break;
                case "datetime": result = DataType.DATETIME; break;
                case "time": result = DataType.TIME; break;
                case "timestamp": result = DataType.TIMESTAMP; break;
                case "bit": result = DataType.BIT; break;
            }
            return result;
        }

        private bool ParseBoolean(string boolean)
        {
            return (boolean.ToLower() == "y");
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

        private Deferrability ParseDeferrability(string deferrability)
        {
            var result = Deferrability.Unknown;
            switch (deferrability.ToLower())
            {
                case "not deferrable": result = Deferrability.NotDeferrable; break;
                case "deferrable": result = Deferrability.Deferrable; break;
            }
            return result;
        }

        private InitialMode ParseInitialMode(string mode)
        {
            var result = InitialMode.Unknown;
            switch (mode.ToLower())
            {
                case "immediate": result = InitialMode.InitiallyImmediate; break;
                case "deferred": result = InitialMode.InitiallyDeferred; break;
            }
            return result;
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