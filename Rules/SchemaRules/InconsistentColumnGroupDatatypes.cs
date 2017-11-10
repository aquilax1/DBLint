using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class InconsistentColumnGroupDatatypes : BaseSchemaRule
    {
        public override string Name
        {
            get
            {
                return "Inconsistent Data Types in Column Sequence";
            }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables)
            {
                var colGroups = (from col in table.Columns
                                 let colName = Regex.Replace(col.ColumnName, @"\d+$", "")
                                 group col by colName into colGroup
                                 where colGroup.Count() > 1
                                 select new { Columns = colGroup.ToList() }).ToList();

                var inconsistentGroups = (from colGroup in colGroups
                                          where !colGroup.Columns.TrueForAll(c => c.DataType == colGroup.Columns.First().DataType)
                                          select colGroup.Columns).ToList();

                foreach (var incGroup in inconsistentGroups)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("Column Name");
                    dt.Columns.Add("Data Type");
                    foreach (var col in incGroup.Select(c => c))
                    {
                        var row = dt.NewRow();
                        row[0] = col.ColumnName;
                        row[1] = col.DataType;
                        dt.Rows.Add(row);
                    }

                    var issue = new Issue(this, this.DefaultSeverity.Value);
                    issue.Name = this.Name;
                    issue.Context = new TableContext(table);
                    issue.Description = new Description("Sequence of related columns in table '{0}' do not have the same data type", table);
                    issue.ExtendedDescription = new Description("Inconsistent data types for the following columns:\n{0}", dt);
                    issueCollector.ReportIssue(issue);
                }
            }
        }
        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }

    }
}
