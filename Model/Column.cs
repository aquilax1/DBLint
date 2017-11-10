using System;
using DBLint.DataTypes;

namespace DBLint.Model
{
    public class Column : ColumnID
    {
        public Column(string databaseName, string schemaName, string tableName, string columnName)
            : base(databaseName, schemaName, tableName, columnName)
        {
        }

        private String _defaultValue = null;

        public Schema Schema { get; internal set; }
        public Table Table { get; internal set; }
        public Database Database { get; internal set; }

        public int OrdinalPosition { get; internal set; }
        public bool IsNullable { get; internal set; }
        public DataType DataType { get; internal set; }
        public Int64 CharacterMaxLength { get; internal set; }
        public int NumericPrecision { get; internal set; }
        public int NumericScale { get; internal set; }
        public String DefaultValue { get { return this._defaultValue; } set { this._defaultValue = value; } }
        public bool IsSequence { get; internal set; }
        public bool IsDefaultValueAFunction { get; internal set; }

        public bool Unique { get; internal set; }

    }
}
