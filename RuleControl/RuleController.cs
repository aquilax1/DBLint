using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DBLint.Model;
using DBLint.Data;
using System.Diagnostics;

namespace DBLint.RuleControl
{
    public delegate void ExecutionStartedHandler();
    public delegate void ExecutionFinishedHandler(IExecutionSummary summary);
    public delegate void CurrentTableChangedHandler(Table table);
    public delegate void ProgressChangedHandler(int progress);

    /// <summary>
    /// Schedules and executes rules/providers
    /// </summary>
    public class RuleController
    {
        private IScheduler scheduler = new PerTableScheduler();
        private ProgressTracker progressTracker;
        private Table currentTable = null;

        //Execution summary
        private ExecutionSummaryImpl executionSummary = new ExecutionSummaryImpl();
        private ReaderWriterLockSlim executionSummaryLock = new ReaderWriterLockSlim();
        private Stopwatch executionClockTotal = new Stopwatch();
        
        //Objects passed to the executables when executing them
        private IssueCollector issueCollector;
        private ProviderCollection providerCollection;
        private Database database;

        //Events
        public event ExecutionStartedHandler ExecutionStarted = delegate { };
        public event ExecutionFinishedHandler ExecutionFinished = delegate { };
        public event CurrentTableChangedHandler CurrentTableChanged = delegate { };
        public event ProgressChangedHandler ProgressUpdated = delegate { };

        public ProviderCollection ProviderCollection
        {
            get { return this.providerCollection; }
        }

        public void Reset()
        {
            this.executionClockTotal.Reset();
            this.ExecutionStarted = null;
            this.ExecutionFinished = null;
            this.CurrentTableChanged = null;
            this.executionSummary = new ExecutionSummaryImpl();
            if (this.issueCollector != null)
                this.issueCollector.Reset();
        }
        /// <summary>
        /// Executes all executable in the given ExecutableCollection
        /// </summary>
        public void Execute(ExecutableCollection executables, Database databaseModel, IEnumerable<TableID> tablesToCheckNames, IssueCollector issueCollector)
        {
            this.providerCollection = new ProviderCollection();

            var tablesToCheck = databaseModel.Tables.Where(t => tablesToCheckNames.Any(c => c.TableName == t.TableName && c.SchemaName == t.SchemaName));

            this.database = databaseModel;
            this.issueCollector = issueCollector;

            this.ExecutionStarted();
            this.executionClockTotal.Start();

            var plan = scheduler.GetExecutionPlan(executables, tablesToCheck);

            this.progressTracker = new ProgressTracker(plan.GetExecutables(), tablesToCheck);

            plan.ExecutePlan(this.runExecutable, this.finishedHandler);
        }

        private void finishedHandler()
        {
            this.executionClockTotal.Stop();
            this.executionSummary.ExecutionTime = this.executionClockTotal.Elapsed;
            this.ExecutionFinished(this.executionSummary);
        }

        private void runExecutable(Object executableObject)
        {
            ExecutionUnit unit = (ExecutionUnit)executableObject;
            var executable = unit.Executable;

            if (unit.Finalize)
            {
                ((IDataExecutable)executable).Finalize(this.database, this.providerCollection);
                if (executable.IsProvider())
                    this.providerCollection.AddProvider((IProvider)executable);
                return;
            }

            if (executable.IsData() && unit.Table != null && unit.Table != this.currentTable)
            {
                this.currentTable = unit.Table;
                CurrentTableChanged(unit.Table);
                //Console.WriteLine("Finished: " + unit.Table.TableName);
            }

            var table = (DataTable)unit.Table;

            Stopwatch clock = new Stopwatch();
            clock.Start();
            FinishedStatus finishedStatus;

            //Execute (executables have different execute signatures)
#if !DEBUG
            try
            {
#endif
            if (executable.IsSchemaRule())
            {
                ISchemaRule rule = (ISchemaRule)executable;
                rule.Execute(this.database, this.issueCollector, this.providerCollection);
            }
            else if (executable.IsSQLRule())
            {
                SQLRule rule = (SQLRule)executable;
                rule.Execute(this.database, this.issueCollector);
            }
            else if (executable.IsDataRule())
            {
                IDataRule rule = (IDataRule)executable;
                rule.Execute(table, this.issueCollector, this.providerCollection);
            }
            else if (executable.IsSchemaProvider())
            {
                ISchemaProvider prov = (ISchemaProvider)executable;
                prov.Execute(this.database, this.providerCollection);
            }
            else if (executable.IsDataProvider())
            {
                IDataProvider prov = (IDataProvider)executable;
                prov.Execute(table, this.providerCollection);
            }

            if (executable is IProvider && executable.IsSchema())
            {
                this.providerCollection.AddProvider((IProvider)executable);
            }

            finishedStatus = FinishedStatus.Success;
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                finishedStatus = FinishedStatus.Failed;
            }
#endif

            clock.Stop();
            executionSummaryLock.EnterWriteLock();
            if (executable.IsSchemaRule() || executable.IsSQLRule())
            {
                var issues = this.issueCollector.GetIssues((IRule)executable);
                RuleSummary summary = new RuleSummary((IRule)executable, issues, finishedStatus, clock.Elapsed);
                this.executionSummary.AddRuleSummary(summary);
            }
            else if (executable.IsDataRule())
            {
                IRule rule = (IRule)executable;
                var issues = this.issueCollector.GetIssues(rule);
                var sum = executionSummary.GetSummary(rule);
                if (sum == null)
                {
                    sum = new RuleSummary(rule, null, finishedStatus, new TimeSpan());
                    this.executionSummary.AddRuleSummary(sum);
                }
                sum.ExecutionTime.Add(clock.Elapsed);
                sum.Issues = issues;
            }

            int pbefore = this.progressTracker.Progress;
            this.progressTracker.UpdateProgress(executable, table);
            this.ProgressUpdated(this.progressTracker.Progress);
            int pafter = this.progressTracker.Progress;

            //if (pbefore != pafter)
            //    Console.WriteLine(pafter);

            executionSummaryLock.ExitWriteLock();
        }
    }
}
