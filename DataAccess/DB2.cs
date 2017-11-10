using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;
using DBLint.DataAccess.DBObjects;
using IBM.Data.DB2;

namespace DBLint.DataAccess
{
    public class DB2 : Factory
    {
        public DB2(Connection connectionInfo, int timeout)
        {
            var connBuilder = new DB2ConnectionStringBuilder();

            if (!String.IsNullOrEmpty(connectionInfo.Port))
            {
                connBuilder.Server = String.Format("{0}:{1}", connectionInfo.Host, connectionInfo.Port);
                //connBuilder["Server"] = String.Format("{0}:{1}", connectionInfo.Host, connectionInfo.Port);
            }
            else
            {
                connBuilder.Server = connectionInfo.Host;
                //connBuilder["Server"] = connectionInfo.Host;
            }

            connBuilder.Database = connectionInfo.Database;
            connBuilder.UserID = connectionInfo.UserName;
            connBuilder.Password = connectionInfo.Password;
            connBuilder.MaxPoolSize = Convert.ToInt32(connectionInfo.MaxConnections);
            connBuilder.ConnectionLifeTime = timeout;

            //connBuilder["Database"] = connectionInfo.Database;
            //connBuilder["UID"] = connectionInfo.UserName;
            //connBuilder["PWD"] = connectionInfo.Password;
            //connBuilder["Max Pool Size"] = connectionInfo.MaxConnections;
            //connBuilder["Connection Lifetime"] = timeout;

            //throw new Exception(connBuilder.ToString());

            this.Setup(connBuilder.ConnectionString, DB2Factory.Instance);
        }

        public override DBMSs DBMS
        {
            get { return DBMSs.DB2; }
        }

        public override string GetFormatedSelectStatement(string schemaName, string tableName, List<string> columnNames)
        {
            return String.Format("select \"{0}\" from \"{1}\".\"{2}\"", String.Join("\", \"", columnNames), schemaName, tableName);
        }

        public override Data.DataRow GetDataRow(System.Data.Common.DbDataReader reader)
        {
            return new Data.DB2DataRow(reader);
        }

        public override List<DBObjects.Schema> GetSchemas()
        {
            var stmt = "select schemaname as schema_name from syscat.schemata";
            var ds = this.ExecuteSelect(stmt);
            var result = new List<Schema>();
            foreach (DataRow dr in ds.Tables[0].Rows)
                result.Add(new Schema(dr["schema_name"].ToString()));
            return result;
        }

