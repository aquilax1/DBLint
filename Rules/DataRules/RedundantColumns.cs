using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Model;
using DBLint.Data;

namespace DBLint.Rules.DataRules
{
    public class RedundantColumns : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return Severity.High; }
        }

        public Property<int> MinRows = StandardProperties.MinimumRows(100);

        public override string Name
        {
            get { return "Redundant Columns"; }
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < this.MinRows.Value)
                return;

            List<ColumnPair> candidates = this.getCandidates(table);
            //Prune candidates
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (DataRow row in rowEnumerable)
                {
                    foreach (var candidate in candidates.ToArray())
                    {
                        Object val1 = row[candidate.Column1.ColumnName];
                        Object val2 = row[candidate.Column2.ColumnName];

                        //If both are either null or empty string, skip
                        bool val1_null = (val1 is DBNull || (val1 is String && ((String)val1) == ""));
                        bool val2_null = (val2 is DBNull || (val2 is String && ((String)val2) == ""));
                        if (val1_null && val2_null)
                        {
                            candidate.Ignored++;
                            continue;
                        }

                        //If values are not equal, remove candidate
                        if (val1.Equals(val2) == false)
                        {
                            candidates.Remove(candidate);
                            continue;
                        }

                        if (candidate.Values.Count <= 3)
                            if (!candidate.Values.Contains(val1))
                                candidate.Values.Add(val1);
                    }

                    if (candidates.Count == 0)
                        break;
                }
            //Remaining candidates are redundant
            foreach (var redundantPair in candidates)
            {
                //Skip pairs that has few unique values
                if (redundantPair.Values.Count <= 3)
                    continue;
                //Skip pairs that has too many null value pairs
                var nullPercent = (float)redundantPair.Ignored / table.Cardinality;
                if (nullPercent > 0.8)
                    continue;

                Issue i = new Issue(this, this.Severity);
                i.Name = "Redundant Column";
                i.Description = new Description("The columns '{0}' and '{1}' contains the same values", redundantPair.Column1, redundantPair.Column2);
                i.Context = new TableContext(table);
                issueCollector.ReportIssue(i);
            }
        }

        private List<ColumnPair> getCandidates(DataTable table)
        {
            List<ColumnPair> candidates = new List<ColumnPair>();
            for (int i = 0; i < table.QueryableColumns.Count; i++)
            {
                for (int j = i + 1; j < table.QueryableColumns.Count; j++)
                {
                    Column c1 = table.QueryableColumns[i];
                    Column c2 = table.QueryableColumns[j];

                    if (c1.DataType != c2.DataType)
                        continue;

                    candidates.Add(new ColumnPair(c1, c2));
                }
            }
            return candidates;
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        private class ColumnPair
        {
            public Column Column1 { get; private set; }
            public Column Column2 { get; private set; }
            public int Ignored = 0;
            public List<Object> Values { get; private set; }

            public ColumnPair(Column c1, Column c2)
            {
                this.Column1 = c1;
                this.Column2 = c2;
                this.Values = new List<Object>();
            }
        }
    }
}
