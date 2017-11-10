using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Model;
using DBLint.DataTypes;

namespace DBLint.Rules.SchemaRules
{
    public class WrongUseOfTextColumns : BaseSchemaRule
    {
        public Property<float> AllowedPercentage = new Property<float>("Allowed Percentage", 70f,
            "Percentage of text columns allowed in a table", v => v >= 0 && v <= 100f);

        public override string Name
        {
            get { return "Too Many Text Columns in a Table"; }
        }

        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }
        
        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            float allowedPercentage = AllowedPercentage.Value / 100f;
            foreach (var table in database.Tables)
            {
                var textColumns = table.Columns.Where(c => c.DataType == DataType.TEXT || c.DataType == DataType.LONGVARCHAR);
                float percent = ((float)textColumns.Count() / table.Columns.Count);
                if (textColumns.Count() > 2 && percent > allowedPercentage)
                {
                    var issue = new Issue(this, this.DefaultSeverity.Value);
                    issue.Name = "Too Many Text Column(s)";
                    issue.Context = new TableContext(table);
                    issue.Description = new Description("Overuse of data type text in table {0}", table);
                    issue.ExtendedDescription = new Description("The table contains {0} columns and {1} of them are of data type text. Consider using a more appropiate data type for one or more of the following columns:\n{2}",
                        table.Columns.Count, textColumns.Count(), GetTable(textColumns));
                    issueCollector.ReportIssue(issue);
                }
            }
        }

        private DataTable GetTable(IEnumerable<Column> columns)
        {
            var dt = new DataTable();
            dt.Columns.Add("Column Name");
            dt.Columns.Add("Column Data Type");
            foreach (var column in columns)
            {
                var row = dt.NewRow();
                row[0] = column.ColumnName;
                row[1] = column.DataType;
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}
