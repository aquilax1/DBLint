using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.Data;

namespace DBLint.RuleControl
{
    public abstract class BaseDataProvider : BaseExecutable, IDataProvider
    {
        public abstract void Execute(DataTable table, IProviderCollection providers);

        public virtual bool SkipTable(Table table)
        {
            return false;
        }

        public virtual void Finalize(Database database, IProviderCollection providers)
        { }

        public virtual bool RunAlways
        {
            get { return false; }
        }
    }
}
