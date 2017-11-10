using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Model;
using DBLint.Data;

namespace DBLint.Rules.DataRules
{
    public class MissingNotNullConstraint : BaseDataRule
    {
        public override string Name
        {
            get { return "Missing 'not null' Constraints"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.Low; }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        public Property<int> MinRows = StandardProperties.MinimumRows(100);

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < MinRows.Value)
                return;

            var candidates = table.QueryableColumns.Where(c => c.IsNullable == true).ToList();
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var candidate in candidates.ToArray())
                    {
                        Object val = row[candidate.ColumnName];
                        if (val is DBNull)
                            candidates.Remove(candidate);
                    }

                    if (candidates.Count == 0)
                        break;
                }

            foreach (var candidate in candidates)
            {
                Issue issue = new Issue(this, this.Severity);
                issue.Name = "Missing 'not null' Constraint";
                issue.Description = new Description("Column '{0}' in table {1} is nullable, but no null values exists.",
                    candidate.ColumnName, table);
                issue.ExtendedDescription = new Description("Consider adding a not-null constraint.\nRows analyzed: {0}", table.Cardinality);
                issue.Context = new ColumnContext(candidate);
                issueCollector.ReportIssue(issue);
            }
        }
    }
}
