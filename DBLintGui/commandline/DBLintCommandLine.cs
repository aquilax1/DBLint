using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using DBLint;
using DBLint.DataAccess;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Used for command line execution of DBLint
    /// </summary>
    class CommandLineExecutor
    {
        //Parsed arguments specified as input to the program
        private CommandLineOptions options = null;

        public CommandLineExecutor(CommandLineOptions options)
        {
            this.options = options;
        }

        public void Execute()
        {
            Run run;

            //Create run from config file if its specified
            if (this.options.ConfigFile != null)
            {
                if (!File.Exists(this.options.ConfigFile))
                {
                    Console.WriteLine("DBLint: Could not find config file");
                    Quit();
                }
                run = new Run(this.options.ConfigFile);

                if (!run.ConfigFile.IsValid())
                {
                    Console.WriteLine("DBLint: The config file is not valid");
                    Quit();
                }
            }
            //no config file specified, create empty run
            else
            {
                run = new Run();
            }

            //If connection arguments are specified in arguments, modify the connection loaded from config file
            this.overrideConnection(run.Connection);
            //Check connection
            if (run.Connection == null)
            {
                Console.WriteLine("DBLint: Connection not specified");
                Quit();
            }
            else if (run.Connection.DBMS == DBMSs.NONE)
            {
                Console.WriteLine("Database system (DBMS) has not been specified");
                Quit();
            }
            else if (!this.testConnection(run.Connection))
            {
                Console.WriteLine("DBLint: Unable to connect to database");
                Quit();
            }

            //Set tables to check
            this.setTablesToCheck(run);
            //remove tables if --exclude-tables is set
            run.TablesToCheck = this.removeExcludedTables(run.TablesToCheck);

            run.ExecutionFinished += new ExecutionFinishedHandler(ExecutionFinished);
            run.Start();
        }

        private bool testConnection(Connection conn)
        {
            if (conn.Database == null)
            {
                Console.WriteLine("Database not specified");
                return false;
            }

            try
            {
                return (new DataAccess.Extractor(conn).Database.TestConnection());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        private void overrideConnection(Connection connection)
        {
            //Check if --port is set
            if (this.options.Port != null)
            {
                connection.Port = this.options.Port;
            }

            //Check if --host is set
            if (this.options.Host != null)
            {
                connection.Host = this.options.Host;
            }

            //Check if --username is set
            if (this.options.Username != null)
            {
                connection.UserName = this.options.Username;
            }

            //Check if --password is set
            if (this.options.Password != null)
            {
                connection.Password = this.options.Password;
            }

            //Check if --database is set
            if (this.options.DatabaseName != null)
            {
                connection.Database = this.options.DatabaseName;
            }

            //Check if --database-system is set
            if (this.options.DBMS.HasValue)
            {
                connection.DBMS = this.options.DBMS.Value;
            }

            //Check if --authmode
            if (this.options.AuthMode.HasValue)
            {
                connection.AuthenticationMethod = this.options.AuthMode.Value;
            }
        }

        //Sets "run.TablesToCheck" from either --include-tables, config file and --exclude-tables
        private void setTablesToCheck(Run run)
        {
            if (this.options.Tables != null && this.options.Tables.Length > 0)
            {
                run.TablesToCheck = new List<TableID>(); //Reset list from config file, use command line args instead

                //Get all tables from connection, or schema if specified in in args.
                var tables = this.getAllTables(run.Connection);

                //Loop over all patterns
                foreach (var pattern in this.options.Tables)
                {
                    Regex regex = this.stringToRegex(pattern);

                    //Try match on all tables
                    foreach (TableID table in tables)
                    {
                        if (regex.IsMatch(table.TableName) && !run.TablesToCheck.Any(t => t.SchemaName == table.SchemaName && t.TableName == table.TableName))
                        {
                            run.TablesToCheck.Add(table);
                        }
                    }
                }
            }
            //If schema is specified in args (without tables, restrict 
            else if (this.options.SchemaNames != null)
            {
                var tables = this.getAllTables(run.Connection); //getAllTables is already filtering on SchemaNames option
                run.TablesToCheck = tables.ToList();

            }
            //Extract from config fil
            else if (run.ConfigFile != null)
            {
                run.TablesToCheck = run.ConfigFile.GetTablesToCheck().ToList();
            }
            //Not specified at all, assume all tables
            else
            {
                run.TablesToCheck = this.getAllTables(run.Connection).ToList();
            }
        }

        private List<TableID> removeExcludedTables(IEnumerable<TableID> tables)
        {
            List<TableID> excluded = new List<TableID>();

            //Only relevant if --exclude-tables is set
            if (this.options.ExcludedTables != null)
            {
                //Loop through each pattern an match tables
                foreach (var pattern in this.options.ExcludedTables)
                {
                    Regex regex = this.stringToRegex(pattern);
                    //Regex regex = new Regex(pattern);

                    foreach (var table in tables)
                    {
                        //Matches current pattern and have not been added to excluded already
                        if (regex.IsMatch(table.TableName) && !excluded.Contains(table))
                        {
                            excluded.Add(table);
                        }
                    }
                }
            }

            //Return all tables not in excluded list
            return tables.Where(t => !excluded.Contains(t)).ToList();
        }

        private Regex stringToRegex(string str)
        {
            var regex = Regex.Escape(str).Replace(@"\*", ".*");
            regex = "^" + regex + "$";
            return new Regex(regex, RegexOptions.IgnoreCase);
        }

        private IEnumerable<TableID> getAllTables(Connection connection)
        {
            Extractor extractor = new Extractor(connection);

            //Check if "schemas" is specified in command line arguments
            if (this.options.SchemaNames != null)
            {
                var tables = extractor.Database.GetTables(this.options.SchemaNames);
                return Run.DBObjectTableToModelTable(tables, connection.Database);
            }
            //Asumme all schemas in connection if not specified
            else
            {
                List<DataAccess.DBObjects.Table> tables;
                if (connection.DBMS == DBMSs.MYSQL)
                {
                    tables = extractor.Database.GetTables(connection.Database);
                }
                else
                {
                    tables = extractor.Database.GetTables();
                }
                return Run.DBObjectTableToModelTable(tables, connection.Database);
            }
        }

        //Display issues in the console
        static void ExecutionFinished(RuleControl.IExecutionSummary summary)
        {
            TextIssueOutputter.OutputIssues(summary, Console.Out);
            Quit();
        }

        private static void Quit()
        {
            //Console.ReadLine();
            Environment.Exit(0);
        }
    }
}