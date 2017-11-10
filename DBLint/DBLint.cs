using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Xml;
using DBLint.RuleControl;
using DBLint.Rules;
using DBLint.DataAccess;
using DBLint.Model;

namespace DBLint
{
    public class DatabaseLint
    {
        public ExecutableCollection AllExecutables { get; private set; }
        public RuleController RuleController { get; private set; }
        public IssueCollector IssueCollector { get; private set; }
        public Database DatabaseModel { get; set; }
        public Configuration Config { get; set; }
        public IExecutionSummary ExecutionSummary { get; private set; }
        public event ThreadStart AllDone;
        public IConfigFile ConfigFile { get; set; }

        public DatabaseLint()
        {
            //Load all executables
            this.AllExecutables = RuleLoader.LoadRules();
            //Save standard config if none exist
            if (DBLint.Settings.IsNormalContext)
            {
                /*
                if (!File.Exists(Settings.CONFIG_FILE))
                    PropertyUtils.SaveProperties(this.AllExecutables.GetExecutables(), Settings.CONFIG_FILE);
                PropertyUtils.LoadProperties(this.AllExecutables.GetExecutables(), Settings.CONFIG_FILE);
                */
            }
            this.RuleController = new RuleController();
            this.IssueCollector = new IssueCollector();
            this.Config = new Configuration();
            this.ConfigFile = new XMLConfigFile(Settings.XMLCONFIG);
        }

        /// <summary>
        /// Creates a new DatabaseLint object and sets up the configuration according to the specified configuration file
        /// </summary>
        public DatabaseLint(String configurationFile) : this()
        {
            this.ConfigFile = new XMLConfigFile(configurationFile);

            this.Config.Connection = ConfigFile.GetConnection();

            //Get SQL rules from config file and add to allExecutables
            var SQLRules = ConfigFile.GetSQLRules();
            this.AllExecutables.AddExecutables(SQLRules);

            //Select rules from executables
            this.Config.AllRules = this.AllExecutables.GetRules().Select(e => (IRule)e);

            //Load property values from config rule
            foreach (var r in Config.AllRules)
            {
                ConfigFile.ConfigureRule(r);
            }

            //Filter out rules which should not be run
            this.Config.RulesToRun = this.Config.AllRules.Where(r => ConfigFile.RunRule(r));

            //Tables to run
            this.Config.TablesToCheck = ConfigFile.GetTablesToCheck();
        }

        public void BuildModel()
        {
            if (this.Config != null)
            {
                //Extractor is used to create database model and extract schemas
                Extractor extractor = new Extractor(this.Config.Connection);

                var schemaNames = this.getSchemaNamesToCheck(this.Config.TablesToCheck);
                var schemas = extractor.Database.GetSchemas().Where(s => schemaNames.Contains(s.SchemaName));

                //Build model
                Database model = ModelBuilder.ModelBuilder.DatabaseFactory(extractor, schemas, this.Config.IgnoreTables);
                this.DatabaseModel = model;
            }
        }

        public void Run()
        {
            //Create standard configration, check on all tables and run all rules
            Configuration config = new Configuration();
            config.RulesToRun = this.AllExecutables.GetRules().Select(e => (IRule)e);
            config.TablesToCheck = this.DatabaseModel.Tables;
            this.Run(config);
        }

        public void Run(Configuration config)
        {
            this.IssueCollector.Reset();
            this.Config = config;
            //If no tables, assumme all tables
            if (config.TablesToCheck == null || config.TablesToCheck.Count() == 0)
            {
                config.TablesToCheck = this.DatabaseModel.Tables;
            }
            //If no rules are specified, assume all schema rules (primarily for command line)
            if (config.RulesToRun == null || config.RulesToRun.Count() == 0)
            {
                config.RulesToRun = this.AllExecutables.GetSchemaRules();
            }
            //Filter out rules which should not run
            var filteredExes = this.AllExecutables.GetExecutables().ToList();
            filteredExes.RemoveAll(e => e.IsRule() && !config.RulesToRun.Contains(e));
            //Create a new Rule Collection containing only the executables in filteredExes
            ExecutableCollection executables = new ExecutableCollection(filteredExes);
            this.RuleController.ExecutionFinished += new ExecutionFinishedHandler(RuleController_ExecutionFinished);
            this.RuleController.Execute(executables, this.DatabaseModel, config.TablesToCheck, this.IssueCollector);
        }

        void RuleController_ExecutionFinished(IExecutionSummary summary)
        {
            this.ExecutionSummary = summary;
            if (AllDone != null)
                AllDone();
        }

        public void SaveConfiguration()
        {
            this.ConfigFile.Save(this.Config);
        }

        private IEnumerable<string> getSchemaNamesToCheck(IEnumerable<TableID> tablesToCheck)
        {
            return tablesToCheck.Select(t => t.SchemaName).Distinct().ToList();
        }
    }
}
