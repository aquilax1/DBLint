using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class TooManyNulls : BaseSchemaRule
    {
        public Property<float> AllowedPercentage = new Property<float>("Allowed Nullable Percentage", 75f,
            "Percentage of nullable columns allowed in a table", v => v >= 0 && v <= 100f);
        public Property<Severity> HighPercentageProperty = new Property<Severity>("High Nullable Percentage Severity", Severity.Medium, "The severity for issues of tables with a high percentage of nullable columns");
        public override string Name
        {
            get { return "Too Many Nullable Columns"; }
        }

        protected override string DefaultSeverityName
        {
            get
            {
                return "All Columns Nullable Severity";
            }
        }

        protected override Severity Severity
        {
            get { return Severity.High; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            float allowedPercentage = AllowedPercentage.Value / 100f;
            foreach (var table in database.Tables.Where(tbl => tbl.Columns.Any(c => c.IsNullable)))
            {
                IEnumerable<Column> pkColumns = Enumerable.Empty<Column>();
                if (table.PrimaryKey != null)
                    pkColumns = table.PrimaryKey.Columns;

                int nullableCount = 0;
                int colsNotInPkCount = 0;

                foreach (var col in table.Columns)
                {
                    if (col.IsNullable)
                    {
                        nullableCount++;
                    }
                    else
                    {
                        if (!pkColumns.Contains(col))
                        {
                            colsNotInPkCount++;
                        }
                    }
                }


                if (nullableCount > 0 && colsNotInPkCount == 0 && pkColumns.Count() <= 1)
                {
                    issueCollector.ReportIssue(new Issue(this, this.DefaultSeverity.Value)
                                                   {
                                                       Name = "Too Many Nullable Columns",
                                                       Description = new Description("All columns in table {0} are nullable (except the primary key columns)", table),
                                                       ExtendedDescription = new Description("Consider adding not-null constraints."),
                                                       Context = new TableContext(table)
                                                   });
                }
                else if (nullableCount > 2 && nullableCount > table.Columns.Count * allowedPercentage)
                {
                    float percent = nullableCount * 100f / table.Columns.Count;
                    issueCollector.ReportIssue(new Issue(this, HighPercentageProperty.Value)
                    {
                        Name = "Too Many Nullable Columns",
                        Description = new Description("{1} percent of columns in table '{0}' are nullable. Consider adding more not-null constraints.", table, Math.Round(percent)),
                        Context = new TableContext(table)
                    });

                }
            }
        }
    }
}
