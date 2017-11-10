using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class NullableAndUnique : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Nullable and Unique Columns"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var tbl in database.Tables)
            {
                foreach (var column in tbl.Columns.Where(c => c.IsNullable && c.Unique))
                {
                    issueCollector.ReportIssue(new Issue(this, this.DefaultSeverity.Value)
                                                   {
                                                       Name = "Nullable and Unique Column",
                                                       Description = new Description("Column '{0}' in table '{1}' is both nullable and unique", column, column.Table),
                                                       Context = new ColumnContext(column)
                                                   });
                }
            }
        }

        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }

    }
}
