using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class VarcharsWithZeroLength : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Varchars Columns With Length Zero"; }
        }

        protected override Severity Severity
        {
            get { return Severity.Critical; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables)
            {
                var textColumns = table.Columns.Where(c => c.DataType == DataType.VARCHAR && c.CharacterMaxLength == 0);
                foreach (var column in textColumns)
                {
                    var issue = new Issue(this, this.DefaultSeverity.Value);
                    issue.Name = "Varchar Column With Length Zero";
                    issue.Context = new TableContext(table);
                    issue.Description = new Description("Column '{0}' in table {1} is a varchar column with length zero", column, table);
                    issueCollector.ReportIssue(issue);
                }
            }
        }
    }
}
