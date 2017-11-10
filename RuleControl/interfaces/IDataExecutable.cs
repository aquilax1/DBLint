using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    public interface IDataExecutable
    {
        bool SkipTable(Table table);
        void Finalize(Database database, IProviderCollection providers);
    }
}
