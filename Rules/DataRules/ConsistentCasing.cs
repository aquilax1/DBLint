using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.RuleControl;
using DBLint.Model;
using DBLint.DataTypes;

namespace DBLint.Rules.DataRules
{
    public class ConsistentCasing : BaseDataRule
    {
        public override string Name
        {
            get { return "Inconsistent Casing of First Character in Text Columns"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.Low; }
        }

        public Property<int> MinValues = new Property<int>("Minimum Values", 100, "The minimum number of values in a column before applying this rule.", v => v > 0);
        public Property<int> Threshold = new Property<int>("Threshold (%)", 5, "If the number of values starting with either lowercase or uppercase is below this threshold, report an issue", v => v >= 0 && v <= 100);

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < MinValues.Value)
                return;

            var columns = table.QueryableColumns.Where(c => DataTypesLists.TextTypes().Contains(c.DataType));
            if (columns.Count() == 0)
                return;

            var testColumns = columns.Select(c => new TestColumn(c)).ToList();

            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var testColumn in testColumns)
                    {
                        Object val = row[testColumn.Column.ColumnName];

                        if (val is String)
                        {
                            String str = val as String;
                            if (str.Length < 1)
                                continue;

                            testColumn.ValueCount += 1;
                            if (str[0] == char.ToLower(str[0]))
                                testColumn.LowercaseStart += 1;
                            else
                                testColumn.UppercaseStart += 1;
                        }
                    }
                }

            foreach (var testColumn in testColumns)
            {
                if (testColumn.ValueCount <= this.MinValues.Value)
                    continue;

                float lowerPercent = ((float)testColumn.LowercaseStart / testColumn.ValueCount) * 100;
                float upperPercent = ((float)testColumn.UppercaseStart / testColumn.ValueCount) * 100;

                float lowest = Math.Min(lowerPercent, upperPercent);
                if (lowest < Threshold.Value && lowest != 0)
                {
                    Issue issue = new Issue(this, this.Severity);
                    issue.Name = "Inconsistent Casing of First Character in Text Column";
                    issue.Context = new ColumnContext(testColumn.Column);
                    issue.Description = new Description("Inconsistent casing of first character in column '{0}'", testColumn.Column);
                    issue.ExtendedDescription = new Description("{0}% starts with lowercase\n{1}% starts with uppercase", lowerPercent, upperPercent);
                    issueCollector.ReportIssue(issue);
                }
            }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        private class TestColumn
        {
            public int UppercaseStart { get; set; }
            public int LowercaseStart { get; set; }
            public int ValueCount { get; set; }
            public Column Column { get; private set; }

            public TestColumn(Column column)
            {
                this.UppercaseStart = 0;
                this.LowercaseStart = 0;
                this.ValueCount = 0;
                this.Column = column;
            }
        }
    }
}
