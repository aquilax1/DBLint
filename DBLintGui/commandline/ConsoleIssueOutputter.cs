using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using DBLint.RuleControl;
using DBLint.ReportGeneration;

namespace DBLint.DBLintGui
{
    public static class TextIssueOutputter
    {
        public static void OutputIssues(IExecutionSummary summary, TextWriter output)
        {
            if (summary.RuleSummaries.Count() == 0)
            {
                output.WriteLine("DBLint: No issues found");

            }
            else
            {
                int totalIssueCount = summary.RuleSummaries.Sum(rs => rs.Issues.Count());

                divider("List of issues", output);

                int issueCount = 0;

                foreach (RuleSummary s in summary.RuleSummaries)
                {
                    foreach (Issue i in s.Issues)
                    {
                        issueCount++;
                        output.WriteLine("Issue {0}: {1}", issueCount.ToString().PadLeft(totalIssueCount.ToString().Length), i.Description);
                    }
                }

                divider("Summary", output);
                
                foreach (var ruleSummary in summary.RuleSummaries)
                {
                    if (ruleSummary.Issues.Count() > 0)
                    {
                        output.WriteLine("{0} issues in rule: {1}", ruleSummary.Issues.Count().ToString().PadLeft(totalIssueCount.ToString().Length), ruleSummary.Rule.Name);
                    }
                }
            }
        }

        private static void divider(string title, TextWriter output)
        {
            output.WriteLine();
            Console.WriteLine("---------------------- " + (title + " ").ToString().PadRight(30, '-'));
            output.WriteLine();
        } 
    }
}
