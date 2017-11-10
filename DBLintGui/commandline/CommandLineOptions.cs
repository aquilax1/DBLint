using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace DBLint.DBLintGui
{
    class CommandLineOptions
    {
        [Option('c', "config", Required = false, HelpText = "Path to config file")]
        public String ConfigFile { get; set; }

        [OptionArray('t', "include-tables", HelpText = "List of tables to check. Wildcards * are allowed")]
        public string[] Tables { get; set; }

        [OptionArray('e', "exclude-tables", HelpText = "List of tables to exclude from analysis. Wildcards * are allowed")]
        public string[] ExcludedTables { get; set; }

        [Option('d', "database", Required = false, HelpText = "Database name")]
        public String DatabaseName { get; set; }

        [OptionArray('s', "schema", Required = false, HelpText = "Names of schemas to check")]
        public string[] SchemaNames { get; set; }

        [Option('h', "host", Required = false, HelpText = "Database Host")]
        public String Host { get; set; }

        [Option("port", Required = false, HelpText = "Database port")]
        public String Port { get; set; }

        [Option('u', "username", Required = false, HelpText = "Database username")]
        public String Username { get; set; }

        [Option('p', "password", Required = false, HelpText = "Database password")]
        public String Password { get; set; }

        [Option("database-system", Required = false, HelpText = "Database system. Possible values: POSTGRESQL, MSSQL, MYSQL, ORACLE, FIREBIRD, DB2")]
        public DBLint.DataAccess.DBMSs? DBMS { get; set; }

        [Option("authmode", Required = false, HelpText = "SQL Server authentication mode. Possible values: SQLAuthentication, WindowsAuthentication")]
        public DBLint.DataAccess.AuthenticationMethod? AuthMode { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
