using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    public delegate void NewIssueHandler(Issue issue);

    public interface IIssueCollector
    {
        void ReportIssue(Issue issue);
        event NewIssueHandler NewIssue;
    }
}
