using DBLint.RuleControl;

namespace DBLint.DBLintGui
{
    public class RuleConfiguration
    {
        public readonly IExecutable Rule;
        public bool ScheduledForExecution { get; set; }
        public string Name { get; set; }
        public bool DataRule { get; set; }

        public RuleConfiguration(IExecutable rule)
        {
            this.Rule = rule;
        }
    }
}