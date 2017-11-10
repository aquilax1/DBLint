using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.DataRules
{
    public class NotNullAllBlank : BaseDataRule
    {
        public override string Name
        {
            get { return "Not-Null Columns Containing Many Empty Strings"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.High; }
        }

        public Property<int> Threshold = new Property<int>("Threshold", 10, "Percentage of rows allowed to be the empty string", v => v >= 0 && v <= 100);
        public Property<int> MinRows = StandardProperties.MinimumRows(50);

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < MinRows.Value)
                return;

            var candidateColumns = table.Columns.Where(c => DataTypes.DataTypesLists.TextTypes().Contains(c.DataType));

            Dictionary<Column, int> candidates = new Dictionary<Column, int>(); //column -> blank_count
            candidateColumns.ToList().ForEach(c => candidates.Add(c, 0));
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var candidate in candidates.Keys.ToArray())
                    {
                        Object val = row[candidate.ColumnName];
                        if (val is String && val.Equals(String.Empty))
                        {
                            candidates[candidate] += 1;
                        }
                    }
                }

            foreach (var candidate in candidates)
            {
                float percentBlanks = ((float)candidate.Value / table.Cardinality) * 100;
                if (percentBlanks > Threshold.Value)
                {
                    Issue issue = new Issue(this, this.Severity);
                    issue.Name = "The Empty String Used to Represent Null";
                    issue.Context = new ColumnContext(candidate.Key);

                    if (candidate.Key.IsNullable)
                    {
                        issue.Description = new Description("The nullable column '{0}' contains {2} rows of which {1} are the empty string. Consider using null",
                            candidate.Key.ColumnName, candidate.Value, table.Cardinality);
                    }
                    else
                    {
                        issue.Description = new Description("The not-null column '{0}' contains {2} rows of which {1} are the empty string",
                            candidate.Key.ColumnName, candidate.Value, table.Cardinality);
                        issue.ExtendedDescription = new Description("Consider removing the not-null constraint and use null to represent non-existing values");
                    }

                    issueCollector.ReportIssue(issue);
                }
            }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }
    }
}
