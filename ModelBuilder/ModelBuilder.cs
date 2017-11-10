using System;
using System.Collections.Generic;
using System.Linq;
using DBLint.DataAccess;
using DBLint.Model;
using DBLint.Data;
using System.IO;
using DBLint.DataTypes;

namespace DBLint.ModelBuilder
{
    public static class ModelBuilder
    {

        public static Database DatabaseFactory(Extractor extractor, IEnumerable<DBLint.DataAccess.DBObjects.Schema> selectedSchemas, IEnumerable<DBLint.DataAccess.DBObjects.TableID> ignoredTables)
        {
            var database = new Database(extractor.DatabaseName, extractor.Database);
            var ignoredDictionary = DictionaryFactory.CreateTableID<TableID>();
            foreach (var ignoredTable in ignoredTables)
            {
                var tblid = new TableID(extractor.DatabaseName, ignoredTable.SchemaName, ignoredTable.TableName);
                ignoredDictionary.Add(tblid, tblid);
            }

            using (var db = database)
            {
                var dbSchemas = extractor.Database.GetSchemas();
                foreach (var dbSchema in dbSchemas)
                {
                    using (var schema = new Schema(db.DatabaseName, dbSchema.SchemaName))
                    {
                        if (!selectedSchemas.Any(p => p.Equals(dbSchema)))
                            continue;
                        schema.Database = db;
                        db._schemas.Add(schema);

                        #region Table and table columns

                        var dbTables = extractor.Database.GetTables(schema.SchemaName);
                        var tables = from dbTable in dbTables
                                        orderby dbTable.TableName
                                        select dbTable;

                        foreach (var dbTable in tables)
                        {
                            var table = new DataTable(db.DatabaseName, schema.SchemaName, dbTable.TableName, extractor.Database);

                            if (ignoredDictionary.ContainsKey(table))
                            {
                                database.IgnoredTables.Add(table);
                                continue;
                            }

                            schema._tables.Add(table);
                            db.tableDictionary.Add(table, table);
                            table.Database = db;
                            table.Schema = schema;
                        }

                        var dbColumns = extractor.Database.GetColumns(schema.SchemaName);

                        var columnID = new ColumnID(extractor.DatabaseName, schema.SchemaName, null, null);
                        foreach (var dbColumn in dbColumns)
                        {
                            columnID.TableName = dbColumn.TableName;
                            columnID.ColumnName = dbColumn.ColumnName;
                            if (db.tableDictionary.ContainsKey(columnID))
                            {
                                var column = new Column(db.DatabaseName, schema.SchemaName, dbColumn.TableName, dbColumn.ColumnName);
                                var table = db.tableDictionary[column];
                                table._columns.Add(column);
                                column.DataType = dbColumn.DataType;
                                column.DefaultValue = dbColumn.DefaultValue;
                                column.OrdinalPosition = dbColumn.OrdinalPosition;
                                column.CharacterMaxLength = dbColumn.CharacterMaxLength;
                                column.IsNullable = dbColumn.IsNullable;
                                column.NumericPrecision = dbColumn.NumericPrecision;
                                column.NumericScale = dbColumn.NumericScale;
                                column.Table = table;
                                column.Schema = schema;
                                column.Database = db;
                                column.IsSequence = dbColumn.IsSequence;
                                column.IsDefaultValueAFunction = dbColumn.DefaultIsFunction;
                            }
                        }

                        #endregion

                        #region Views and view columns

                        var dbViews = extractor.Database.GetViews(schema.SchemaName);
                        var views = from dbView in dbViews
                                    orderby dbView.ViewName
                                    select dbView;

                        foreach (var dbView in views)
                        {
                            var view = new View(db.DatabaseName, schema.SchemaName, dbView.ViewName);

                            schema._views.Add(view);
                            db.viewDictionary.Add(view, view);
                            view.Database = db;
                            view.Schema = schema;
                        }

                        var dbViewColumns = extractor.Database.GetViewColumns(schema.SchemaName);
                        var viewColumnID = new ViewColumnID(extractor.DatabaseName, schema.SchemaName, null, null);

                        foreach (var dbViewColumn in dbViewColumns)
                        {
                            viewColumnID.ViewName = dbViewColumn.ViewName;
                            viewColumnID.ColumnName = dbViewColumn.ColumnName;

                            if (db.viewDictionary.ContainsKey(viewColumnID))
                            {
                                var viewColumn = new ViewColumn(db.DatabaseName, schema.SchemaName, dbViewColumn.ViewName, dbViewColumn.ColumnName);
                                var view = db.viewDictionary[viewColumn];
                                view._columns.Add(viewColumn);
                                viewColumn.Database = db;
                                viewColumn.Schema = schema;
                                viewColumn.View = view;
                                viewColumn.DataType = dbViewColumn.DataType;
                                viewColumn.DefaultValue = dbViewColumn.DefaultValue;
                                viewColumn.OrdinalPosition = dbViewColumn.OrdinalPosition;
                                viewColumn.CharacterMaxLength = dbViewColumn.CharacterMaxLength;
                                viewColumn.IsNullable = dbViewColumn.IsNullable;
                                viewColumn.NumericPrecision = dbViewColumn.NumericPrecision;
                                viewColumn.NumericScale = dbViewColumn.NumericScale;
                                viewColumn.Privileges = dbViewColumn.Privileges;
                            }
                        }

                        #endregion

                        // Adding functions
                        var functions = extractor.Database.GetFunctions(schema.SchemaName);
                        foreach (var function in functions)
                        {
                            var f = new Function(
                                database.DatabaseName,
                                function.SchemaName,
                                function.RoutineName);
                            f.Database = database;
                            f.Schema = schema;

                            schema._functions.Add(f);
                        }

                        var fParameters = extractor.Database.GetFunctionsParameters(schema.SchemaName);
                        foreach (var fParameter in fParameters)
                        {
                            var fp = new Parameter(database.DatabaseName, fParameter.SchemaName, fParameter.RoutineName, fParameter.ParameterName);
                            fp.Database = schema.Database;
                            fp.Schema = schema;

                            fp.DataType = fParameter.DataType;
                            fp.Direction = fParameter.Direction;
                            fp.CharacterMaxLength = fParameter.CharacterMaxLength;
                            fp.NumericPrecision = fParameter.NumericPrecision;
                            fp.NumericScale = fParameter.NumericScale;
                            fp.OrdinalPosition = fParameter.OrdinalPosition;

                            var tmpF = schema._functions.Where(f => f.FunctionName.Equals(fp.RoutineName)).FirstOrDefault();
                            if (tmpF != null)
                            {
                                fp.Routine = tmpF;
                                tmpF._parameters.Add(fp);
                            }
                        }

                        // Adding stored procedures
                        var storedProcedures = extractor.Database.GetStoredProcedures(schema.SchemaName);
                        foreach (var storedProcedure in storedProcedures)
                        {
                            var sp = new StoredProcedure(
                                database.DatabaseName,
                                storedProcedure.SchemaName,
                                storedProcedure.RoutineName);

                            sp.Database = database;
                            sp.Schema = schema;

                            schema._storedProcedures.Add(sp);
                        }

                        var parameters = extractor.Database.GetStoredProceduresParameters(schema.SchemaName);
                        foreach (var parameter in parameters)
                        {
                            var p = new Parameter(database.DatabaseName, parameter.SchemaName, parameter.RoutineName, parameter.ParameterName);
                            p.Database = schema.Database;
                            p.Schema = schema;

                            p.DataType = parameter.DataType;
                            p.Direction = parameter.Direction;
                            p.CharacterMaxLength = parameter.CharacterMaxLength;
                            p.NumericPrecision = parameter.NumericPrecision;
                            p.NumericScale = parameter.NumericScale;
                            p.OrdinalPosition = parameter.OrdinalPosition;

                            var tmpSp = schema._storedProcedures.Where(sp => sp.StoredProcedureName.Equals(p.RoutineName)).FirstOrDefault();
                            if (tmpSp != null)
                            {
                                p.Routine = tmpSp;
                                tmpSp._parameters.Add(p);
                            }
                        }
                    }
                }
            }
            AddForeignKeys(extractor, database);
            AddPrimaryKeys(extractor, database);
            AddIndices(extractor, database);
            AddUniqueConstraints(extractor, database);

            foreach (var tbl in database.Tables)
                tbl.Dispose();

            foreach (var view in database.Views)
                view.Dispose();

            AddCardinalities(database.tableDictionary, extractor, selectedSchemas);

            database.Escaper = Escaper.GetEscaper(extractor.Database.DBMS);
            database.DBMS = extractor.Database.DBMS;
            return database;
        }

