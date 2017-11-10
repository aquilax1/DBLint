using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class DeviantVarcharLength : BaseSchemaRule
    {
        public Property<int> threshold = new Property<int>("Threshold", 3, "Maximum difference between varchar lengths", v => v > 0);
        public Property<int> minLength = new Property<int>("Minimum Length", 20, "Minimum length of varchars to be considered for this rule", v => v > 0);

        public override string Name
        {
            get
            {
                return "Inconsistent Max Lengths of Varchar Columns";
            }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var schema in database.Schemas)
            {
                var columnGroups = (from col in schema.Columns
                                    where col.DataType == DataType.VARCHAR && col.CharacterMaxLength > minLength.Value
                                    orderby col.CharacterMaxLength ascending
                                    group col by col.CharacterMaxLength into g
                                    select g).ToList();

                var group = new List<List<Column>>();
                for (int i = 0; i < columnGroups.Count - 1; i++)
                {
                    if (group.Count == 0)
                        group.Add(columnGroups[i].ToList());
                    if (Math.Abs(columnGroups[i].First().CharacterMaxLength - columnGroups[i + 1].First().CharacterMaxLength) <= threshold.Value)
                    {
                        group.Add(columnGroups[i + 1].ToList());
                    }
                    else
                    {
                        if (group.Count > 1)
                            issueCollector.ReportIssue(GetIssue(group));
                        group.Clear();
                    }
                }
                if (group.Count > 1)
                    issueCollector.ReportIssue(GetIssue(group));
            }
        }

        public Issue GetIssue(List<List<Column>> groups)
        {
            var colCount = groups.SelectMany(v => v).Count();
            var dt = new DataTable();
            dt.Columns.Add("Number of Columns");
            dt.Columns.Add("Percentage");
            foreach (var group in groups)
            {
                var row = dt.NewRow();
                row[0] = String.Format("{0} columns have length {1}", group.Count, group.First().CharacterMaxLength);
                row[1] = String.Format("{0}%", Math.Round((100.0 / colCount) * group.Count, 2));
                dt.Rows.Add(row);
            }

            Description description = null;
            if (groups.Count == 2)
            {
                var firstGroupCount = groups[0].Count;
                var secondGroupCount = groups[1].Count;
                var firstGroupLength = groups[0].First().CharacterMaxLength;
                var secondGroupLength = groups[1].First().CharacterMaxLength;

                description = new Description("Inconsistent lengths of varchar columns. {0} columns have length {1} and {2} columns have length {3}", firstGroupCount, firstGroupLength, secondGroupCount, secondGroupLength);
            }
            else
            {
                description = new Description("Inconsistent lengths of {0} varchar columns.", colCount);
            }

            var contextTables = groups.SelectMany(gt => gt.Select(g => g.Table)).Distinct().ToList();
            var issue = new Issue(this, this.DefaultSeverity.Value)
                            {
                                Name = this.Name, 
                                Context = IssueContext.Create(contextTables), 
                                Description = description, 
                                ExtendedDescription = new Description("{0}", dt)
                            };
            return issue;
        }
        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }

    }
}
