using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;

namespace DBLint.IncrementalRuns
{
    public enum IssueStatus { New, Fixed }

    public class IssueDiff
    {
        public Issue Issue { get; private set; }
        public IssueStatus Status { get; private set; }
        public IEnumerable<DBLint.Model.TableID> Tables { get; private set; }

        public IssueDiff(Issue issue, IssueStatus status, IEnumerable<DBLint.Model.TableID> tableIDs)
        {
            this.Issue = issue;
            this.Status = status;
            this.Tables = tableIDs;
        }
    }
}
