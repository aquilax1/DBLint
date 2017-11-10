using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Model;
using System.Data;

namespace DBLint.Rules.SchemaRules
{
    public class RedundantIndices : BaseSchemaRule
    {
        public override string Name
        {
            get
            {
                return "Redundant Indices";
            }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var tables = from tab in database.Tables
                         where tab.Indices.Count > 1
                         select tab;

            try
            {
                var start = DateTime.Now;
                foreach (var table in tables)
                {
                    var sortedIndices = table.Indices.OrderByDescending(t => t.Columns.Count).ToList();
                    List<Index> excludeIndex = new List<Index>();
                    List<Index> redundantIndices = new List<Index>();
                    for (int i = 0; i < sortedIndices.Count - 1; i++)
                    {
                        if (!excludeIndex.Contains(sortedIndices[i]))
                        {
                            for (int j = i + 1; j < sortedIndices.Count; j++)
                            {
                                var subIndexLen = sortedIndices[j].Columns.Count;
                                if (sortedIndices[i].Columns.ToList().GetRange(0, subIndexLen).SequenceEqual(sortedIndices[j].Columns))
                                {
                                    redundantIndices.Add(sortedIndices[j]);
                                    if (!excludeIndex.Contains(sortedIndices[j]))
                                        excludeIndex.Add(sortedIndices[j]);
                                }
                            }
                            if (redundantIndices.Count > 0)
                            {
                                var issue = new Issue(this, this.DefaultSeverity.Value);
                                issue.Name = "Redundant Index";
                                issue.Context = new TableContext(table);
                                issue.Description = new Description("Redundant indices for the index '{0}' in table {1}",
                                    sortedIndices[i], table);
                                issue.ExtendedDescription = new Description("The indices in the second table below are redundant to the index:\n\n{0}\n\nRedundant indices:\n\n{1}",
                                    GetIndexTable(new List<Index>() { sortedIndices[i] } ), GetIndexTable(redundantIndices));
                                issueCollector.ReportIssue(issue);
                            }
                        }
                        redundantIndices.Clear();
                    }
                }
                var end = DateTime.Now;
                //Console.WriteLine("redundant indices time: {0}", (end - start));
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("\tError in redundant indices:\n{0}", ex.Message));
                throw ex;
            }
        }

        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }


        public DataTable GetIndexTable(List<Index> indices)
        {
            var dt = new DataTable();
            dt.Columns.Add("Index Name");
            dt.Columns.Add("Ordered Index Columns", typeof(IEnumerable<Column>));
            foreach (var index in indices)
            {
                var row = dt.NewRow();
                row[0] = index.IndexName;
                row[1] = index.Columns;
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}
