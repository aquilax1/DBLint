using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    public interface IConfigurable
    {
        IEnumerable<IProperty> GetProperties();
    }
}
