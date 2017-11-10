using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Data;
using DBLint.DataTypes;
using DBLint.Model;

namespace DBLint.Rules
{
    /*
    public class DataRule : BaseDataRule
    {
        public override bool SkipTable(Table table)
        {
            return false;
        }

        public override string Name
        {
            get { return "Test rule"; }
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            try
            {
                foreach (DataRow dr in table)
                {
                    foreach (var col in table.Columns)
                    {
                        var tt = dr[col.ColumnName];
                    }
                }
            }
            catch (Exception ex)
            {
                var issue = new Issue();
                issue.Name = "Error while looping over all data";
                issue.Description = new Description("There was an error while looping over all data in the table {0}", table);
                issue.ExtendedDescription = new Description(ex.Message);
                issue.Severity = Severity.Critical;
                issue.Context = table;
                issueCollector.ReportIssue(issue);
            }
        }
    }
     */
}
