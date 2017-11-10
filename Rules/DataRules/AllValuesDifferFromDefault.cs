using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.Data;
using DBLint.DataTypes;
using DBLint.RuleControl;

namespace DBLint.Rules.DataRules
{
    public class AllValuesDifferFromDefault : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return RuleControl.Severity.Low; }
        }

        public override string Name
        {
            get { return "All Values Differ From the Default Value"; }
        }

        public Property<int> MinRows = StandardProperties.MinimumRows(100);

        public override bool SkipTable(Table table)
        {
            return table.Columns.All(c => c.DefaultValue == null);
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < this.MinRows.Value)
                return;

            var candidates = table.QueryableColumns.Where(c => c.DefaultValue != null && c.DefaultValue != "0" && c.DefaultValue != "1" && c.IsSequence == false && c.IsDefaultValueAFunction == false).ToList();

            using (var rowEnumerator = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerator)
                {
                    foreach (var candidate in candidates.ToArray())
                    {
                        if (Convert.ToString(row[candidate.ColumnName]).Equals(candidate.DefaultValue))
                            candidates.Remove(candidate);
                    }

                    if (candidates.Count == 0)
                        break;
                }

            foreach (var candidateColumn in candidates)
            {
                Issue issue = new Issue(this, this.Severity);
                issue.Name = "All Values are Different From the Default Value";
                issue.Context = new ColumnContext(candidateColumn);
                issue.Description = new Description("All {0} values in column '{1}' are different from the default value '{2}'. Is the default value necessary?",
                    table.Cardinality, candidateColumn.ColumnName, candidateColumn.DefaultValue);
                issueCollector.ReportIssue(issue);
            }
        }
    }
}
