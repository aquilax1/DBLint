using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    class PrimaryAndUniqueColumnsMatch : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Primary- and Unique-key constraints are on the same columns"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.Medium; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables.Where(t => t.PrimaryKey != null && t.UniqueConstraints.Count > 0))
            {
                foreach (var uq in table.UniqueConstraints)
                {
                    if (uq.Columns.All(c => table.PrimaryKey.Columns.Contains(c)))
                    {
                        System.Data.DataTable descTable = new System.Data.DataTable();
                        descTable.Columns.Add("PK Column(s)", typeof(String));
                        descTable.Columns.Add("UK Column(s)", typeof(String));
                        var row = descTable.NewRow();
                        row[0] = String.Join("\n", table.PrimaryKey.Columns.Select(c => c.ColumnName));
                        row[1] = String.Join("\n", uq.Columns.Select(c => c.ColumnName));

                        descTable.Rows.Add(row);

                        if (uq.Columns.Count == table.PrimaryKey.Columns.Count)
                        {
                            issueCollector.ReportIssue(new Issue(this, this.DefaultSeverity.Value)
                                                           {
                                                               Context = new TableContext(table),
                                                               Description = new Description("Columns in the primary key in table '{0}' is the same as the columns in a unique constraint", table),
                                                               ExtendedDescription = new Description("{0}", descTable),
                                                               Name = this.Name
                                                           });
                        }
                        else 
                        {
                            issueCollector.ReportIssue(new Issue(this, this.DefaultSeverity.Value)
                            {
                                Context = new TableContext(table),
                                Description = new Description("The columns in a unique constraint in table '{0}' is a subset of the primary key", table),
                                ExtendedDescription = new Description("The primary key is a super key. Consider reducing the number of columns in the primary key.\n\n {0}", descTable),
                                Name = this.Name
                            });
                        }
                    }
                }
            }
        }
    }
}
