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
    public class LargeVarchar : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return Severity.Low; }
        }

        public Property<int> MinRows = StandardProperties.MinimumRows(100);
        public Property<int> LargeVarcharLength = new Property<int>("Minimum Length", 40, "Consider only text columns of minimum this length", i => i > 0);
        private Func<Column, bool> columnPredicate;

        public LargeVarchar()
        {
            this.columnPredicate = c => (c.DataType == DataType.VARCHAR || c.DataType == DataType.CHAR) && c.CharacterMaxLength > LargeVarcharLength.Value;
        }

        public override string Name
        {
            get
            {
                return "Large Unfilled Text Columns";
            }
        }
        private class StatCount
        {
            public Column Column;
            public int TotalLength = 0;
            public int Items = 0;
            public int MaxLength = 0;
        }

        public override bool SkipTable(Table table)
        {
            return !table.Columns.Any(columnPredicate);
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var columns = table.Columns.Where(columnPredicate).Select(c => new StatCount { Column = c }).ToArray();
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var col in columns)
                    {
                        var val = row[col.Column.ColumnName];
                        if (val is DBNull || val.ToString().Equals(String.Empty))
                            continue;
                        var length = val.ToString().Length;
                        col.Items += 1;
                        col.MaxLength = Math.Max(col.MaxLength, length);
                        col.TotalLength += length;
                    }
                }
            foreach (var statCount in columns)
            {
                if (statCount.Items < this.MinRows.Value)
                    continue;
                var avgLength = statCount.TotalLength / statCount.Items;
                if (avgLength < statCount.Column.CharacterMaxLength * 0.6 && statCount.MaxLength < statCount.Column.CharacterMaxLength * 0.8)
                {
                    issueCollector.ReportIssue(new Issue(this, this.Severity)
                        {
                            Name = "Large Text Column not Filled",
                            Context = new TableContext(table),
                            Description = new Description("Column '{0}' contains on average {1} characters and the longest value has {2} characters. The maximum length of the column is declared to be {3} charaters", statCount.Column, avgLength, statCount.MaxLength, statCount.Column.CharacterMaxLength)
                        });
                }
            }
        }
    }
}
