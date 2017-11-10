using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DBLint.RuleControl;
using DBLint.Model;

namespace DBLint.Rules.SchemaRules
{
    public class MissingColInSequence : BaseSchemaRule
    {
        public override string Name
        {
            get
            {
                return "Missing Column in a Sequences of Columns";
            }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables)
            {
                var colGroups = (from col in table.Columns
                                 let colName = Regex.Replace(col.ColumnName, @"\d+$", "")
                                 group col by colName into groups
                                 where groups.Count() > 1
                                 select groups);

                foreach (var colGroup in colGroups)
                {
                    int count = colGroup.Count();

                    var sorted = (from c in colGroup
                                  let match = Regex.Match(c.ColumnName, @"^.*?(\d+)$")
                                  let postfix = int.Parse((match.Groups.Count <= 1) ? "1" : match.Groups[1].Value)
                                  orderby postfix
                                  select new { Col = c, Postfix = postfix });

                    int? prev = null;
                    foreach (var col in sorted)
                    {
                        if (prev == null)
                            prev = col.Postfix;
                        else
                        {
                            if (prev + 1 != col.Postfix && sorted.Count() > 3)
                            {
                                var issue = new Issue(this, this.DefaultSeverity.Value);
                                issue.Name = "Missing Column(s) in a Sequence of Columns";
                                issue.Context = new TableContext(table);

                                var columnList = String.Join(", ", sorted.Select(s => s.Col.ColumnName));
                                if (columnList.Length > 20) {
                                    columnList = columnList.Substring(0, 20) + "..";
                                }

                                issue.Description = new Description("Incomplete sequence of columns ({0}) in table {1}", columnList, table);
                                issue.ExtendedDescription = new Description("The following sequence of columns is incomplete:\n{0}", sorted.Select(c => c.Col).ToList());
                                issueCollector.ReportIssue(issue);
                                break;
                            }
                            prev = col.Postfix;
                        }
                    }
                }
            }
        }
        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }

    }
}
