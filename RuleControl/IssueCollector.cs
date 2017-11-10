using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using System.Threading;

namespace DBLint.RuleControl
{
    public class IssueCollector : List<Issue>, IIssueCollector
    {
        private ReaderWriterLockSlim listLock = new ReaderWriterLockSlim();
        private DatabaseDictionary<TableID, List<Issue>> tableIssues = DictionaryFactory.CreateTableID<List<Issue>>();
        private Dictionary<IRule, List<Issue>> ruleIssues = new Dictionary<IRule,List<Issue>>();

        public IEnumerable<Issue> GetIssues()
        {
            this.listLock.EnterReadLock();
            var ret = this.ToList();
            this.listLock.ExitReadLock();
            return ret;
        }

        public void Reset()
        {
            this.listLock.EnterWriteLock();
            this.Clear();
            this.tableIssues.Clear();
            this.ruleIssues.Clear();
            this.listLock.ExitWriteLock();
        }

        public IEnumerable<Issue> GetIssues(params Severity[] severity)
        {
            this.listLock.EnterReadLock();
            var ret = this.Where(i => severity.Contains(i.Severity)).OrderBy(i => i.Severity);
            this.listLock.ExitReadLock();
            return ret;
        }

        public IEnumerable<Issue> GetIssues(ContextLocation location)
        { 
            this.listLock.EnterReadLock();
            var ret = this.Where(i => i.Context.Location == location).ToList();
            this.listLock.ExitReadLock();
            return ret;
        }

        public IEnumerable<Issue> GetIssues(IRule rule)
        {
            this.listLock.EnterReadLock();
            var ret = this.Where(i => i.Rule == rule).ToList();
            this.listLock.ExitReadLock();
            return ret;
        }

        public IEnumerable<Issue> GetDatabaseIssues(Database db)
        { 
            return this.Where(i => i.Context.Location == ContextLocation.Database &&
                                   ((DatabaseContext)i.Context).Database.Equals(db)).ToList();
        }

        public IEnumerable<Issue> GetSchemaIssues(Schema schema)
        {
            return this.Where(i => i.Context.Location == ContextLocation.Schema &&
                                    Schema.SchemaEquals(schema, ((SchemaContext)i.Context).Schema)).ToList();
        }

        public IEnumerable<Issue> GetTableIssues(Table table)
        {
            return this.GetIssues(table).Where(i => i.Context.Location == ContextLocation.Table);
        }

        public IEnumerable<Issue> GetColumnIssues(Column column)
        {
            var tabIssues = this.GetIssues(column.Table);
            return tabIssues.Where(i => i.Context.Location == ContextLocation.Column &&
                                   ((ColumnContext)i.Context).Column.Equals(column)).ToList();
        }

        public IEnumerable<Issue> GetIssues(TableID table)
        {
            this.listLock.EnterReadLock();
            List<Issue> ret = new List<Issue>();
            if (this.tableIssues.ContainsKey(table))
                ret = this.tableIssues[table];
            this.listLock.ExitReadLock();
            return ret;
        }

        public void ReportIssue(Issue issue)
        {
            this.validateIssue(issue);
            this.listLock.EnterWriteLock();
            this.Add(issue);
            foreach (TableID table in issue.Context.GetTables())
            {
                if (!this.tableIssues.ContainsKey(table))
                    this.tableIssues.Add(table, new List<Issue>());                
                this.tableIssues[table].Add(issue);
            }

            if (!this.ruleIssues.ContainsKey(issue.Rule))
                this.ruleIssues.Add(issue.Rule, new List<Issue>());
            this.ruleIssues[issue.Rule].Add(issue);

            this.NewIssue(issue);
            this.listLock.ExitWriteLock();
        }

        private void validateIssue(Issue issue)
        {
            if (issue == null)
                throw new Exception("A null-issue is passed to the issue collector");
            else if (issue.Name == null)
                throw new Exception("Issue has no name");
            else if (issue.Description == null)
                throw new Exception("Issue has no description: " + issue.Name);
            else if (issue.Context == null)
                throw new Exception("Issue has no context: " + issue.Name);
        }

        public event NewIssueHandler NewIssue = delegate { };
    }
}
