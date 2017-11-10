using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    class TooShortColumnName : BaseSchemaRule
    {
        public Property<int> MinimalCharactersInName = new Property<int>("Minimum Number of Characters", 2, "The minimum number of characters in a column's name.", c => c >= 0);

        public override string Name
        {
            get { return "Too Short Column Names"; }
        }
        protected override Severity Severity
        {
            get { return RuleControl.Severity.Medium; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var columns = database.Tables.SelectMany(t => t.Columns).Where(c => c.ColumnName.Length < this.MinimalCharactersInName.Value);

            foreach (var column in columns)
            {
                issueCollector.ReportIssue(new Issue(this, this.DefaultSeverity.Value)
                                               {
                                                   Name = "Too Short Column Name",
                                                   Description = new Description("Column '{0}' has a shorter name than allowed in table {1}.", column, column.Table),
                                                   Context = new ColumnContext(column)
                                               });
            }
        }
    }
}
