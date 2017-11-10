using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Data;
using DBLint.Model;

namespace DBLint.Rules.DataRules
{
    public class NumberDateInVarchar : BaseDataRule
    {
        public override string Name
        {
            get { return "Numbers or Dates Stored in Varchar Columns"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.High; }
        }

        public Property<int> minValues = new Property<int>("Minimum Values to Consider",
            5, "The minimum number of values to be classified as a wrong type before reporting an issue.", i => i > 0);

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var textColumns = table.Columns.Where(c => DataTypes.DataTypesLists.TextTypes().Contains(c.DataType));
            var candidates = textColumns.Select(c => new Candidate(c)).ToList();

            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var candidate in candidates.ToArray())
                    {
                        Object val = row[candidate.Column.ColumnName];

                        if (val is DBNull || (val is String) == false)
                            continue;

                        String str = (String)val;
                        ValueType type = Classifier.Classify(str);

                        if (type != ValueType.Date && type != ValueType.Float && type != ValueType.Int)
                        {
                            candidates.Remove(candidate);
                            continue;
                        }

                        if (candidate.ValuesFound == 0)
                        {
                            candidate.Type = type;
                        }

                        if (type != candidate.Type)
                            candidates.Remove(candidate);
                        else
                            candidate.ValuesFound += 1;
                    }

                    if (candidates.Count == 0)
                        break;
                }

            foreach (var candidate in candidates)
            {
                if (candidate.ValuesFound < this.minValues.Value)
                    continue;

                Issue i = new Issue(this, this.Severity);
                i.Name = "Numbers or Dates Stored in Varchar Column";
                i.Context = new ColumnContext(candidate.Column);
                i.Description = new Description("The varchar column '{0}' is used to store values of type '{1}'", candidate.Column, candidate.Type.ToString());
                issueCollector.ReportIssue(i);
            }
        }

        private class Candidate
        {
            public Column Column { get; set; }
            public ValueType Type { get; set; }
            public int ValuesFound { get; set; }

            public Candidate(Column column)
            {
                this.Column = column;
            }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }
    }
}
