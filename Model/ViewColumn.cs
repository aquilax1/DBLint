using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;

namespace DBLint.Model
{
    public class ViewColumn : ViewColumnID
    {
        public ViewColumn(string databaseName, string schemaName, string viewName, string columnName)
            :base(databaseName, schemaName, viewName, columnName)
        {

        }

        private String _defaultValue = null;

        public Database Database { get; internal set; }
        public Schema Schema { get; internal set; }
        public View View { get; internal set; }
        
        public int OrdinalPosition { get; internal set; }
        public bool IsNullable { get; internal set; }
        public DataType DataType { get; internal set; }
        public Int64 CharacterMaxLength { get; internal set; }
        public int NumericPrecision { get; internal set; }
        public int NumericScale { get; internal set; }
        public String DefaultValue { get { return this._defaultValue; } set { this._defaultValue = value; } }
        public Privileges Privileges { get; internal set; }

        // public bool Unique { get; internal set; }

    }
}
