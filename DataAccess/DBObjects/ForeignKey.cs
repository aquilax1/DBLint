using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;

namespace DBLint.DataAccess.DBObjects
{
    public class ForeignKeyPair
    {
        public string ForeignKeyColumn { get; set; }
        public string PrimaryKeyColumn { get; set; }

        public ForeignKeyPair() { }

        public ForeignKeyPair(string foreignKeyColumn, string primaryKeyColumn)
            : this()
        {
            this.ForeignKeyColumn = foreignKeyColumn;
            this.PrimaryKeyColumn = primaryKeyColumn;
        }
    }

    public class ForeignKey : ForeignKeyID
    {
        private Deferrability _deferrability = Deferrability.Unknown;
        private InitialMode _initialMode = InitialMode.Unknown;

        public string PrimaryKeySchemaName { get; set; }
        public string PrimaryKeyTableName { get; set; }
        public Dictionary<int, ForeignKeyPair> IndexOrderdColumnNames { get; set; }
        public UpdateRule UpdateRule { get; set; }
        public DeleteRule DeleteRule { get; set; }
        public Deferrability Deferrability
        {
            get { return this._deferrability; }
            set { this._deferrability = value; }
        }
        public InitialMode InitialMode
        {
            get { return this._initialMode; }
            set { this._initialMode = value; }
        }

        public bool IsMultiColumn
        {
            get { return this.IndexOrderdColumnNames.Count > 1; }
        }

        public ForeignKey()
        {
            this.IndexOrderdColumnNames = new Dictionary<int, ForeignKeyPair>();
        }

        public ForeignKey(string schemaName, string tableName, string foreignKeyName)
            : this()
        {
            this.SchemaName = schemaName;
            this.TableName = tableName;
            this.ForeignKeyName = foreignKeyName;
        }

        public ForeignKey(string schemaName, string tableName, string foreignKeyName, string primaryKeySchemaName, string primaryKeyTableName)
            : this(schemaName, tableName, foreignKeyName)
        {
            this.PrimaryKeySchemaName = primaryKeySchemaName;
            this.PrimaryKeyTableName = primaryKeyTableName;
        }

        public void AddColumn(int position, string foreignKeyColumnName, string primaryKeyColumnName)
        {
            this.IndexOrderdColumnNames.Add(position, new ForeignKeyPair(foreignKeyColumnName, primaryKeyColumnName));
        }

        public void AddColumns(Dictionary<int, ForeignKeyPair> columns)
        {
            foreach (var pair in columns)
            {
                this.IndexOrderdColumnNames.Add(pair.Key, pair.Value);
            }
        }
    }
}
