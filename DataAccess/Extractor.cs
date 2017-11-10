using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace DBLint.DataAccess
{
    public class DisplayNameAttribute : Attribute
    {
        public readonly string Name;
        public DisplayNameAttribute(string name)
        {
            Name = name;
        }
    }
    
    public enum DBMSs
    {
        [DisplayName("None")]
        NONE,
        [DisplayName("PostgreSQL")]
        POSTGRESQL,
        [DisplayName("MS SQL")]
        MSSQL,
        [DisplayName("MySQL")]
        MYSQL,
        [DisplayName("Oracle")]
        ORACLE,
        [DisplayName("Firebird")]
        FIREBIRD,
        [DisplayName("DB2")]
        DB2
    }

    public class Extractor
    {
        Factory dataAccess;
        public string DatabaseName;
        
        public Factory Database
        {
            get { return this.dataAccess; }
        }

        public Extractor(Connection connection)
        {
            this.DatabaseName = connection.Database;
            var timeout = 60;
            switch (connection.DBMS)
            {
                case DBMSs.NONE:
                case DBMSs.POSTGRESQL:
                    dataAccess = new PostgreSQL(connection, timeout);
                    break;
                case DBMSs.MSSQL:
                    dataAccess = new MSSQL(connection, timeout);
                    break;
                case DBMSs.MYSQL:
                    dataAccess = new MySQL(connection, timeout);
                    break;
                case DBMSs.ORACLE:
                    dataAccess = new Oracle(connection, timeout);
                    break;
                case DBMSs.FIREBIRD:
                    dataAccess = new Firebird(connection, timeout);
                    break;
                case DBMSs.DB2:
                    dataAccess = new DB2(connection, timeout);
                    break;
            }
        }
    }
}
