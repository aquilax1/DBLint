using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    /// <summary>
    /// The interface of a rule.
    /// </summary>
    public interface IRule : IExecutable
    {
        Property<Severity> DefaultSeverity { get; set; }
    }
}
