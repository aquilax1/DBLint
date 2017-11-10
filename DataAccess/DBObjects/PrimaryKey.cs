using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.DataAccess.DBObjects
{
    public class PrimaryKey : PrimaryKeyID
    {
        public Dictionary<int, string> IndexOrderdColumnNames { get; set; }

        public bool IsMultiColumn
        {
            get { return this.IndexOrderdColumnNames.Count > 1; }
        }

        public PrimaryKey()
        {
            this.IndexOrderdColumnNames = new Dictionary<int, string>();
        }

        public PrimaryKey(string schemaName, string tableName, string keyName)
            : this()
        {
            this.SchemaName = schemaName;
            this.TableName = tableName;
            this.KeyName = keyName;
        }

        public void AddColumn(int position, string columnName)
        {
            this.IndexOrderdColumnNames.Add(position, columnName);
        }

        public void AddCoumns(Dictionary<int, string> columns)
        {
            foreach (var pair in columns)
            {
                this.IndexOrderdColumnNames.Add(pair.Key, pair.Value);
            }
        }
    }
}
