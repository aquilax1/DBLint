using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.DataTypes;

namespace DBLint.Rules.DataRules
{
    class TooManyNullFields : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return Severity.Low; }
        }

        public Property<int> MinRows = StandardProperties.MinimumRows(100);
        public Property<float> MaxPercentageNull = new Property<float>("Maxmimum Percentage of Nulls", 90, "The maximum percentage of tuples which is null", v => v >= 0 && v <= 100);
        public override string Name
        {
            get { return "Column Containing Too Many Nulls"; }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < MinRows.Value)
                return;

            var checkedColumns = table.QueryableColumns;
            if (checkedColumns.Count == 0)
                return;

            var columnNullCount = DictionaryFactory.CreateColumnID<int>();

            foreach (var col in checkedColumns)
                columnNullCount[col] = 0;

            int rowCount = 0;
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    rowCount++;
                    foreach (var col in checkedColumns)
                    {
                        var val = row[col.ColumnName];
                        if (val is DBNull)
                            columnNullCount[col] += 1;
                    }
                }

            var maxnullCount = this.MaxPercentageNull.Value * rowCount / 100f;

            var columnsWithTooManyNulls = from cp in columnNullCount
                                          where cp.Value > maxnullCount
                                          select cp;
            foreach (var columnWithTooManyNulls in columnsWithTooManyNulls)
            {
                var percentNull = Math.Round(columnWithTooManyNulls.Value * 100f / rowCount, 1);
                issueCollector.ReportIssue(new Issue(this, this.Severity)
                                               {
                                                   Name = "Column Containing Too Many Nulls",
                                                   Context = new ColumnContext(columnWithTooManyNulls.Key),
                                                   Description = new Description("Column '{0}' has {1} percent nulls", columnWithTooManyNulls.Key, percentNull)
                                               });
            }
        }
    }
}