        private static void AddCardinalities(DatabaseDictionary<TableID, Table> tableDictionary, Extractor extractor, IEnumerable<DataAccess.DBObjects.Schema> selectedSchemas)
        {
            TableID tid = new TableID(extractor.DatabaseName, null, null);

            foreach (var schema in selectedSchemas)
            {
                var cardinalities = extractor.Database.GetTableCardinalities(schema.SchemaName);
                foreach (var cardinality in cardinalities)
                {
                    tid.SchemaName = cardinality.SchemaName;
                    tid.TableName = cardinality.TableName;
                    var scount = cardinality.Cardinality;
                    if (string.IsNullOrEmpty(scount) || !tableDictionary.ContainsKey(tid))
                        continue;
                    var count = int.Parse(scount);
                    var tbl = tableDictionary[tid];
                    tbl.Cardinality = count;
                }
            }
        }

        private static void AddUniqueConstraints(Extractor extractor, Database database)
        {
            foreach (var schema in database.Schemas)
            {
                var tableID = new TableID(schema.DatabaseName, schema.SchemaName, null);
                var uniqueConstraints = extractor.Database.GetUniqueConstraints(schema.SchemaName);
                foreach (var uniqueConstraint in uniqueConstraints)
                {
                    tableID.TableName = uniqueConstraint.TableName;
                    if (!database.tableDictionary.ContainsKey(tableID))
                        continue;

                    var table = database.tableDictionary[tableID];
                    using (var uc = new UniqueConstraint(table))
                    {
                        var kname = uniqueConstraint.KeyName;
                        uc.ConstraintName = kname;
                        var columns = from colname in uniqueConstraint.IndexOrderdColumnNames
                                      orderby colname.Key
                                      select colname.Value;
                        
                        var addConstraint = true;
                        foreach (var colname in columns)
                        {
                            var column = table.Columns.FirstOrDefault(col => col.ColumnName == colname);
                            if (column == null)
                            {
                                addConstraint = false;
                                break;
                            }
                            uc._columns.Add(column);
                            if (!uniqueConstraint.IsMultiColumn)
                                column.Unique = true;
                        }
                        if (addConstraint)
                            table._uniqueConstraints.Add(uc);
                    }
                }
            }
        }

