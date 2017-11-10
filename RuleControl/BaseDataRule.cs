using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.Data;

namespace DBLint.RuleControl
{
    public abstract class BaseDataRule : BaseRule, IDataRule
    {
        public abstract bool SkipTable(Table table);
        public virtual void Finalize(Database database, IProviderCollection providers)
        {
        }

        public abstract void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers);
    }
}
