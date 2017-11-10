using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;
using DBLint.Data;
using DBLint.RuleControl;
using DBLint.Model;

namespace DBLint.Rules.DataRules
{
    public class ColumnsOneValue : BaseDataRule
    {
        public override string Name
        {
            get { return "Columns With Only One Value"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.Low; }
        }

        public Property<int> MinValues = new Property<int>("Minimum Values", 100, "The minimum number of values in a column before applying this rule.", v => v > 0);

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < MinValues.Value)
                return;

            Dictionary<Column, Object> values = new Dictionary<Column, Object>();
            Dictionary<Column, int> valuesCount = new Dictionary<Column, int>();
            var candidates = table.QueryableColumns.ToList();
            candidates.ForEach(c => valuesCount.Add(c, 0));

            using (var rowEnumerator = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerator)
                {
                    foreach (var candidate in candidates.ToArray())
                    {
                        Object val = row[candidate.ColumnName];

                        if (val is DBNull || string.IsNullOrEmpty(val as String))
                            continue;


                        double dval;
                        if (double.TryParse(val.ToString(), out dval))
                        {
                            if (dval == -1 || dval == 0 || dval == 1)
                            {
                                candidates.Remove(candidate);
                                continue;
                            }
                        }

                        valuesCount[candidate] += 1;

                        if (!values.ContainsKey(candidate))
                        {
                            values.Add(candidate, val);
                        }
                        else
                        {
                            if (!val.Equals(values[candidate]))
                            {
                                candidates.Remove(candidate);
                            }
                        }
                    }

                    if (candidates.Count == 0)
                        break;
                }

            foreach (var candidate in candidates)
            {
                if (valuesCount[candidate] < MinValues.Value)
                    continue;

                Issue issue = new Issue(this, this.Severity);
                issue.Name = "Column With Only One Value";
                issue.Context = new ColumnContext(candidate);
                issue.Description = new Description("Column '{0}' contains only one value ({1}). Total occurences: {2}", candidate, values[candidate], valuesCount[candidate]);
                issueCollector.ReportIssue(issue);
            }
        }

        public override bool SkipTable(Model.Table table)
        {
            return false;
        }
    }
}
