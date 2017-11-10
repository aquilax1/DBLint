using System;
using System.Collections.Generic;

namespace DBLint.DataTypes
{
    public static class DataTypesLists
    {
        private static readonly List<DataType> numeric = new List<DataType>()
        {
             DataType.TINYINT,
            DataType.SMALLINT,
            DataType.INTEGER,
            DataType.BIGINT,
            DataType.FLOAT,
            DataType.REAL,
            DataType.DOUBLE,
            DataType.NUMERIC,
            DataType.DECIMAL,
            DataType.REAL,
            DataType.NUMBER
        };

        private static readonly List<DataType> text = new List<DataType>()
        {
            DataType.LONGNVARCHAR,
            DataType.LONGVARCHAR,
            DataType.NCHAR,
            DataType.NVARCHAR,
            DataType.VARCHAR,
            DataType.CHAR,
            DataType.TEXT
        };

        private static readonly List<DataType> date = new List<DataType>()
        {
            DataType.DATE,
            DataType.DATETIME,
            DataType.TIME,
            DataType.TIMESTAMP
        };

        public static List<DataType> NumericTypes()
        {
            return numeric;
        }

        public static List<DataType> TextTypes()
        {
            return text;
        }

        public static List<DataType> DateTypes()
        {
            return date;
        }
    }

    public enum DataType
    {
        BIT,
        TINYINT,
        SMALLINT,
        INTEGER,
        BIGINT,
        FLOAT,
        REAL,
        DOUBLE,
        NUMERIC,
        NUMBER,
        DECIMAL,
        CHAR,
        VARCHAR,
        LONGVARCHAR,
        TEXT,
        DATE,
        DATETIME,
        TIME,
        TIMESTAMP,
        BINARY,
        VARBINARY,
        LONGVARBINARY,
        NULL,
        STRUCT,
        ARRAY,
        BLOB,
        CLOB,
        REF,
        DATALINK,
        BOOLEAN,
        ROWID,
        NCHAR,
        NVARCHAR,
        LONGNVARCHAR,
        NCLOB,
        XML,
        ENUM,
        IMAGE,
        DEFAULT
    }

    public enum UpdateRule
    {
        DEFAULT,
        NOACTION,
        CASCADE,
        RESTRICT,
        NULL
    }

    public enum DeleteRule
    {
        DEFAULT,
        NOACTION,
        CASCADE,
        RESTRICT,
        NULL
    }

    public enum Deferrability
    {
        Deferrable,
        NotDeferrable,
        Unknown
    }

    public enum InitialMode
    {
        InitiallyImmediate,
        InitiallyDeferred,
        Unknown
    }

    public enum ParameterDirection
    {
        Input,
        Output,
        InputOutput,
        Unknown
    }

    [Flags]
    public enum Privileges
    {
        None = 0x0,
        Select = 0x2,
        Insert = 0x4,
        Update = 0x8,
        References = 0x16
    }
}
