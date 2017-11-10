using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    public abstract class BaseExecutable : BaseConfigurable, IExecutable
    {
        public abstract String Name { get; }

        public virtual DependencyList Dependencies
        {
            get { return null; }
        }
    }
}
