using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class TablesWithZeroColumns : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Table With Too Few Columns"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables)
            {
                if (table.Columns.Count == 0)
                {
                    var issue = new Issue(this, this.DefaultSeverity.Value)
                        {
                            Name = string.Format("Table Without Columns"),
                            Context = new TableContext(table),
                            Description = new Description("Table {0} does not have any columns", table),
                        };
                    issueCollector.ReportIssue(issue);
                }
                else if (table.Columns.Count == 1)
                {
                    var issue = new Issue(this, this.DefaultSeverity.Value)
                    {
                        Name = string.Format("Table With Only One Column"),
                        Context = new TableContext(table),
                        Description = new Description("Table {0} has only a single column: '{1}'", table, table.Columns[0])
                    };
                    issueCollector.ReportIssue(issue);
                }
            }
        }
        protected override Severity Severity
        {
            get { return Severity.High; }
        }

    }
}
