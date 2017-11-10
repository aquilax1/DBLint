using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirebirdSql.Data.FirebirdClient;
using DBLint.DataAccess.DBObjects;
using System.Data;
using System.Text.RegularExpressions;
using DBLint.DataTypes;

namespace DBLint.DataAccess
{
    public class Firebird : Factory
    {
        private string _schemaName;

        public Firebird(Connection connectionInfo, int timeout)
        {
            var connStr = String.Format("User={0};Password={1};Database={2};DataSource={3};Port={4};Connection lifetime={5};Pooling=true; MinPoolSize=0;MaxPoolSize={6};",
                connectionInfo.UserName,
                connectionInfo.Password,
                connectionInfo.Database,
                connectionInfo.Host,
                connectionInfo.Port,
                timeout,
                connectionInfo.MaxConnections);
            this.Setup(connStr, FirebirdClientFactory.Instance);
            this._schemaName = connectionInfo.UserName;
        }

        public override DBMSs DBMS
        {
            get { return DBMSs.FIREBIRD; }
        }

        public override string GetFormatedSelectStatement(string schemaName, string tableName, List<string> columnNames)
        {
            return String.Format("select \"{0}\" from \"{1}\"", String.Join("\", \"", columnNames), tableName);
        }

        public override Data.DataRow GetDataRow(System.Data.Common.DbDataReader reader)
        {
            return new Data.FirebirdDataRow(reader);
        }

        public override List<DBObjects.Schema> GetSchemas()
        {
            var result = new List<Schema>();
            result.Add(new Schema(this._schemaName));
            return result;
        }

