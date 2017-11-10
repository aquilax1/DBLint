using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    public interface IExecutionSummary
    {
        TimeSpan ExecutionTime { get; }
        IEnumerable<RuleSummary> RuleSummaries { get; }
        RuleSummary GetSummary(IRule rule);
    }

    public enum FinishedStatus { Success, Failed, Ignored }

    public class RuleSummary
    {
        public IRule Rule { get; private set; }
        public FinishedStatus Status { get; private set; }
        public TimeSpan ExecutionTime { get; private set; }
        public IEnumerable<Issue> Issues { get; set; }

        public RuleSummary(IRule rule, IEnumerable<Issue> issues, FinishedStatus status, TimeSpan execTime)
        {
            this.Rule = rule;
            this.Issues = issues;
            this.Status = status;
            this.ExecutionTime = execTime;
        }
    }

    public class ExecutionSummaryImpl : IExecutionSummary
    {
        public TimeSpan ExecutionTime { get; set; }
        public List<RuleSummary> ruleSummaries = new List<RuleSummary>();
        public IEnumerable<RuleSummary> RuleSummaries
        {
            get { return this.ruleSummaries; }
        }


        public void AddRuleSummary(RuleSummary summary)
        {
            lock (this.RuleSummaries)
            {
                this.ruleSummaries.Add(summary);
            }
        }

        public RuleSummary GetSummary(IRule rule)
        {
            return this.ruleSummaries.Where(s => s.Rule == rule).FirstOrDefault();
        }
    }
}
