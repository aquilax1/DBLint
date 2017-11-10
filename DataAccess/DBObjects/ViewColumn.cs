using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;

namespace DBLint.DataAccess.DBObjects
{
    public class ViewColumn : ViewColumnID
    {
        private Privileges privileges = Privileges.None;
        private String defaultValue = null;

        public int OrdinalPosition { get; set; }
        public bool IsNullable { get; set; }
        public DataType DataType { get; set; }
        public Int64 CharacterMaxLength { get; set; }
        public int NumericPrecision { get; set; }
        public int NumericScale { get; set; }
        public String DefaultValue { get { return this.defaultValue; } set { this.defaultValue = value; } }
        public Privileges Privileges { get { return this.privileges; } set { this.privileges = value; } }
   

        public ViewColumn(string schemaName, string viewName, string columnName)
        {
            this.SchemaName = schemaName;
            this.ViewName = viewName;
            this.ColumnName = columnName;
        }
    }
}
