using System;
using System.Linq;
using DBLint.DataAccess;
using DBLint.Model;

namespace DBLint.ModelBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var extractor = new DBLint.DataAccess.Extractor(new Connection { Database = "magento", DBMS = DBMSs.MYSQL, Host = "172.25.11.18", Password = "!Dr01123622", Port = "3306", UserName = "root" });
            var selectedSchemas = extractor.Database.GetSchemas();

            var ignoredTables = new DataAccess.DBObjects.Table[] { };

            var db = DBLint.ModelBuilder.ModelBuilder.DatabaseFactory(extractor, selectedSchemas, ignoredTables);

            //PrintDatabase(db);
        }

        private static void PrintDatabase(Database db)
        {
            foreach (var schema in db.Schemas)
            {
                Console.WriteLine(schema.SchemaName);
                foreach (var table in schema.Tables)
                {
                    Console.WriteLine("\t{0}", table.TableName);
                    foreach (var column in table.Columns)
                    {
                        Console.WriteLine("\t\t{0}", column.ColumnName);
                    }
                }
            }
        }

    }
}
