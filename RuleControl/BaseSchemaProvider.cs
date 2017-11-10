using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    public abstract class BaseSchemaProvider : BaseExecutable, ISchemaProvider
    {
        public abstract void Execute(Database database, IProviderCollection providers);

        public virtual bool RunAlways
        {
            get { return false; }
        }
    }
}
