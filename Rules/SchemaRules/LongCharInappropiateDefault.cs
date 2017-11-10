using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class LongCharInappropiateDefault : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Inappropriate Length of Default Value For Char Columns"; }
        }

        public Property<int> MaxLength = new Property<int>("Minimum Char Size", 20, 
            "Consider only char columns with a length larger than this value", v => v > 0);
        public Property<float> MinimumFill = new Property<float>("Minimum Fill", 50f, 
            "The percentage of the maximum length the default value must have", v => v >= 0 && v <= 100);

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            // Select only column of type 'char' and only if the max length is larger than 20 and
            // if the default value fills below 50% of the possible max length
            var columnsToCheck = (from col in database.Columns
                                  where col.DataType == DataType.CHAR
                                  && col.CharacterMaxLength > MaxLength.Value &&
                                  col.DefaultValue != null &&
                                  ((float)col.DefaultValue.Length / col.CharacterMaxLength) < (MinimumFill.Value / 100f)
                                  select col);

            foreach (var column in columnsToCheck)
            {
                var issue = new Issue(this, this.DefaultSeverity.Value);
                issue.Name = "Inappropriate Length of Default Value For Char Column";
                issue.Context = new ColumnContext(column);
                issue.Description = new Description("Inappropriate length of default value for char column '{0}' in table {1}", column, column.Table);
                issue.ExtendedDescription = new Description("The maximum character length of this column is {0}, but the default value has a length of {1}. Consider using varchar to save space.", column.CharacterMaxLength, column.DefaultValue.Length);
                issueCollector.ReportIssue(issue);
            }
        }
        protected override Severity Severity
        {
            get { return Severity.High; }
        }

    }
}
