using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.DBLintGui.ExecuteTab
{
    /// <summary>
    /// This class initializes events, boots the analysis process, cleans up etc.
    /// </summary>
    public class ExecuteWorker
    {
        private readonly DBLintExecuter _executer;
        private readonly DatabaseLint _dblint;

        private Thread _thread;
        private Database _database;

        public string ReportDir { get; set; }

        public ExecuteWorker(DBLintExecuter executer, DatabaseLint dblint)
        {
            _executer = executer;
            _dblint = dblint;
        }

        public void StartWork()
        {
            _thread = new Thread(DoWork);
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        private void DoWork()
        {
            using (var executer = _executer)
            using (Semaphore sem = new Semaphore(0, 1))
            {
                List<DataAccess.DBObjects.Schema> selectedSchemas = new List<DataAccess.DBObjects.Schema>();

                var ignoredTables = new List<DataAccess.DBObjects.Table>();
                foreach (var schema in _executer.ViewModel.MetadataSelection.Schemas)
                {
                    if (schema.Include == null || schema.Include.Value)
                    {
                        selectedSchemas.Add(schema.DBSchema);
                        foreach (var table in schema.Tables.Value)
                        {
                            if (!table.Include)
                                ignoredTables.Add(table.DatabaseTable);
                        }
                    }
                }

                _database = GetDatabaseMetadata(selectedSchemas, ignoredTables);
                SetUpDatabaseLint(sem);

                List<Model.Table> selectedTables = GetSelectedTables(_database);
                List<IExecutable> rulesToRun = GetRulesToRun();

                try
                {
                    _dblint.Run(new Configuration(rulesToRun, selectedTables));
                }
                catch (Exception e)
                {
                    _executer.PostMessage("Whoops, an error happend during the execution of a rule: " + e.InnerException.ToString());
                }
                
              
                sem.WaitOne();

                _executer.PostMessage("Rules finished");
                _executer.PostMessage("Generating HTML report");

                string reportname = Util.FileUtils.EscapeDirectoryName(this._database.FriendlyName);

                var reportDir = Path.Combine("reports", reportname);

                this.ReportDir = reportDir;
                DBLint.ReportGeneration.HTMLBuilder.WriteReport(reportDir, _dblint);
                _executer.PostMessage("HTML finished");
                System.Diagnostics.Process.Start(Path.Combine(reportDir, "mainpage.html"));
                _executer.Update(e => e.WorkDone = e.TotalWork);
                _executer.SetFinished(reportname);
            }
        }

        private void SetUpDatabaseLint(Semaphore sem)
        {
            _dblint.RuleController.Reset();
            _dblint.RuleController.ExecutionStarted += () => _executer.PostMessage("Executing rules");
            _dblint.RuleController.CurrentTableChanged += t => _executer.PostMessage(String.Format("Analyzing {0}.{1}", t.Schema, t.TableName));
            _dblint.RuleController.CurrentTableChanged += t => _executer.Update(e => e.TablesAnalyzed += 1);
            _dblint.RuleController.ProgressUpdated += t => _executer.Update(e => e.WorkDone = t);
            _dblint.RuleController.ExecutionFinished += summary => sem.Release();
            _dblint.IssueCollector.NewIssue += issue => _executer.Update(e => e.FoundIssues += 1);
            _dblint.DatabaseModel = _database;
        }

        private List<IExecutable> GetRulesToRun()
        {
            var rulesToRun = new List<IExecutable>();
            foreach (var ruleSet in _executer.ViewModel.RulesConfiguration.RuleSets)
                foreach (var rule in ruleSet.Rules)
                    if (rule.Include)
                        rulesToRun.Add(rule.Executable);
            return rulesToRun;
        }

        private List<Model.Table> GetSelectedTables(Database database)
        {
            var selectedTables = new List<DBLint.Model.Table>();
            var tid = new WriteableTableID(_executer.ViewModel.MetadataSelection.Extractor.DatabaseName, null, null);

            foreach (var schema in _executer.ViewModel.MetadataSelection.Schemas)
            {
                if (schema.Include.HasValue && !schema.Include.Value)
                    continue;
                tid.SetSchemaName(schema.Name);
                foreach (var tbl in schema.Tables.Value)
                {
                    if (!tbl.Include)
                        continue;
                    tid.SetTableName(tbl.Name);
                    Model.Table table = database.GetTable(tid);
                    selectedTables.Add(table);
                }
            }
            return selectedTables;
        }

        private Database GetDatabaseMetadata(IEnumerable<DataAccess.DBObjects.Schema> selectedSchemas, IEnumerable<DataAccess.DBObjects.Table> ignoredTables)
        {
            _executer.PostMessage("Fetching metadata...");
            var database = DBLint.ModelBuilder.ModelBuilder.DatabaseFactory(_executer.ViewModel.MetadataSelection.Extractor, selectedSchemas, ignoredTables);
            _executer.PostMessage("Metadata done");
            // TODO: A large hach to handle databases as files
            try
            {
                if (File.Exists(database.DatabaseName))
                    database.FriendlyName = Path.GetFileName(database.DatabaseName);
                else if (Directory.Exists(database.DatabaseName))
                    database.FriendlyName = Path.GetDirectoryName(database.DatabaseName);
            }
            catch { }
            return database;
        }
    }
}