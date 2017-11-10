using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;

namespace DBLint.DataAccess.DBObjects
{
    public class Parameter : ParameterID
    {
        public DataType DataType { get; set; }
        public ParameterDirection Direction { get; set; }
        public int OrdinalPosition { get; set; }
        public Int64 CharacterMaxLength { get; set; }
        public int NumericPrecision { get; set; }
        public int NumericScale { get; set; }
    }
}
