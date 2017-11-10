using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class PrimaryKeyFirst : BaseSchemaRule
    {
        public override string Name
        {
            get
            {
                return "Primary-Key Columns Not Positioned First";
            }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var tables = (from tab in database.Tables
                          where tab.PrimaryKey != null
                          select tab).ToList();

            foreach (var table in tables)
            {
                for (int i = 0; i < table.PrimaryKey.Columns.Count; i++)
                {
                    if (table.PrimaryKey.Columns[i] != table.Columns[i])
                    {
                        var issue = new Issue(this, this.DefaultSeverity.Value);
                        issue.Name = "Primary-Key Column(s) Not Positioned First";
                        issue.Description = new Description("The columns in primary key in table {0} should be positioned first, and in the same order as the primary-key index", table);
                        issue.Context = new TableContext(table);
                        issueCollector.ReportIssue(issue);
                        break;
                    }
                }
            }
        }

        protected override Severity Severity
        {
            get { return Severity.Low; }
        }
    }
}
