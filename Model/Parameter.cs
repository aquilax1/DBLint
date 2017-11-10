using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;

namespace DBLint.Model
{
    public class Parameter : ParameterID
    {
        public Parameter(string databaseName, string schemaName, string routineName, string parameterName)
            : base(databaseName, schemaName, routineName, parameterName)
        {

        }

        public Database Database { get; internal set; }
        public Schema Schema { get; internal set; }
        public RoutineID Routine { get; internal set; }

        public int OrdinalPosition { get; internal set; }
        public DataType DataType { get; internal set; }
        public ParameterDirection Direction { get; internal set; }
        public Int64 CharacterMaxLength { get; internal set; }
        public int NumericPrecision { get; internal set; }
        public int NumericScale { get; internal set; }
    }
}
