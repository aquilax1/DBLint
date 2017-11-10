using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DBLint.RuleControl;
using DBLint.DataAccess;
using DBLint.Model;
using DBLint.ModelBuilder;

namespace DBLint
{
    /// <summary>
    /// Facade for running a dblint analysis
    /// Alternative to the DatabaseLint class
    /// 
    /// Example usage:
    /// 
    /// IConfigFile config = new XMLConfigFile("conf.xml");
    /// var run = new Run(config);
    /// run.Start();
    /// </summary>
    public class Run
    {
        //Public properties
        public Connection Connection { get; private set; }
        public ExecutableCollection Executables { get; private set; }
        public IList<IRule> RulesToRun { get; private set; }
        public Database DatabaseModel { get; set; }
        public List<TableID> TablesToCheck { get; set; }
        public event ExecutionFinishedHandler ExecutionFinished = delegate { };
        public IConfigFile ConfigFile = null;

        //Constructor, create run from config file
        public Run(IConfigFile config)
        {
            this.ConfigFile = config;

            //Load all executables
            this.Executables = RuleLoader.LoadRules();

            if (this.ConfigFile.IsValid())
            {
                //Configure executables
                this.configureRulesFromConfig();

                //Extract connection from config
                this.Connection = config.GetConnection();

                //Set RulesToRun from the config file and configure
                this.setRulesToRunFromConfig();
            }
        }

        public Run(string configFilePath)
            : this(new XMLConfigFile(configFilePath))
        {
        }

        public Run()
        {
            this.Executables = RuleLoader.LoadRules();
            this.RulesToRun = this.Executables.GetSchemaRules().Cast<IRule>().ToList();
            this.Connection = new Connection();
        }

        public void SetTablesToCheck()
        {
            if (this.TablesToCheck != null)
            {
                return;
            }

            //Extract tables from config file
            if (this.ConfigFile != null)
            {
                var tables = this.ConfigFile.GetTablesToCheck();
                if (tables.Count() > 0)
                {
                    this.TablesToCheck = tables.ToList();
                    return;
                }
            }

            //Not specified in config, assume all tables in the database
            Extractor extractor = new Extractor(this.Connection);

            List<DataAccess.DBObjects.Table> dbtables;
            if (this.Connection.DBMS == DBMSs.MYSQL)
            {
                dbtables = extractor.Database.GetTables(this.Connection.Database);
            }
            else
            {
                dbtables = extractor.Database.GetTables();
            }
            this.TablesToCheck = Run.DBObjectTableToModelTable(dbtables, Connection.Database).ToList();
        }

        public void BuildModel()
        {
            this.SetTablesToCheck();

            //Extractor is used to create database model and extract schemas
            Extractor extractor = new Extractor(this.Connection);

            var schemaNamesToCheck = this.getSchemaNamesFromTables(this.TablesToCheck); //Schema names to check
            var schemas = extractor.Database.GetSchemas().Where(s => schemaNamesToCheck.Contains(s.SchemaName));

            //Find tables to ignore (these will not be a part of the model)
            //The opposite of tablesToCheck
            var ignoredTables = new List<DataAccess.DBObjects.TableID>();
            var tables = extractor.Database.GetTables(schemaNamesToCheck);
           
            foreach (var table in tables)
            {
                if (!this.TablesToCheck.Any(t => t.TableName == table.TableName && t.SchemaName == table.SchemaName))
                {
                    ignoredTables.Add(table);
                }
            }

            //Build model
            Database model = ModelBuilder.ModelBuilder.DatabaseFactory(extractor, schemas, ignoredTables);
            this.DatabaseModel = model;
        }

        public void Start()
        {
            if (this.DatabaseModel == null)
            {
                this.BuildModel();
            }

            RuleController ruleController = new RuleController();

            //Filter out rules which should not run
            var filteredExes = this.Executables.GetExecutables().ToList();
            filteredExes.RemoveAll(e => e.IsRule() && !this.RulesToRun.Contains(e));
            //Create a new Rule Collection containing only the executables in filteredExes
            ExecutableCollection executables = new ExecutableCollection(filteredExes);
            ruleController.ExecutionFinished += new ExecutionFinishedHandler(RuleController_ExecutionFinished);
            ruleController.Execute(executables, this.DatabaseModel, this.TablesToCheck, new IssueCollector());
        }

        void RuleController_ExecutionFinished(IExecutionSummary summary)
        {
            this.ExecutionFinished(summary);
        }

        private void setRulesToRunFromConfig()
        {
            this.RulesToRun = new List<IRule>();
            foreach (IRule rule in this.Executables.GetRules())
            {
                if (ConfigFile.RunRule(rule))
                {
                    this.RulesToRun.Add(rule);
                }
            }

            //If no rules have been specified, assume all rules should be run
            if (this.RulesToRun.Count == 0)
            {
                this.RulesToRun = this.Executables.GetRules().Cast<IRule>().ToList();
            }
        }

        private void configureRulesFromConfig()
        {
            foreach (var executable in this.Executables)
            {
                this.ConfigFile.ConfigureRule(executable);
            }
        }

        private IEnumerable<String> getSchemaNamesFromTables(IEnumerable<TableID> tables)
        {
            return tables.Select(t => t.SchemaName).Distinct().ToList();
        }

        public static IEnumerable<TableID> DBObjectTableToModelTable(IEnumerable<DataAccess.DBObjects.Table> tables, string databaseName)
        {
            return tables.Select(t => new TableID(databaseName, t.SchemaName, t.TableName));
        }
    }
}
