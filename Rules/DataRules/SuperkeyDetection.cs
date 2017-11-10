using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.DataRules
{
    public class SuperkeyDetection : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return Severity.High; }
        }

        public Property<int> MinRows = StandardProperties.MinimumRows(100);
        public override string Name
        {
            get { return "Defined Primary Key is not a Minimal Key"; }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        public override DependencyList Dependencies
        {
            get
            {
                return DependencyList.Create<InformationContent>();
            }
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var informationContent = providers.GetProvider<InformationContent>();
            if (table.PrimaryKey == null || table.Cardinality < MinRows.Value)
                return;
            var pkcolumns = table.PrimaryKey.Columns;
            var escaper = table.Database.Escaper;
            // Heuristic: Only check columns storing a lot of entropy

            var entropyOrderedCOlumns = pkcolumns.OrderByDescending(col => informationContent[col]).ToArray();

            double maxPossibleCardinality = 1;
            var currentColumns = new List<Column>(entropyOrderedCOlumns.Length);
            var first = true;
            var currentColumnsString = new StringBuilder();
            foreach (var col in entropyOrderedCOlumns)
            {
                currentColumns.Add(col);
                if (currentColumns.Count == entropyOrderedCOlumns.Length)
                    break; // Last column added. It is known to be a key.

                maxPossibleCardinality *= informationContent[col];
                if (table.Cardinality - maxPossibleCardinality > 0.2f)
                    continue; // If not enough entropy to generate a higher card. no need to query the data.

                var escapedCol = escaper.Escape(col);
                if (first)
                {
                    currentColumnsString.AppendFormat("{0}", escapedCol);
                    first = false;
                }
                else
                {
                    currentColumnsString.AppendFormat(", {0}", escapedCol);
                }

                var query = string.Format(@"SELECT  COUNT(*)
                                            FROM    ( SELECT    COUNT(*) AS rowcnt
                                                      FROM      {0}
                                                      GROUP BY  {1}
                                                    ) AS exp1
                                            WHERE   rowcnt > 1 ", escaper.Escape(table), currentColumnsString.ToString());
                var res = table.QueryTable(query);
                if (res is DBNull)
                    break;

                var num = Convert.ToInt32(res);
                if (num == 0)
                {
                    issueCollector.ReportIssue(new Issue(this, this.Severity)
                                                   {
                                                       Name = "Defined Primary Key is not a Minimal Key",
                                                       Context = new TableContext(table),
                                                       Description = new Description("Primary key for table {0}, is a superkey.", table),
                                                       ExtendedDescription = new Description("Columns {0} are enough to uniquely identify a tuple. Currently used are {1}", currentColumns, table.PrimaryKey.Columns),
                                                   });
                    break;
                }
            }
        }
    }
}