        public override List<DBObjects.Table> GetTables(string schemaName)
        {
            var stmt = String.Format(
                @"SELECT
                  RDB$RELATION_NAME as TableName
                FROM
                  RDB$RELATIONS
                WHERE
                  RDB$SYSTEM_FLAG = 0 AND
                  RDB$VIEW_BLR IS NULL AND
                  (RDB$RELATION_TYPE = 0 OR RDB$RELATION_TYPE IS NULL);");
            using (var ds = this.ExecuteSelect(stmt))
            {
                var result = new List<Table>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                    result.Add(new Table(this._schemaName, dr["TableName"].ToString().Trim()));
                return result;
            }
        }

        public override List<DBObjects.Column> GetColumns(string schemaName)
        {
            var stmt = String.Format(
                @"SELECT
                  rf.RDB$RELATION_NAME AS TableName,
                  rf.RDB$FIELD_NAME AS FieldName,
                  rf.RDB$FIELD_POSITION + 1 AS FieldPosition,
                  rf.RDB$DEFAULT_SOURCE AS DefaultValue,
                  rf.RDB$NULL_FLAG AS NotNull,
                  f.RDB$FIELD_LENGTH AS FieldLength,
                  f.RDB$FIELD_PRECISION AS FieldPrecision,
                  f.RDB$FIELD_SCALE AS FieldScale,
                  f.RDB$FIELD_TYPE AS DataType,
                  f.RDB$FIELD_SUB_TYPE AS BlobSubType
                FROM
                  RDB$RELATION_FIELDS rf
                  LEFT JOIN RDB$RELATIONS r on r.rdb$relation_name = rf.rdb$relation_name
                  LEFT JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
                WHERE
                  r.RDB$SYSTEM_FLAG = 0 AND
                  r.RDB$VIEW_BLR IS NULL AND
                  (r.RDB$RELATION_TYPE = 0 OR r.RDB$RELATION_TYPE IS NULL)
                ORDER BY
                  rf.RDB$RELATION_NAME,
                  rf.RDB$FIELD_POSITION;");
            using (var ds = this.ExecuteSelect(stmt))
            {
                var result = new List<Column>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var tmp = new Column(this._schemaName, dr["TableName"].ToString().Trim(), dr["FieldName"].ToString().Trim());
                    tmp.OrdinalPosition = Convert.ToInt32(dr["FieldPosition"]);
                    tmp.IsNullable = (dr["NotNull"] != DBNull.Value) ? false : true;
                    tmp.DataType = this.ParseDataType(Convert.ToInt32(dr["DataType"]), Convert.ToInt32(dr["BlobSubType"]));
                    tmp.CharacterMaxLength = Convert.ToInt64(dr["FieldLength"]);
                    if (dr["FieldPrecision"] != DBNull.Value)
                        tmp.NumericPrecision = Convert.ToInt32(dr["FieldPrecision"]);
                    if (dr["FieldScale"] != DBNull.Value)
                        tmp.NumericScale = Math.Abs(Convert.ToInt32(dr["FieldScale"]));
                    if (dr["DefaultValue"] != DBNull.Value)
                    {
                        tmp.DefaultValue = this.ParseDefaultValue(dr["DefaultValue"].ToString().Trim());
                        if (tmp.DefaultValue.ToUpper() == "CURRENT_DATE")
                            tmp.DefaultIsFunction = true;
                    }
                    // Handle cases where the values is actually a bigint
                    if (tmp.DataType == DataType.NUMERIC && tmp.NumericPrecision == 0 && tmp.NumericScale == 0)
                        tmp.DataType = DataType.BIGINT;
                    if (tmp.DataType == DataType.NUMERIC && tmp.NumericPrecision == 0 && tmp.CharacterMaxLength != 0)
                    {
                        tmp.NumericPrecision = (int)tmp.CharacterMaxLength;
                        tmp.CharacterMaxLength = 0;
                    }
                    result.Add(tmp);
                }
                return result;
            }
        }

        private Regex defStringValue = new Regex(@"^DEFAULT '(.+)'");
        private Regex defValue = new Regex(@"^DEFAULT (.+)");

        private string ParseDefaultValue(string value)
        {
            var match = defStringValue.Match(value);
            if (match.Success)
                return match.Groups[1].ToString();
            match = defValue.Match(value);
            if (match.Success)
                return match.Groups[1].ToString();
            return value;
        }

        public override List<DBObjects.Unique> GetUniqueConstraints(string schemaName)
        {
            var stmt = String.Format(
                @"SELECT 
                  rc.rdb$constraint_name as ConstraintName,
                  rc.rdb$relation_name as TableName,
                  s.rdb$field_name as FieldName,
                  s.rdb$field_position+1 as FieldPosition,
                  rc.rdb$deferrable as Deferrable,
                  rc.rdb$initially_deferred as InitiallyDeferred
                FROM rdb$relation_constraints rc
                  LEFT JOIN rdb$index_segments s ON (rc.rdb$index_name = s.rdb$index_name)
                WHERE 
                  rc.rdb$constraint_type = 'UNIQUE';");
            using (var ds = this.ExecuteSelect(stmt))
            {
                var cList = new Utils.ChainedList<Unique>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var uk = new Unique(this._schemaName, dr["TableName"].ToString().Trim(), dr["ConstraintName"].ToString().Trim());
                    uk.AddColumn(Convert.ToInt32(dr["FieldPosition"]), dr["FieldName"].ToString().Trim());
                    Unique tmp;
                    if (!cList.Add(uk, out tmp))
                        tmp.AddCoumns(uk.IndexOrderdColumnNames);
                }
                return cList.ToList();
            }
        }

        public override List<DBObjects.PrimaryKey> GetPrimaryKeys(string schemaName)
        {
            var stmt = String.Format(
                @"SELECT
                  rc.rdb$constraint_name as ConstraintName,
                  s.rdb$field_name as FieldName,
                  rc.rdb$relation_name as TableName,
                  s.rdb$field_position+1 as FieldPosition
                FROM
                  rdb$index_segments s
                  LEFT JOIN rdb$relation_constraints rc ON rc.rdb$index_name = s.rdb$index_name
                WHERE
                  rc.rdb$constraint_type = 'PRIMARY KEY';");
            using (var ds = this.ExecuteSelect(stmt))
            {
                var cList = new Utils.ChainedList<PrimaryKey>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var pk = new PrimaryKey(this._schemaName, dr["TableName"].ToString().Trim(), dr["ConstraintName"].ToString().Trim());
                    pk.AddColumn(Convert.ToInt32(dr["FieldPosition"]), dr["FieldName"].ToString().Trim());
                    PrimaryKey tmp;
                    if (!cList.Add(pk, out tmp))
                        tmp.AddCoumns(pk.IndexOrderdColumnNames);
                }
                return cList.ToList();
            }
        }

        public override List<DBObjects.ForeignKey> GetForeignKeys(string schemaName)
        {
            var stmt = String.Format(
                @"SELECT
                  rc.rdb$constraint_name AS FkName,
                  rcc.rdb$relation_name AS ChildTable,
                  isc.rdb$field_name AS ChildColumn,
                  isc.rdb$field_position+1 AS ChildPosition,
                  rcp.rdb$relation_name AS ParentTable,
                  isp.rdb$field_name AS ParentColumn,
                  rc.rdb$update_rule AS UpdateRule,
                  rc.rdb$delete_rule AS DeleteRule,
                  rcc.rdb$deferrable AS Deferrable,
                  rcc.rdb$initially_deferred AS InitiallyDeferred
                FROM
                  rdb$ref_constraints rc
                  INNER JOIN rdb$relation_constraints rcc on rc.rdb$constraint_name = rcc.rdb$constraint_name
                  INNER JOIN rdb$index_segments isc on rcc.rdb$index_name = isc.rdb$index_name
                  INNER JOIN rdb$relation_constraints rcp on rc.rdb$const_name_uq  = rcp.rdb$constraint_name
                  INNER JOIN rdb$index_segments isp on rcp.rdb$index_name = isp.rdb$index_name;");
            using (var ds = this.ExecuteSelect(stmt))
            {
                var cList = new Utils.ChainedList<ForeignKey>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var fk = new ForeignKey(this._schemaName, dr["ChildTable"].ToString().Trim(), dr["FkName"].ToString().Trim(), this._schemaName, dr["ParentTable"].ToString().Trim());
                    fk.DeleteRule = this.ParseDeleteRule(dr["DeleteRule"].ToString().Trim());
                    fk.UpdateRule = this.ParseUpdateRule(dr["UpdateRule"].ToString().Trim());
                    fk.InitialMode = (this.ParseBoolean(dr["InitiallyDeferred"].ToString().Trim())) ? InitialMode.InitiallyDeferred : InitialMode.InitiallyImmediate;
                    fk.Deferrability = (this.ParseBoolean(dr["Deferrable"].ToString().Trim())) ? Deferrability.Deferrable : Deferrability.NotDeferrable;
                    fk.AddColumn(Convert.ToInt32(dr["ChildPosition"]), dr["ChildColumn"].ToString().Trim(), dr["ParentColumn"].ToString().Trim());
                    ForeignKey tmp;
                    if (!cList.Add(fk, out tmp))
                        tmp.AddColumns(fk.IndexOrderdColumnNames);
                }
                return cList.ToList();
            }
        }

        public override List<DBObjects.TableCardinality> GetTableCardinalities(string schemaName)
        {
            var stmt = String.Format(
                @"SELECT
                  RDB$RELATION_NAME as TableName
                FROM
                  RDB$RELATIONS
                WHERE
                  RDB$SYSTEM_FLAG = 0 AND
                  RDB$VIEW_BLR IS NULL AND
                  (RDB$RELATION_TYPE = 0 OR RDB$RELATION_TYPE IS NULL);");

            var cntStmt = "SELECT COUNT(*) FROM \"{0}\"";

            using (var ds = this.ExecuteSelect(stmt))
            {
                var result = new List<TableCardinality>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    var tableName = dr["TableName"].ToString().Trim();
                    var cnt = (int)this.ExecuteSingleValue(String.Format(cntStmt, tableName));

                    result.Add(new TableCardinality()
                    {
                        SchemaName = this._schemaName,
                        TableName = tableName,
                        Cardinality = cnt.ToString()
                    });
                }
                return result;
            }
        }

        public override List<DBObjects.Index> GetIndices(string schemaName)
        {
            var stmt = String.Format(
                @"SELECT 
                  i.rdb$relation_name AS TableName,
                  s.rdb$field_name AS FieldName,
                  i.rdb$index_name AS IndexName,
                  rc.rdb$constraint_name AS AltIndexName,
                  i.rdb$foreign_key AS IsForeignKey,
                  i.rdb$unique_flag AS IsUnique,
                  s.rdb$field_position+1 AS FieldPosition,
                  rc.rdb$deferrable AS Deferrable,
                  rc.rdb$initially_deferred AS InitiallyDeferred
                FROM rdb$indices i
                  LEFT JOIN rdb$index_segments s ON (i.rdb$index_name = s.rdb$index_name)
                  LEFT JOIN rdb$relation_constraints rc ON (i.rdb$index_name = rc.rdb$index_name)
                WHERE 
                  i.rdb$system_flag = 0 AND
                  s.rdb$field_name IS NOT NULL");
            using (var ds = this.ExecuteSelect(stmt))
            {
                var cList = new Utils.ChainedList<Index>();
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    string index_name;
                    if (dr["AltIndexName"] == DBNull.Value)
                        index_name = dr["IndexName"].ToString().Trim();
                    else
                        index_name = dr["AltIndexName"].ToString().Trim();
                    var ix = new Index(this._schemaName, dr["TableName"].ToString().Trim(), index_name);
                    ix.IsUnique = (dr["IsUnique"] != DBNull.Value) ? true : false;
                    ix.AddColumn(Convert.ToInt32(dr["FieldPosition"]), dr["FieldName"].ToString().Trim());
                    Index tmp;
                    if (!cList.Add(ix, out tmp))
                        tmp.AddCoumns(ix.IndexOrderdColumnNames);
                }
                return cList.ToList();
            }
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

        private DataType ParseDataType(int type, int blubSubType)
        {
            var result = DataType.DEFAULT;
            if (type != 0)
            {
                switch (type)
                {
                    case 7: result = DataType.SMALLINT; break;
                    case 8: result = DataType.INTEGER; break;
                    //case 9: result = QUAD ????
                    case 10: result = DataType.FLOAT; break;
                    case 11: result = DataType.FLOAT; break; // D_FLOAT
                    case 12: result = DataType.DATE; break;
                    case 13: result = DataType.TIME; break;
                    case 14: result = DataType.CHAR; break;
                    case 16: result = DataType.NUMERIC; break; // int 64
                    case 27: result = DataType.DOUBLE; break;
                    case 35: result = DataType.TIMESTAMP; break;
                    case 37: result = DataType.VARCHAR; break;
                    case 40: result = DataType.VARCHAR; break; // C_STRING
                    case 261: result = DataType.BLOB; break;
                }
            }
            else if (blubSubType != 0)
            {
                switch (blubSubType)
                {
                    case 1: result = DataType.TEXT; break;
                    case 261: result = DataType.BINARY; break;
                }
            }
            return result;
        }

        private UpdateRule ParseUpdateRule(string rule)
        {
            var result = UpdateRule.DEFAULT;
            switch (rule.ToLower())
            {
                case "noaction": result = UpdateRule.NOACTION; break;
                case "restrict": result = UpdateRule.RESTRICT; break;
                case "cascade": result = UpdateRule.CASCADE; break;
                case "null": result = UpdateRule.NULL; break;
                case "default": result = UpdateRule.DEFAULT; break;
            }
            return result;
        }

        private DeleteRule ParseDeleteRule(string rule)
        {
            var result = DeleteRule.DEFAULT;
            switch (rule.ToLower())
            {
                case "noaction": result = DeleteRule.NOACTION; break;
                case "restrict": result = DeleteRule.RESTRICT; break;
                case "cascade": result = DeleteRule.CASCADE; break;
                case "null": result = DeleteRule.NULL; break;
                case "default": result = DeleteRule.DEFAULT; break;
            }
            return result;
        }

        private bool ParseBoolean(string value)
        {
            return (value.ToLower() == "yes");
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
