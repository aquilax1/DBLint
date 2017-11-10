using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Model;
using DBLint.Data;

namespace DBLint.Rules.DataRules
{
    public class AllValuesDefault : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return RuleControl.Severity.High; }
        }

        public override string Name
        {
            get { return "All Values Equals the Default Value"; }
        }

        public Property<int> MinRows = StandardProperties.MinimumRows(100);
        
        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < this.MinRows.Value)
                return;

            var candidates = table.QueryableColumns.Where(c => c.DefaultValue != null).ToList();
            using (var rowEnumerable = table.GetTableRowEnumerable())
            {
                foreach (var row in rowEnumerable)
                {
                    foreach (var candidateColumn in candidates.ToArray())
                    {
                        Object val = row[candidateColumn.ColumnName];
                        if (!val.ToString().Equals(candidateColumn.DefaultValue))
                        {
                            candidates.Remove(candidateColumn);
                        }
                    }

                    if (candidates.Count == 0)
                        break;
                }
            }

            foreach (var candidateColumn in candidates)
            {
                if (candidateColumn.DefaultValue == "0" || candidateColumn.DefaultValue == "1")
                    continue;

                Issue issue = new Issue(this, this.Severity);
                issue.Name = "All Values Equals the Default Value";
                issue.Context = new ColumnContext(candidateColumn);
                issue.Description = new Description("All {0} values in column '{1}' equals default value '{2}'. Is the column necessary?",
                    table.Cardinality, candidateColumn.ColumnName, candidateColumn.DefaultValue);
                issueCollector.ReportIssue(issue);
            }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }
    }
}
