using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.DataRules
{
    public class NoEmptyTable : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }

        public override string Name
        {
            get { return "Empty Tables"; }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality == 0)
                issueCollector.ReportIssue(new Issue(this, Severity.Medium)
                                               {
                                                   Name = "Empty Table",
                                                   Context = new TableContext(table),
                                                   Description = new Description("Table {0} is empty", table)
                                               });
        }
    }
}
