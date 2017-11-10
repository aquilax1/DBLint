using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DBLint.Model;

namespace DBLint.RuleControl
{
    public enum Severity { Critical, High, Medium, Low };

    /// <summary>
    /// Models an issue description
    /// </summary>
    public class Description
    {
        public String Text { get; private set; }
        public Object[] Parameters { get; private set; }

        public Description(String text, params Object[] parameters)
        {
            String.Format(text, parameters);
            this.Text = text;
            this.Parameters = parameters;
        }

        public override string ToString()
        {
            return String.Format(this.Text, this.Parameters);
        }
    }

    public class Issue
    {
        public IRule Rule { get; set; }
        /// <summary>
        /// Name of the issue, e.g. 'Missing primary key'
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// Issue description, should be approximately 1 line
        /// </summary>
        public Description Description { get; set; }
        /// <summary>
        /// Extended issue description
        /// </summary>
        public Description ExtendedDescription { get; set; }
        /// <summary>
        /// Issue severity
        /// </summary>
        public Severity Severity { get; set; }
        /// <summary>
        /// Where the issue was found. Can e.g. be a table, a column, a list of tables, etc.
        /// </summary>
        /// 
        public IssueContext Context { get; set; }

        public Issue(IRule rule, Severity severity)
        {
            this.Rule = rule;
            this.Severity = severity;
        }
    }
}