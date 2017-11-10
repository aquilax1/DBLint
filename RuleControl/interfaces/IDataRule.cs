using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.Data;

namespace DBLint.RuleControl
{
    public interface IDataRule : IRule, IDataExecutable
    {
        void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers);
    }
}
