using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class TooLongColumnNames : BaseSchemaRule
    {
        public Property<int> MaxLength = new Property<int>("Maximum Length", 30, "Maximum allowed length of column names", i => i > 0);

        public override string Name
        {
            get { return "Too Long Column Names"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var longColumNames = database.Columns.Where(c => c.ColumnName.Length > this.MaxLength.Value);
            foreach (var column in longColumNames)
            {
                var issue = new Issue(this, this.DefaultSeverity.Value);
                issue.Name = "Too Long Column Name";
                issue.Context = new ColumnContext(column);
                issue.Description = new Description("The column name '{0}' in table {1} is too long (length: {2})", column, column.Table, column.ColumnName.Length);
                issueCollector.ReportIssue(issue);
            }
        }
        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }

    }
}