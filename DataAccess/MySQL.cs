using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using DBLint.DataAccess.DBObjects;
using DBLint.DataTypes;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace DBLint.DataAccess
{
    public class MySQL : Factory
    {
        private MySQLVersion _runningVersion = null;

        private class MySQLVersion
        {
            public int Major;
            public int Minor;
            public int Fix;
        }

        private class Constraint
        {
            public string ConstraintType;
            public string TableName;
            public string ConstraintName;
        }
        private Dictionary<string, List<Constraint>> _schemaConstraints = new Dictionary<string, List<Constraint>>();

        private List<Constraint> GetConstraints(string schema)
        {
            if (!this._schemaConstraints.ContainsKey(schema))
            {
                var stmt = string.Format(@"select CONSTRAINT_NAME, TABLE_NAME, CONSTRAINT_TYPE
                                           from information_schema.table_constraints
                                           where table_schema = '{0}'
                                           and constraint_type in ('UNIQUE', 'FOREIGN KEY', 'PRIMARY KEY')", schema);
                _schemaConstraints[schema] = (from dr in this.ExecuteSelect(stmt).Tables[0].Rows.Cast<DataRow>()
                                              select new Constraint
                                              {
                                                  ConstraintName = dr["CONSTRAINT_NAME"].ToString(),
                                                  ConstraintType = dr["CONSTRAINT_TYPE"].ToString(),
                                                  TableName = dr["TABLE_NAME"].ToString()
                                              }).ToList();
            }
            return _schemaConstraints[schema];
        }

        public MySQL(Connection connectionInfo, int timeout)
        {
            var connStr = String.Format("Server={0}; Database={1}; Uid={2}; Pwd={3}; Port={4}; Max Pool Size={5}; Connect Timeout={6}",
                connectionInfo.Host,
                connectionInfo.Database,
                connectionInfo.UserName,
                connectionInfo.Password,
                connectionInfo.Port,
                connectionInfo.MaxConnections,
                timeout);
            this.Setup(connStr, MySqlClientFactory.Instance);
        }

        public override DBMSs DBMS
        {
            get { return DBMSs.MYSQL; }
        }

        public override string GetFormatedSelectStatement(string schemaName, string tableName, List<String> columnNames)
        {
            return String.Format("select `{0}` from `{1}`.`{2}`", String.Join("`, `", columnNames), schemaName, tableName);
        }

        public override Data.DataRow GetDataRow(System.Data.Common.DbDataReader reader)
        {           
            return new Data.MySQLDataRow(reader);
        }

        private void SetVersion()
        {
            try
            {
                var stmt = @"select @@version";
                var versionStr = (String)this.ExecuteSingleValue(stmt);
                if (!String.IsNullOrEmpty(versionStr))
                {
                    var regex = new Regex(@"([0-9]{1})\.([0-9]+)\.([0-9]+).*");
                    var match = regex.Match(versionStr);
                    if (match.Success && match.Groups.Count == 4)
                    {
                        var major = Int32.Parse(match.Groups[1].Value);
                        var minor = Int32.Parse(match.Groups[2].Value);
                        var fix = Int32.Parse(match.Groups[3].Value);

                        this._runningVersion = new MySQLVersion { Major = major, Minor = minor, Fix = fix };
                    }
                }
            }
            catch
            {

            }
        }

        public override List<Schema> GetSchemas()
        {
            if (this._runningVersion == null)
                this.SetVersion();

            this._schemaConstraints.Clear();
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
            //List<Table> result = new List<Table>();

            var result = (from dr in ds.Tables[0].Rows.Cast<DataRow>()
                          select new Table(dr["schema_name"].ToString(), dr["table_name"].ToString()))
                      .AsParallel()
                      .ToList();

            //foreach (DataRow dr in ds.Tables[0].Rows)
            //    result.Add(new Table(dr["schema_name"].ToString(), dr["table_name"].ToString()));
            return result;
        }

        public override List<Column> GetColumns(string schemaName)
        {
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
                    col.column_default as default_value,
                    col.extra
                from information_schema.columns as col
                inner join (select * 
                            from information_schema.tables
                            where table_schema = '{0}'
                            and table_type = 'BASE TABLE'
                ) as tab using (table_name , table_schema)
                where col.table_schema = '{0}'",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            //var result = new List<Column>();
            var result = (from dr in ds.Tables[0].Rows.Cast<DataRow>()
                          select ConvertRowToColumn(dr)).ToList();
            return result;
        }

        private Column ConvertRowToColumn(DataRow dr)
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
                tmp.DefaultValue = this.ParseDefaultValue(dr["default_value"].ToString(), tmp.DataType);
            if (dr["extra"] != DBNull.Value)
            {
                if (dr["extra"].ToString().ToLower().Equals("auto_increment"))
                    tmp.IsSequence = true;
            }
            //result.Add(tmp);
            return tmp;
        }

        public override List<Unique> GetUniqueConstraints(string schemaName)
        {
            var stmt = String.Format(
                @"select 
	                ky.table_schema as schema_name,
                    ky.table_name,
	                ky.constraint_name as key_name,
	                ky.ordinal_position,
	                ky.column_name,
                    ky.constraint_name
                from information_schema.key_column_usage as ky
                where ky.table_schema = '{0}'
                order by ky.constraint_name, ky.ordinal_position",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<Unique>();
            var constraints = from c in this.GetConstraints(schemaName)
                              where c.ConstraintType == "UNIQUE"
                              select c;

            var rows = from dr in ds.Tables[0].Rows.Cast<DataRow>()
                       join c in constraints on dr["table_name"].ToString() equals c.TableName
                       where dr["constraint_name"].ToString() == c.ConstraintName
                       select new { row = dr, constraint = c };

            foreach (var item in rows)
            {
                var dr = item.row;
                var uqSchemaName = dr["schema_name"].ToString();
                var uqTableName = dr["table_name"].ToString();
                var uqkKeyName = dr["key_name"].ToString();
                var uq = new Unique(uqSchemaName, uqTableName, uqkKeyName);
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
                where table_schema = '{0}'
                order by ky.table_name, ky.ordinal_position",
                schemaName);
            var constraints = from c in GetConstraints(schemaName)
                              where c.ConstraintType == "PRIMARY KEY"
                              select c;

            var ds = this.ExecuteSelect(stmt);

            var rows = from dr in ds.Tables[0].Rows.Cast<DataRow>()
                       join c in constraints on dr["key_name"].ToString() equals c.ConstraintName
                       where c.TableName == dr["table_name"].ToString()
                       select dr;

            var cList = new Utils.ChainedList<PrimaryKey>();
            foreach (DataRow dr in rows)
            {
                var pk = new PrimaryKey(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["key_name"].ToString());
                pk.AddColumn(Convert.ToInt32(dr["ordinal_position"]), dr["column_name"].ToString());
                PrimaryKey tmp;
                if (!cList.Add(pk, out tmp))
                    tmp.AddCoumns(pk.IndexOrderdColumnNames);
            }
            return cList.ToList();
        }

        public override List<ForeignKey> GetForeignKeys(string schemaName)
        {
            var stmt = String.Format(
                @"select
                    fky.constraint_schema as schema_name,
                    fky.table_name,
                    fky.constraint_name as key_name,
                    fky.column_name,
                    fky.referenced_table_schema as pri_schema_name,
                    fky.referenced_table_name as pri_table_name,
                    fky.referenced_column_name as pri_column_name,
                    fky.ordinal_position,
                    rco.update_rule,
                    rco.delete_rule
                from (select * from information_schema.key_column_usage where table_schema = '{0}') as fky
                            
                inner join (select  constraint_schema as table_schema, 
                                    constraint_name, 
                                    table_name, 
                                    referenced_table_name, 
                                    unique_constraint_name, 
                                    update_rule, 
                                    delete_rule
                            from information_schema.referential_constraints 
                            WHERE CONSTRAINT_SCHEMA = '{0}'                           
                            ) as rco
                            using (table_schema, constraint_name, table_name, referenced_table_name)",
                schemaName);

            // Hack, to avoide the missing referential_constraints table on old MySQL Versions, default to "Set Default" rules
            if (this._runningVersion != null && this._runningVersion.Major == 5 && this._runningVersion.Minor == 0)
            {
                stmt = String.Format(@"select
	                constraint_schema as schema_name,
	                table_name,
	                constraint_name as key_name,
	                column_name,
	                referenced_table_schema as pri_schema_name,
	                referenced_table_name as pri_table_name,
	                referenced_column_name as pri_column_name,
	                ordinal_position,
	                'RESTRICT' as update_rule,
	                'CASCADE' as delete_rule
                from information_schema.key_column_usage where table_schema = '{0}'
                and constraint_name <> 'PRIMARY'",
                schemaName);
            }

            var constraints = from c in GetConstraints(schemaName)
                              where c.ConstraintType == "FOREIGN KEY"
                              select c;

            var ds = this.ExecuteSelect(stmt);

            var rows = from dr in ds.Tables[0].Rows.Cast<DataRow>()
                       join c in constraints on dr["table_name"].ToString() equals c.TableName
                       where c.ConstraintName == dr["key_name"].ToString()
                       select dr;


            var cList = new Utils.ChainedList<ForeignKey>();
            foreach (DataRow dr in rows)
            {
                var fk = new ForeignKey(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["key_name"].ToString(),
                    dr["pri_schema_name"].ToString(), dr["pri_table_name"].ToString());
                fk.UpdateRule = this.ParseUpdateRule(dr["update_rule"].ToString());
                fk.DeleteRule = this.ParseDeleteRule(dr["delete_rule"].ToString());
                // fk.IsDeferrable = this.ParseBoolean(dr["is_deferrable"].ToString()); // TODO NOT EXTRACTED FROM MYSQL
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
                    sta.table_schema as schema_name,
                    sta.table_name,
                    sta.index_name,
                    sta.column_name,
                    sta.seq_in_index as column_index,
                    sta.non_unique
                  from   information_schema.statistics sta
                  where  sta.table_schema = '{0}'",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<Index>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var index = new Index(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["index_name"].ToString());
                index.IsUnique = !Convert.ToBoolean(dr["non_unique"]);
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
                case "mediumint": result = DataType.INTEGER; break;
                case "float": result = DataType.FLOAT; break;
                case "decimal": result = DataType.DECIMAL; break;
                case "double": result = DataType.DOUBLE; break;
                case "varchar": result = DataType.VARCHAR; break;
                case "char": result = DataType.CHAR; break;
                case "text": result = DataType.TEXT; break;
                case "mediumtext": result = DataType.TEXT; break;
                case "longtext": result = DataType.TEXT; break;
                case "enum": result = DataType.ENUM; break;
                case "blob": result = DataType.BLOB; break;
                case "longblob": result = DataType.BLOB; break;
                case "date": result = DataType.DATE; break;
                case "datetime": result = DataType.DATETIME; break;
                case "time": result = DataType.TIME; break;
                case "timestamp": result = DataType.TIMESTAMP; break;
                case "bit": result = DataType.BIT; break;
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

        private DBLint.DataTypes.ParameterDirection ParseParameterDirection(string direction)
        {
            var result = DBLint.DataTypes.ParameterDirection.Unknown;
            switch (direction.ToLower())
            {
                case "in": result = DataTypes.ParameterDirection.Input; break;
                case "out": result = DataTypes.ParameterDirection.Output; break;
                case "inout": result = DataTypes.ParameterDirection.InputOutput; break;
            }
            return result;
        }

        public override List<TableCardinality> GetTableCardinalities(string schemaName)
        {
            var query = @"SELECT TABLE_SCHEMA as SchemaName, TABLE_NAME as TableName, TABLE_ROWS as RowCount 
                          FROM INFORMATION_SCHEMA.TABLES
                          WHERE `TABLE_SCHEMA` = '{0}'
                          ORDER BY SchemaName, TableName";
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

        private Regex defBit = new Regex(@"^b'([0|1])'");

        private String ParseDefaultValue(string value, DataType type)
        {
            if (type == DataType.BIT)
            {
                var match = defBit.Match(value);
                if (match.Success)
                    return match.Groups[1].ToString();
            }
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
            var result = new List<StoredProcedure>();
            var query = @"
                select
	                r.routine_schema as schema_name,
	                r.routine_name
                from information_schema.routines as r
                where
	                r.routine_schema = '{0}'
	                and r.routine_type = 'PROCEDURE';";
            query = string.Format(query, schemaName);
            var res = ExecuteSelect(query);
            foreach (var row in res.Tables[0].Rows)
            {
                var r = (DataRow)row;
                result.Add(new StoredProcedure
                {
                    SchemaName = r["schema_name"].ToString(),
                    RoutineName = r["routine_name"].ToString()
                });
            }
            return result;
        }

        public override List<Function> GetFunctions(string schemaName)
        {
            var result = new List<Function>();
            var query = @"
                select
	                r.routine_schema as schema_name,
	                r.routine_name
                from information_schema.routines as r
                where
	                r.routine_schema = '{0}'
	                and r.routine_type = 'FUNCTION'";
            query = string.Format(query, schemaName);
            var res = ExecuteSelect(query);
            foreach (var row in res.Tables[0].Rows)
            {
                var r = (DataRow)row;
                result.Add(new Function
                {
                    SchemaName = r["schema_name"].ToString(),
                    RoutineName = r["routine_name"].ToString()
                });
            }
            return result;
        }

        private List<Parameter> GetParametersInternally(string schemaName, string routineType)
        {
            var result = new List<Parameter>();

            // Hack to escape too old versions, that dont have the needed tables in the information schema
            if (this._runningVersion != null && this._runningVersion.Major == 5 && this._runningVersion.Minor < 3)
                return result;

            var query = @"
                select
	                p.specific_name as schema_name,
	                p.specific_name as procedure_name,
	                p.ordinal_position,
	                p.parameter_mode,
	                p.parameter_name,
	                p.data_type,
	                p.character_maximum_length as character_max_length,
	                p.numeric_precision,
	                p.numeric_scale
                from information_schema.routines as r
                inner join information_schema.parameters as p on r.routine_catalog = p.specific_catalog
                and r.routine_schema = p.specific_schema
                and r.routine_name = p.specific_name
                where
	                r.routine_schema = '{0}'
	                and r.routine_type = '{1}'
                order by p.specific_name, p.ordinal_position asc";
            query = string.Format(query, schemaName, routineType);
            var res = ExecuteSelect(query);
            foreach (var row in res.Tables[0].Rows)
            {
                var r = (DataRow)row;
                var p = new Parameter
                {
                    SchemaName = r["schema_name"].ToString(),
                    RoutineName = r["procedure_name"].ToString(),
                    OrdinalPosition = Convert.ToInt32(r["ordinal_position"]),
                    Direction = this.ParseParameterDirection(r["parameter_mode"].ToString()),
                    ParameterName = r["parameter_name"].ToString(),
                    DataType = this.ParseDataType(r["data_type"].ToString())
                };

                if (r["character_max_length"] != DBNull.Value)
                    p.CharacterMaxLength = Convert.ToInt64(r["character_max_length"]);
                if (r["numeric_precision"] != DBNull.Value)
                    p.NumericPrecision = Convert.ToInt32(r["numeric_precision"]);
                if (r["numeric_scale"] != DBNull.Value)
                    p.NumericScale = Convert.ToInt32(r["numeric_scale"]);
                result.Add(p);
            }
            return result;
        }

        public override List<Parameter> GetStoredProceduresParameters(string schemaName)
        {
            return this.GetParametersInternally(schemaName, "PROCEDURE");
        }

        public override List<Parameter> GetFunctionsParameters(string schemaName)
        {
            return this.GetParametersInternally(schemaName, "FUNCTION");
        }

        public override List<View> GetViews(string schemaName)
        {
            var stmt = String.Format(
               @"select
                    table_schema as schema_name,
                    table_name as view_name
                from information_schema.tables
                where table_schema = '{0}'
                    and table_type = 'VIEW'",
               schemaName);
            var ds = this.ExecuteSelect(stmt);
            var result = (from dr in ds.Tables[0].Rows.Cast<DataRow>()
                          select new View(dr["schema_name"].ToString(), dr["view_name"].ToString()))
                      .AsParallel()
                      .ToList();
            return result;
        }

        public override List<ViewColumn> GetViewColumns(string schemaName)
        {
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
                    col.column_default as default_value,
                    col.privileges
                from information_schema.columns as col
                inner join (select * 
                            from information_schema.tables
                            where table_schema = '{0}'
                            and table_type = 'VIEW'
                ) as tab using (table_name , table_schema)
                where col.table_schema = '{0}'",
                schemaName);
            var ds = this.ExecuteSelect(stmt);
            //var result = new List<Column>();
            var result = (from dr in ds.Tables[0].Rows.Cast<DataRow>()
                          select ConvertRowToViewColumn(dr)).ToList();
            return result;
        }

        private Privileges ParsePrivileges(string value)
        {
            var result = Privileges.None;
            var privilegesArr = value.Split(',');
            foreach (var pr in privilegesArr)
            {
                if (pr.ToLower() == "select")
                    result |= Privileges.Select;
                if (pr.ToLower() == "insert")
                    result |= Privileges.Insert;
                if (pr.ToLower() == "references")
                    result |= Privileges.References;
                if (pr.ToLower() == "update")
                    result |= Privileges.Update;
            }
            if (result != Privileges.None)
                result &= ~Privileges.None;
            return result;
        }

        private ViewColumn ConvertRowToViewColumn(DataRow dr)
        {
            var tmp = new ViewColumn(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["column_name"].ToString());
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
                tmp.DefaultValue = this.ParseDefaultValue(dr["default_value"].ToString(), tmp.DataType);
            if (dr["privileges"] != DBNull.Value)
                tmp.Privileges = this.ParsePrivileges(dr["privileges"].ToString());
            //result.Add(tmp);
            return tmp;
        }
    }
}