        public override List<DBObjects.Table> GetTables(string schemaName)
        {
            var stmt = String.Format(
                @"select
                    tabschema as schema_name,
                    tabname as table_name
                from
                    syscat.tables
                where
                        tabschema = '{0}'
                    and type = 'T'", schemaName);
            var ds = this.ExecuteSelect(stmt);
            var result = new List<Table>();
            foreach (DataRow dr in ds.Tables[0].Rows)
                result.Add(new Table(dr["schema_name"].ToString(), dr["table_name"].ToString()));
            return result;
        }

        public override List<DBObjects.Column> GetColumns(string schemaName)
        {
            var stmt = String.Format(
                @"select
	                tabschema as schema_name,
	                tabname as table_name,
	                colname as column_name,
	                colno as ordinal_position,
	                nulls as nullable, -- Y or N
	                typename as data_type,
	                length,
	                scale,
	                default as default_value	
                from
	                syscat.columns
                where
	                tabschema = '{0}'", schemaName);
            var ds = this.ExecuteSelect(stmt);
            var result = new List<Column>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var column = new Column(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["column_name"].ToString());
                column.OrdinalPosition = Convert.ToInt32(dr["ordinal_position"]);
                column.IsNullable = (dr["nullable"].ToString().ToLower() == "y");
                column.DataType = this.ParseDataType(dr["data_type"].ToString());
                column.CharacterMaxLength = Convert.ToInt32(dr["length"]);
                column.NumericPrecision = Convert.ToInt32(dr["length"]);
                column.NumericScale = Convert.ToInt32(dr["scale"]);
                if (dr["default_value"] != DBNull.Value)
                    column.DefaultValue = dr["default_value"].ToString(); // TODO: Consider to parse this for specific system values.....
                result.Add(column);
            }
            return result;
        }

        public override List<DBObjects.Unique> GetUniqueConstraints(string schemaName)
        {
            var stmt = String.Format(
                @"select
	                inx.tabschema as schema_name,
	                inx.tabname as table_name,
	                inx.indname as index_name,
	                inxcol.colname as column_name,
	                inxcol.colseq as column_position
                from
	                syscat.indexes as inx,
	                syscat.indexcoluse inxcol
                where
	                inxcol.indschema = inx.tabschema
	                and inxcol.indname = inx.indname
	                and inx.tabschema = '{0}'
	                and inx.uniquerule = 'U'
	                order by inxcol.indname, inxcol.colseq", schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<Unique>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var uk = new Unique(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["index_name"].ToString());
                uk.AddColumn(Convert.ToInt32(dr["column_position"]), dr["column_name"].ToString());
                Unique tmp;
                if (!cList.Add(uk, out tmp))
                    tmp.AddCoumns(uk.IndexOrderdColumnNames);
            }
            return cList.ToList();
        }

        public override List<DBObjects.PrimaryKey> GetPrimaryKeys(string schemaName)
        {
            var stmt = String.Format(
                @"select
	                inx.tabschema as schema_name,
	                inx.tabname as table_name,
	                inx.indname as index_name,
	                inxcol.colname as column_name,
	                inxcol.colseq as column_position
                from
	                syscat.indexes as inx,
	                syscat.indexcoluse inxcol
                where
	                inxcol.indschema = inx.tabschema
	                and inxcol.indname = inx.indname
	                and inx.tabschema = '{0}'
	                and inx.uniquerule = 'P'
	                order by inxcol.indname, inxcol.colseq", schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<PrimaryKey>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var pk = new PrimaryKey(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["index_name"].ToString());
                pk.AddColumn(Convert.ToInt32(dr["column_position"]), dr["column_name"].ToString());
                PrimaryKey tmp;
                if (!cList.Add(pk, out tmp))
                    tmp.AddCoumns(pk.IndexOrderdColumnNames);
            }
            return cList.ToList();
        }

        public override List<DBObjects.ForeignKey> GetForeignKeys(string schemaName)
        {
            var stmt = String.Format(
                @"select
	                ccol.constname as key_name,
	                ccol.tabschema as child_schema_name,
	                ccol.tabname as child_table_name,
	                ccol.colname as child_column_name,
	                pcol.tabschema as parent_schema_name,
	                pcol.tabname as parent_table_name,
	                pcol.colname as parent_column_name,
	                ccol.colseq as ordinal_position,
	                fk.deleterule as delete_rule, -- A = NO ACTION, C = CASCADE, N = SET NULL, R = RESTRICT
	                fk.updaterule as update_rule -- A = NO ACTION, C = CASCADE, N = SET NULL, R = RESTRICT
                from
	                syscat.references as fk,
	                syscat.keycoluse as pcol, -- The parent columns
	                syscat.keycoluse as ccol  -- The child columns
                where
	                fk.tabschema = '{0}'
	                and ccol.tabschema = fk.tabschema
	                and ccol.tabname = fk.tabname
	                and ccol.constname = fk.constname
	                and pcol.tabschema = fk.tabschema  
	                and pcol.tabname = fk.reftabname
	                and pcol.constname = fk.refkeyname
	                and ccol.colseq = pcol.colseq
	                order by ccol.tabname, ccol.constname, ccol.colseq", schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<ForeignKey>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var fk = new ForeignKey(dr["child_schema_name"].ToString(), dr["child_table_name"].ToString(), dr["key_name"].ToString(),
                    dr["parent_schema_name"].ToString(), dr["parent_table_name"].ToString());
                fk.UpdateRule = this.ParseUpdateRule(dr["update_rule"].ToString());
                fk.DeleteRule = this.ParseDeleteRule(dr["delete_rule"].ToString());
                fk.AddColumn(Convert.ToInt32(dr["ordinal_position"]), dr["child_column_name"].ToString(), dr["parent_column_name"].ToString());
                ForeignKey tmp;
                if (!cList.Add(fk, out tmp))
                    tmp.AddColumns(fk.IndexOrderdColumnNames);
            }
            return cList.ToList();
        }

