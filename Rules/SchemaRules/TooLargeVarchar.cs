using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.DataTypes;

namespace DBLint.Rules.SchemaRules
{
    public class TooLargeVarchar :BaseSchemaRule
    {
        public Property<int> LargeVarcharLength = new Property<int>("Maximum Length", 265, "Consider only varchar columns with a length larger than this value", i => i > 0);

        protected override Severity Severity
        {
            get { return Severity.Low; }
        }

        public override string Name
        {
            get { return "Too Large Varchar Columns"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var largeVarchars = (from col in database.Columns
                                 where col.DataType == DataType.VARCHAR && col.CharacterMaxLength > this.LargeVarcharLength.Value
                                 select col);
            foreach (var column in largeVarchars)
            {
                var issue = new Issue(this, this.DefaultSeverity.Value);
                issue.Name = "Too Large Varchar Column";
                issue.Context = new ColumnContext(column);
                issue.Description = new Description("Length of varchar column '{0}' in table '{1}' is {2}", column, column.Table, column.CharacterMaxLength);
                issueCollector.ReportIssue(issue);
            }
        }
    }
}