        private static void AddIndices(Extractor extractor, Database database)
        {
            foreach (var schema in database.Schemas)
            {
                var tableID = new TableID(schema.DatabaseName, schema.SchemaName, null);
                var indices = extractor.Database.GetIndices(schema.SchemaName);
                foreach (var index in indices)
                {
                    tableID.TableName = index.TableName;
                    if (!database.tableDictionary.ContainsKey(tableID))
                        continue;

                    var table = database.tableDictionary[tableID];
                    using (var ix = new Index(table, index.IndexName))
                    {
                        var columns = from colname in index.IndexOrderdColumnNames
                                      orderby colname.Key
                                      select colname.Value;
                        var addIndex = true;
                        foreach (var colname in columns)
                        {
                            var column = table.Columns.FirstOrDefault(col => col.ColumnName == colname);
                            if (column == null)
                            {
                                addIndex = false;
                                break;
                            }
                            ix._columns.Add(column);
                        }
                        ix.IsUnique = index.IsUnique;
                        if (addIndex)
                            table._indices.Add(ix);
                    }
                }
            }
        }

        private static void AddPrimaryKeys(Extractor extractor, Database database)
        {
            foreach (var schema in database.Schemas)
            {
                var tableID = new TableID(database.DatabaseName, schema.SchemaName, null);
                foreach (var pk in extractor.Database.GetPrimaryKeys(schema.SchemaName))
                {
                    tableID.TableName = pk.TableName;
                    if (!database.tableDictionary.ContainsKey(tableID))
                        continue;
                    var table = database.tableDictionary[tableID];
                    using (var primaryKey = new PrimaryKey(table, pk.KeyName))
                    {
                        var columns = from c in pk.IndexOrderdColumnNames
                                      orderby c.Key
                                      select table.Columns.First(col => col.ColumnName == c.Value);
                        foreach (var column in columns)
                            primaryKey._columns.Add(column);
                        primaryKey.Table = table;
                        primaryKey.Schema = schema;
                        primaryKey.Database = database;
                        table.PrimaryKey = primaryKey;
                        if (!primaryKey.IsMulticolumn)
                            primaryKey.Columns.First().Unique = true;
                    }
                }
            }
        }

        
    private static void AddForeignKeys(Extractor extractor, Database db)
    {
            foreach (var schema in db.Schemas)
            {
                var foreignKeys = extractor.Database.GetForeignKeys(schema.SchemaName);
                var pktableID = new TableID(extractor.DatabaseName, null, null);
                var fktableID = new TableID(extractor.DatabaseName, null, null);

                foreach (var foreignKey in foreignKeys)
                {
                    pktableID.SchemaName = foreignKey.PrimaryKeySchemaName;
                    pktableID.TableName = foreignKey.PrimaryKeyTableName;

                    fktableID.SchemaName = foreignKey.SchemaName;
                    fktableID.TableName = foreignKey.TableName;

                    if (!(db.tableDictionary.ContainsKey(pktableID) && db.tableDictionary.ContainsKey(fktableID)))
                        continue;

                    var pktable = db.tableDictionary[pktableID];
                    var fktable = db.tableDictionary[fktableID];

                    pktable._referencedBy.Add(fktable);
                    fktable._references.Add(pktable);

                    using (var fk = new ForeignKey(db.DatabaseName, schema.SchemaName, foreignKey.ForeignKeyName))
                    {
                        fk.Schema = schema;
                        fk.Database = db;
                        fk.PKTable = pktable;
                        fk.FKTable = fktable;
                        fk.UpdateRule = foreignKey.UpdateRule;
                        fk.DeleteRule = foreignKey.DeleteRule;
                        fk.Deferrability = foreignKey.Deferrability;
                        fk.InitialMode = foreignKey.InitialMode;
                        var addFk = true;
                        foreach (var colpair in foreignKey.IndexOrderdColumnNames)
                        {
                            var pkcolumn = pktable.Columns.FirstOrDefault(c => c.ColumnName == colpair.Value.PrimaryKeyColumn);
                            var fkcolumn = fktable.Columns.FirstOrDefault(c => c.ColumnName == colpair.Value.ForeignKeyColumn);
                            if (fkcolumn == null || pkcolumn == null)
                            {
                                addFk = false;
                                break;
                            }
                            var fkpair = new ForeignKeyPair(fkcolumn, pkcolumn, colpair.Key);
                            fk._columnPairs.Add(fkpair);
                        }
                        if (addFk)
                            fktable._foreignKeys.Add(fk);
                    }
                }
            }
        }
    }
}
