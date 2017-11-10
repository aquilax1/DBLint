using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Data;
using DBLint.Model;

namespace DBLint.Rules.DataRules
{
    public class EnumSuggestion : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return Severity.Low; }
        }

        public override string Name
        {
            get { return "Column Values from a Small Domain"; }
        }

        public Property<int> MinRows = StandardProperties.MinimumRows(100);
        public Property<int> MaximumValues = new Property<int>("Maximum Value", 6, "Maximum number of unique values in a column", v => v > 20);
        public Property<int> MinimumValues = new Property<int>("Minimum Value", 2, "Minimum number of unique values in a column");

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < MinRows.Value)
                return;

            //Find candidates (text columnns)
            var textColumns = table.Columns.Where(c => c.DataType == DataTypes.DataType.VARCHAR ||
                                                  c.DataType == DataTypes.DataType.CHAR).ToList();
            var fk_columns = (from fks in table.ForeignKeys
                              from colpair in fks.ColumnPairs
                              select colpair.FKColumn);
            textColumns = textColumns.Where(c => fk_columns.Contains(c) == false).ToList();

            List<Candidate> candidates = textColumns.Select(c => new Candidate(c)).ToList();

            //Prune candidates
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (DataRow row in rowEnumerable)
                {
                    foreach (var candidate in candidates.ToArray())
                    {
                        Object value = row[candidate.Column.ColumnName];
                        if (value is DBNull || (value is String && ((String)value) == ""))
                            continue;

                        if (candidate.Values.ContainsKey(value))
                            continue;
                        else
                            candidate.Values.Add(value, 0);

                        if (candidate.Values.Count > this.MaximumValues.Value)
                        {
                            candidates.Remove(candidate);
                        }
                    }
                }

            //Report issues
            foreach (var candidate in candidates)
            {
                if (candidate.Values.Count < MinimumValues.Value)
                    break;

                System.Data.DataTable valuesTable = new System.Data.DataTable();
                valuesTable.Columns.Add("Values", typeof(String));
                foreach (Object value in candidate.Values.Keys)
                {
                    var row = valuesTable.NewRow();
                    row[0] = value.ToString();
                    valuesTable.Rows.Add(row);
                }

                Issue issue = new Issue(this, this.Severity);
                issue.Name = "Column Values from a Small Domain";
                issue.Description = new Description("Values in column '{0}' indicates that an enum could be appropriate", candidate.Column.ColumnName);
                issue.ExtendedDescription = new Description("{0} rows checked.\n\n{1}", table.Cardinality, valuesTable);
                issue.Context = new ColumnContext(candidate.Column);
                issueCollector.ReportIssue(issue);
            }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        private class Candidate
        {
            public Column Column { get; set; }
            public IDictionary<Object, int> Values { get; set; }

            public Candidate(Column column)
            {
                this.Column = column;
                this.Values = new Dictionary<Object, int>();
            }
        }
    }
}
