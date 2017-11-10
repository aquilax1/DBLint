using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;

namespace DBLint.DataAccess.DBObjects
{
    public class Column : ColumnID
    {
        private bool isSequence = false;
        private bool defaultIsFunction = false;
        private String defaultValue = null;

        public int OrdinalPosition { get; set; }
        public bool IsNullable { get; set; }
        public DataType DataType { get; set; }
        public Int64 CharacterMaxLength { get; set; }
        public int NumericPrecision { get; set; }
        public int NumericScale { get; set; }
        public String DefaultValue { get { return this.defaultValue; } set { this.defaultValue = value; } }
        public bool IsSequence
        {
            get { return this.isSequence; }
            set { this.isSequence = value; }
        }
        public bool DefaultIsFunction
        {
            get { return this.defaultIsFunction; }
            set { this.defaultIsFunction = value; }
        }

        public Column() { }

        public Column(string schemaName, string tableName, string columnName)
            : base()
        {
            this.SchemaName = schemaName;
            this.TableName = tableName;
            this.ColumnName = columnName;
        }
    }
}
