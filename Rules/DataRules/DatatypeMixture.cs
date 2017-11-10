using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.DataRules
{
    public class DatatypeMixture : BaseDataRule
    {
        public override string Name
        {
            get { return "Mixture of Data Types in Text Columns"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.High; }
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var columns = table.QueryableColumns.Where(c => DataTypesLists.TextTypes().Contains(c.DataType));
            var candidates = columns.ToList().Select(c => new Candidate(c)).ToList();

            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var candidate in candidates)
                    {
                        Object value = row[candidate.Column.ColumnName];
                        if (value is String)
                        {
                            String valueStr = (String)value;
                            if (valueStr == String.Empty)
                                continue;
                            ValueType type = Classifier.Classify(valueStr);
                            candidate.AddValue(valueStr, type);
                        }
                    }

                    if (candidates.Count() == 0)
                        break;
                }

            foreach (var candidate in candidates)
            {
                if (candidate.Values.Count <= 1)
                    continue;

                System.Data.DataTable valuesTable = new System.Data.DataTable();
                valuesTable.Columns.Add("Value Examples", typeof(String));
                valuesTable.Columns.Add("Data Type", typeof(String));
                foreach (var dicEntry in candidate.Values)
                {
                    foreach (var val in dicEntry.Value)
                    {
                        var row = valuesTable.NewRow();
                        row[0] = val;
                        row[1] = dicEntry.Key.ToString();
                        valuesTable.Rows.Add(row);
                    }
                }


                Issue issue = new Issue(this, this.Severity);
                issue.Name = "Mixture of Data Types in a Column";
                issue.Description = new Description("The column '{0}' contains a mixture of data types", candidate.Column);
                issue.ExtendedDescription = new Description("Examples:\n\n{0}", valuesTable);
                issue.Context = new ColumnContext(candidate.Column);
                issueCollector.ReportIssue(issue);
            }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        private class Candidate
        {
            public Column Column { get; private set; }
            public Dictionary<ValueType, List<String>> Values = new Dictionary<ValueType, List<String>>();

            public Candidate(Column column)
            {
                this.Column = column;
            }

            public void AddValue(String value, ValueType type)
            {
                if (!Values.ContainsKey(type))
                {
                    Values.Add(type, new List<String>());
                }

                if (Values[type].Count < 5)
                {
                    Values[type].Add(value);
                }
            }
        }
    }
}
