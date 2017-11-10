using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace DBLint.IncrementalRuns
{
    public interface IDiff
    {
        void Compare(Run oldRun, Run newRun);
        Run NewRun { get; set; }
        Run OldRun { get; set; }
        IEnumerable<TableDiff> Tables { get; set; }
        IEnumerable<IssueDiff> Issues { get; set; }
    }

    public class Diff : IDiff
    {
        public Run NewRun { get; set; }
        public Run OldRun { get; set; }
        public IEnumerable<TableDiff> Tables { get; set; }
        public IEnumerable<IssueDiff> Issues { get; set; }

        public void Compare(Run oldRun, Run newRun)
        {
            this.NewRun = newRun;
            this.OldRun = oldRun;
            var tables = new List<TableDiff>();
            var issues = new List<IssueDiff>();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            //Added tables
            Dictionary<Table, Table> oldRunTables = new Dictionary<Table, Table>();
            Dictionary<Table, Table> oldRunTablesIgnored = new Dictionary<Table, Table>();
            oldRun.Tables.ToList().ForEach(t => oldRunTables.Add(t, t));
            oldRun.IgnoredTables.ToList().ForEach(t => oldRunTablesIgnored.Add(t, t));

            foreach (var t in newRun.Tables)
            {
                if (!oldRunTables.ContainsKey(t) && !oldRunTablesIgnored.ContainsKey(t))
                    tables.Add(new TableDiff(t.TableID, TableStatus.New, -1, t.Score));
            }

            //Removed tables
            Dictionary<Table, Table> newRunTables = new Dictionary<Table, Table>();
            Dictionary<Table, Table> newRunTablesIgnored = new Dictionary<Table, Table>();
            newRun.Tables.ToList().ForEach(t => newRunTables.Add(t, t));
            newRun.IgnoredTables.ToList().ForEach(t => newRunTablesIgnored.Add(t, t));
            
            foreach (var t in oldRun.Tables)
            {
                if (!newRunTables.ContainsKey(t) && !newRunTablesIgnored.ContainsKey(t))
                    tables.Add(new TableDiff(t.TableID, TableStatus.Removed, t.Score, -1));
            }

            //Changed tables
            foreach (var t in newRun.Tables)
            {
                if (oldRunTables.ContainsKey(t))
                {
                    var oldT = oldRunTables[t];
                    if (t.Score != oldT.Score)
                        tables.Add(new TableDiff(t.TableID, TableStatus.Changed, oldT.Score, t.Score));
                }
            }

            Dictionary<Issue, Issue> newRunIssues = new Dictionary<Issue, Issue>();
            Dictionary<Issue, Issue> oldRunIssues = new Dictionary<Issue, Issue>();
            List<int> codes = new List<int>();
            foreach (var i in oldRun.Issues)
            {
                oldRunIssues.Add(i, i);
            }
            foreach (var i in newRun.Issues)
            {
                newRunIssues.Add(i, i);
            }

            foreach (var i in newRun.Issues)
            {
                if (!oldRunIssues.ContainsKey(i))
                {
                    var tableIDs = i.tables.Select(t => new DBLint.Model.TableID(t.DatabaseName, t.SchemaName, t.TableName));
                    issues.Add(new IssueDiff(i, IssueStatus.New, tableIDs));
                }
            }

            foreach (var i in oldRun.Issues)
            {
                if (!newRunIssues.ContainsKey(i))
                {
                    var tableIDs = i.tables.Select(t => new DBLint.Model.TableID(t.DatabaseName, t.SchemaName, t.TableName));
                    issues.Add(new IssueDiff(i, IssueStatus.Fixed, tableIDs));
                }
            }

            this.Tables = tables;
            this.Issues = issues;
        }
    }
}
