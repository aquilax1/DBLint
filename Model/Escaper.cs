using System;

namespace DBLint.Model
{
    public interface IEscaper
    {
        string Escape(TableID tableID);
        string Escape(ColumnID tableID);
        string Log10Function(string parameter);
        string RoundFunction(string parameter, int precision);
    }

    public class Escaper
    {
        public static IEscaper GetEscaper(DataAccess.DBMSs dbms)
        {
            switch (dbms)
            {
                case DataAccess.DBMSs.MSSQL:
                    return MSSQLEscaper.Default;
                case DataAccess.DBMSs.MYSQL:
                    return MySQLEscaper.Default;
                case DataAccess.DBMSs.POSTGRESQL:
                    return PostgreSQLEscaper.Default;
                case DataAccess.DBMSs.ORACLE:
                    return OracleSQLEscaper.Default;
                case DataAccess.DBMSs.FIREBIRD:
                    return FirebirdEscaper.Default;
                case DataAccess.DBMSs.DB2:
                    return DB2Escaper.Default;
            }
            return null;
        }

        private class MySQLEscaper : IEscaper
        {
            private MySQLEscaper() { }
            public static IEscaper Default = new MySQLEscaper();
            public string Escape(TableID tableID)
            {
                return string.Format("`{0}`.`{1}`", tableID.SchemaName, tableID.TableName);
            }

            public string Escape(ColumnID columnID)
            {
                return string.Format("`{1}`", columnID.TableName, columnID.ColumnName);
            }

            public string Log10Function(string parameter)
            {
                return "log10(" + parameter + ")";
            }

            public string RoundFunction(string parameter, int precision)
            {
                return string.Format("round({0}, {1})", parameter, precision);
            }
        }

        private class FirebirdEscaper : IEscaper
        {
            private FirebirdEscaper() { }
            public static IEscaper Default = new FirebirdEscaper();
            public string Escape(TableID tableID)
            {
                return string.Format("{0}", tableID.TableName);
            }

            public string Escape(ColumnID columnID)
            {
                return string.Format("{0}", columnID.ColumnName);
            }

            public string Log10Function(string parameter)
            {
                return "log10(" + parameter + ")";
            }

            public string RoundFunction(string parameter, int precision)
            {
                return parameter;
            }
        }

        private class DB2Escaper : IEscaper
        {
            private DB2Escaper() { }
            public static IEscaper Default = new DB2Escaper();
            public string Escape(TableID tableID)
            {
                return string.Format("\"{0}\"", tableID.TableName);
            }

            public string Escape(ColumnID columnID)
            {
                return string.Format("\"{0}\"", columnID.ColumnName);
            }

            public string Log10Function(string parameter)
            {
                return "log10(" + parameter + ")";
            }

            public string RoundFunction(string parameter, int precision)
            {
                return parameter;
            }
        }

        private class MSSQLEscaper : IEscaper
        {
            private MSSQLEscaper() { }
            public static IEscaper Default = new MSSQLEscaper();
            public string Escape(TableID tableID)
            {
                return string.Format("[{0}].[{1}]", tableID.SchemaName, tableID.TableName);
            }

            public string Escape(ColumnID columnID)
            {
                return string.Format("[{1}]", columnID.TableName, columnID.ColumnName);
            }
            public string Log10Function(string parameter)
            {
                return "log10(" + parameter + ")";
            }

            public string RoundFunction(string parameter, int precision)
            {
                return string.Format("round({0}, {1})", parameter, precision);
            }
        }

        private class PostgreSQLEscaper : IEscaper
        {
            private PostgreSQLEscaper() { }
            public static IEscaper Default = new PostgreSQLEscaper();
            public string Escape(TableID tableID)
            {
                return string.Format("\"{1}\"", tableID.SchemaName, tableID.TableName);
            }

            public string Escape(ColumnID columnID)
            {
                return string.Format("\"{1}\"", columnID.TableName, columnID.ColumnName);
            }

            public string Log10Function(string parameter)
            {
                return "log(" + parameter + ")";
            }

            public string RoundFunction(string parameter, int precision)
            {
                return string.Format("round(cast({0} as numeric), {1})", parameter, precision);
            }
        }

        private class OracleSQLEscaper : IEscaper
        {
            private OracleSQLEscaper() { }
            public static IEscaper Default = new OracleSQLEscaper();
            public string Escape(TableID tableID)
            {
                return string.Format("\"{0}\"", tableID.TableName);
            }

            public string Escape(ColumnID columnID)
            {
                return string.Format("\"{0}\"", columnID.ColumnName);
            }

            public string Log10Function(string parameter)
            {
                return string.Format("LOG(10, {0})", parameter);
            }

            public string RoundFunction(string parameter, int precision)
            {
                return string.Format("round({0}, {1})", parameter, precision);
            }
        }
    }
}