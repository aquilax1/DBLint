using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    public abstract class BaseRule : BaseExecutable
    {
        protected virtual string DefaultSeverityName { get { return "Severity"; } }
        /// <summary>
        /// The default severity for a rule rule
        /// </summary>
        protected abstract Severity Severity { get; }
        private Property<Severity> _defaultSeverity;
        public Property<Severity> DefaultSeverity
        {
            get
            {
                if (_defaultSeverity == null)
                    _defaultSeverity = new Property<Severity>(DefaultSeverityName, this.Severity, "The severity of issues from this rule");
                return _defaultSeverity;
            }
            set { _defaultSeverity = value; }
        }
    }
}