        public override List<DBObjects.TableCardinality> GetTableCardinalities(string schemaName)
        {
            // NOTE: that card can be -1 if stats are not up-to-date (e.i. never run)
            var stmt = String.Format(
                @"select
                    tabschema as schema_name,
                    tabname as table_name,
                    card as row_count
                from
                    syscat.tables
                where
                        tabschema = '{0}'
                    and type = 'T'", schemaName);
            var ds = this.ExecuteSelect(stmt);
            var result = new List<TableCardinality>();
            foreach (DataRow dr in ds.Tables[0].Rows)
                result.Add(new TableCardinality() { SchemaName = dr["schema_name"].ToString(), TableName = dr["table_name"].ToString(), Cardinality = dr["row_count"].ToString() });
            return result;
        }

        public override List<DBObjects.Index> GetIndices(string schemaName)
        {
            var stmt = String.Format(
                @"select
	                inx.tabschema as schema_name,
	                inx.tabname as table_name,
	                inx.indname as index_name,
	                inxcol.colname as column_name,
	                inxcol.colseq as column_position,
	                case (inx.uniquerule)
	                	when 'P' then 'Y'
	                	when 'U' then 'Y'
	                	when 'D' then 'N'
	                end as is_unique
                from
	                syscat.indexes as inx,
	                syscat.indexcoluse inxcol
                where
	                inxcol.indschema = inx.tabschema
	                and inxcol.indname = inx.indname
	                and inx.tabschema = '{0}'
	                and inx.indextype in ('BLOK', 'CLUS', 'DIM', 'REG')
	                order by inxcol.indname, inxcol.colseq", schemaName);
            var ds = this.ExecuteSelect(stmt);
            var cList = new Utils.ChainedList<Index>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var index = new Index(dr["schema_name"].ToString(), dr["table_name"].ToString(), dr["index_name"].ToString());
                index.IsUnique = dr["is_unique"].ToString().ToLower() == "y";
                index.AddColumn(Convert.ToInt32(dr["column_position"]), dr["column_name"].ToString());
                Index tmp;
                if (!cList.Add(index, out tmp))
                    tmp.AddCoumns(index.IndexOrderdColumnNames);
            }
            return cList.ToList();
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

        private DataType ParseDataType(string type)
        {
            var result = DataType.DEFAULT;
            switch (type.ToLower())
            {
                case "array": result = DataType.ARRAY; break;
                case "bigint": result = DataType.BIGINT; break;
                case "binary": result = DataType.BINARY; break;
                case "blob": result = DataType.BLOB; break;
                case "boolean": result = DataType.BOOLEAN; break;
                case "character": result = DataType.CHAR; break;
                case "clob": result = DataType.CLOB; break;
                //case "cursor": result = DataType; break;
                case "date": result = DataType.DATE; break;
                //case "dbclob": result = DataType; break;
                case "decfloat": result = DataType.FLOAT; break;
                case "decimal": result = DataType.DECIMAL; break;
                case "double": result = DataType.DOUBLE; break;
                //case "graphic": result = DataType; break; // This is possible just a BLOB
                case "integer": result = DataType.INTEGER; break;
                case "long varchar": result = DataType.TEXT; break; // What is a long varchar?
                //case "long vargraphic": result = DataType; break;
                case "real": result = DataType.REAL; break;
                //case "reference": result = DataType; break;
                //case "row": result = DataType; break;
                case "smallint": result = DataType.SMALLINT; break;
                case "time": result = DataType.TIME; break;
                case "timestamp": result = DataType.TIMESTAMP; break;
                case "varbinary": result = DataType.VARBINARY; break;
                case "varchar": result = DataType.VARCHAR; break;
                //case "vargraphic": result = DataType; break;
                case "xml": result = DataType.XML; break;

            }
            return result;
        }

        private UpdateRule ParseUpdateRule(string rule)
        {
            var result = UpdateRule.DEFAULT;
            switch(rule.ToLower())
            {
                case "a": result = UpdateRule.NOACTION; break;
                case "c": result = UpdateRule.CASCADE; break;
                case "n": result = UpdateRule.NULL; break;
                case "r": result = UpdateRule.RESTRICT; break;
            }
            return result;
        }
        
        private DeleteRule ParseDeleteRule(string rule)
        {
            var result = DeleteRule.DEFAULT;
            switch (rule.ToLower())
            {
                case "a": result = DeleteRule.NOACTION; break;
                case "c": result = DeleteRule.CASCADE; break;
                case "n": result = DeleteRule.NULL; break;
                case "r": result = DeleteRule.RESTRICT; break;
            }
            return result;
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
