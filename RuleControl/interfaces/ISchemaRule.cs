using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    public interface ISchemaRule : IRule
    {
        void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers);
    }
}
