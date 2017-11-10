using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class SameNameDifferentDatatypes : BaseSchemaRule
    {
        public Property<String> IgnoreName;

        public SameNameDifferentDatatypes()
        {
            this.IgnoreName = new Property<String>("Ignore Column Names", "value,name,type,content",
                "Comma-separated list of column names that will be excluded from the analysis", ValidateNameList);
        }

        private static IEnumerable<string> GetIgnoredNames(string names)
        {
            var ignoredNames = from s in names.Split(',')
                               where !string.IsNullOrWhiteSpace(s)
                               select s.Trim().ToLower();
            return ignoredNames;
        }

        private static bool ValidateNameList(string names)
        {
            var list = GetIgnoredNames(names);
            return !list.Any(n => n.Length == 0);
        }

        public override string Name
        {
            get { return "Different Data Types for Columns With the Same Name"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var ignoredNames = GetIgnoredNames(this.IgnoreName.Value).ToDictionary(n => n);

            var nameGroups = from col in database.Columns
                             where !ignoredNames.ContainsKey(col.ColumnName.ToLower())
                             group col by col.ColumnName into colgroup
                             select colgroup;

            foreach (var columnGroup in nameGroups)
            {
                var datatypeGroups = (from column in columnGroup
                                      group column by new { column.DataType, column.CharacterMaxLength, column.NumericPrecision, column.NumericScale } into datatypeGroup
                                      orderby datatypeGroup.Count() descending
                                      select datatypeGroup).ToArray();

                if (datatypeGroups.Length > 1)
                {
                    var totalCount = datatypeGroups.Sum(g => g.Count());

                    var issue = new Issue(this, this.DefaultSeverity.Value);
                    issue.Name = this.Name;

                    var context = from dtgroup in datatypeGroups
                                  where dtgroup.Count() <= totalCount / 2
                                  select dtgroup;

                    var contextColumns = context.SelectMany(group => group).ToArray();
                    issue.Context = IssueContext.Create(contextColumns);

                    var tbl = new DataTable();
                    tbl.Columns.Add("Data Type");
                    tbl.Columns.Add("Table", typeof(IEnumerable<Table>));

                    var sb = new StringBuilder("Different data types for columns named '" + columnGroup.Key + "'. Data types: ");

                    foreach (var datatypeGroup in datatypeGroups)
                    {
                        var datatype = datatypeGroup.Key;
                        var count = datatypeGroup.Count();
                        string typeDescription;
                        if (datatype.NumericPrecision > 0 && datatype.NumericScale >= 0)
                            typeDescription = string.Format("{0}({1}, {2})", datatype.DataType, datatype.NumericPrecision, datatype.NumericScale);
                        else if (datatype.CharacterMaxLength > 0)
                            typeDescription = string.Format("{0}({1})", datatype.DataType, datatype.CharacterMaxLength);
                        else
                            typeDescription = datatype.DataType.ToString();
                        sb.AppendFormat("{0} ", typeDescription);

                        var tables = (from col in datatypeGroup
                                      select col.Table);
                        tbl.Rows.Add(typeDescription, tables);
                    }
                    issue.Description = new Description(sb.ToString());

                    issue.ExtendedDescription = new Description(string.Format("Data types for '{0}': ", columnGroup.Key) + "{0}", tbl);
                    issueCollector.ReportIssue(issue);
                }
            }

        }
        protected override Severity Severity
        {
            get { return Severity.Low; }
        }

    }
}
