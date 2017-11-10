using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class RedundantForeignKeys : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Redundant Foreign Keys"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.High; }
        }

        private class RFK
        {
            public List<Column> SourceColumns = new List<Column>();
            public List<Column> TargetColumns = new List<Column>();
            public List<ForeignKey> ForeignKeys = new List<ForeignKey>();
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables)
            {
                List<RFK> redundants = new List<RFK>();
                foreach (var fk in table.ForeignKeys)
                { 
                    var s_columns = fk.ColumnPairs.Select(p => p.FKColumn).ToList();
                    var t_columns = fk.ColumnPairs.Select(p => p.PKColumn).ToList();
                    var re = redundants.Where(r => r.SourceColumns.SequenceEqual(s_columns) &&
                                          r.TargetColumns.SequenceEqual(t_columns)).FirstOrDefault();
                    if (re != null)
                        re.ForeignKeys.Add(fk);
                    else
                    {
                        var rfk = new RFK() { SourceColumns = s_columns, TargetColumns = t_columns };
                        rfk.ForeignKeys.Add(fk);
                        redundants.Add(rfk);
                    }
                }

                foreach (var rfk in redundants)
                {
                    if (rfk.ForeignKeys.Count <= 1)
                        continue;

                    var descTable = new System.Data.DataTable();
                    descTable.Columns.Add("Name");
                    descTable.Columns.Add("Source Column(s)", typeof(String));
                    descTable.Columns.Add("Target Column(s)", typeof(String));
                    foreach (var fk in rfk.ForeignKeys)
                    {
                        var row = descTable.NewRow();
                        row[0] = fk.ForeignKeyName;
                        row[1] = String.Join("\n", fk.ColumnPairs.Select(p => p.FKColumn.ColumnName));
                        row[2] = String.Join("\n", fk.ColumnPairs.Select(p => p.PKColumn.Table.TableName + "." + p.PKColumn.ColumnName));
                        descTable.Rows.Add(row);
                    }

                    Issue issue = new Issue(this, this.Severity);
                    issue.Name = "Redundant Foreign Key";
                    issue.Context = new TableContext(table);
                    issue.Description = new Description("Redundant foreign key in table {0}", table);
                    issue.ExtendedDescription = new Description("{0}", descTable);
                    issueCollector.ReportIssue(issue);
                }
            }
        }
    }
}
