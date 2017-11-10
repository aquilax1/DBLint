using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;
using System.Text.RegularExpressions;

namespace DBLint.Rules.DataRules
{
    public class CommaSeparatedLists : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return Severity.Critical; }
        }

        public override string Name
        {
            get { return "Storing Lists in Varchar Columns"; }
        }

        public Property<String> validSubstringProp = new Property<String>("Format of list elements (regexp)", "^[ 0-9a-zA-Z]{1,15}$", "Elements of a list must conform to this format", CommaSeparatedLists.validRegex);
        public Property<String> separatorsProp = new Property<String>("List Separators", ",;#|", "Characters used to separate values");
        public Property<int> minLists = new Property<int>("Minimum Number of Lists", 5, "Number of lists in a column before reporting an issue");
        private Regex validSubstring;

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            this.validSubstring = new Regex(this.validSubstringProp.Value, RegexOptions.Compiled);

            char[] separators = this.separatorsProp.Value.ToCharArray();
            var textColumns = table.Columns.Where(c => DataTypes.DataTypesLists.TextTypes().Contains(c.DataType));
            var candidates = textColumns.Select(c => new Candidate(c)).ToList();
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var candidate in candidates.ToArray())
                    {
                        Object val = row[candidate.Column.ColumnName];

                        if (val is DBNull || (val is String) == false)
                            continue;

                        String str = (String)val;

                        bool isList = false;
                        var listSeparators = (candidate.Separator == default(char)) ? separators : new[] { candidate.Separator };
                        foreach (char sep in listSeparators)
                        {
                            String[] parts = str.Split(sep);
                            isList = parts.All(s => validSubstring.IsMatch(s.Trim()));
                            if (isList && parts.Count() > 1)
                            {
                                candidate.ListsDetected += 1;
                                candidate.Separator = sep;
                                if (candidate.Examples.Count < 5)
                                    candidate.Examples.Add(str);
                                break;
                            }
                        }
                        if (isList == false)
                            candidates.Remove(candidate);
                    }

                    if (candidates.Count == 0)
                        break;
                }

            foreach (var candidate in candidates)
            {
                if (candidate.ListsDetected < this.minLists.Value)
                    continue;

                System.Data.DataTable valuesTable = new System.Data.DataTable();
                valuesTable.Columns.Add("Value Examples", typeof(String));
                foreach (String ex in candidate.Examples)
                {
                    var row = valuesTable.NewRow();
                    row[0] = ex;
                    valuesTable.Rows.Add(row);
                }
                var etcRow = valuesTable.NewRow();
                etcRow[0] = "...";
                valuesTable.Rows.Add(etcRow);

                Issue issue = new Issue(this, this.Severity);
                issue.Name = "Storing Lists in Character Column";
                issue.Context = new ColumnContext(candidate.Column);
                issue.Description = new Description("Column '{0}' contains lists of values.", candidate.Column);
                issue.ExtendedDescription = new Description("{0}", valuesTable);
                issueCollector.ReportIssue(issue);
            }
        }

        public override bool SkipTable(Table table)
        {
            var textColumns = table.Columns.Where(c => DataTypes.DataTypesLists.TextTypes().Contains(c.DataType));
            return (textColumns.Count() == 0);
        }

        private class Candidate
        {
            public Column Column { get; set; }
            public int ListsDetected { get; set; }
            public List<String> Examples { get; set; }
            public char Separator { get; set; }
            public Candidate(Column column)
            {
                this.Column = column;
                this.ListsDetected = 0;
                this.Examples = new List<String>();
            }
        }

        private static bool validRegex(String regex)
        {
            try
            {
                new Regex(regex);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
