using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class MissingPrimaryKey : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Missing Primary Keys"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var tables = database.Tables.Where(t => t.PrimaryKey == null);
            foreach (var table in tables)
            {
                Issue issue = new Issue(this, this.DefaultSeverity.Value);
                issue.Name = "Missing Primary Key";
                issue.Context = new TableContext(table);
                issue.Description = new Description("Table {0} is missing a primary key", table);
                issueCollector.ReportIssue(issue);
            }
        }
        protected override Severity Severity
        {
            get { return Severity.Critical; }
        }

    }
}
