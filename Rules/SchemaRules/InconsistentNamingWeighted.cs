using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.Rules;
using DBLint.Rules.Naming;

namespace DBLint.Rules.SchemaRules
{
    public class InconsistentNamingWeighted : BaseSchemaRule
    {
        private string nonExisting = "Non-Existing Naming Convention";
        private string inconsistent = "Inconsistent Naming Convention";
        public Property<DBLint.DataTypes.NamingConventionRepresentation> NamingConventionRepresentation = new Property<DBLint.DataTypes.NamingConventionRepresentation>("Representation Method", DBLint.DataTypes.NamingConventionRepresentation.Markov, "The representation used to represent the naming convention");
        public override string Name
        {
            get { return "Inconsistent Naming Convention"; }
        }

        public override DependencyList Dependencies
        {
            get
            {
                return new DependencyList(typeof(SchemaProviders.ImportanceProvider));
            }
        }

        protected override Severity Severity
        {
            get { return Severity.High; }
        }

        Property<Severity> NoNamingConventionSeverity = new Property<Severity>("No Naming Convention Severity", RuleControl.Severity.Critical, "Severity for undetectable naming convention");

        public Property<float> InvalidThreshold = new Property<float>("Maximum Invalid Percentage", 30f,
            "A naming convention is non-existent if the percentage of invalid names is above this value", v => v >= 0.0 && v <= 100);
        public Property<float> MarkovTolerance = new Property<float>("Markov Tolerance", 30f,
            "Tolerance in percentage of the Markov Chain used to represent naming conventions. If a path with lower probability than this is followed an issues will be raised", v => v >= 0.0 && v <= 100);

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (database.Tables.Count == 0)
                return;

            float invalidThreshold = this.InvalidThreshold.Value / 100f;

            int columnsTotal = database.Columns.Count;
            INameConventionDetector naming;
            if (this.NamingConventionRepresentation.Value == DataTypes.NamingConventionRepresentation.Markov)
                naming = new MarkovConventionDetector(MarkovTolerance.Value / 100f);
            else
                naming = new TrieNameDetector(30);
            var columnNames = new List<String>();
            var importance = providers.GetProvider<SchemaProviders.ImportanceProvider>();
            foreach (var column in database.Columns)
            {
                double tableRank = importance[column.Table];
                int weight;
                if (tableRank < 1)
                    weight = 1;
                else
                    weight = (int)(tableRank);

                //for (int i = 0; i < weight; i++)
                //    columnNames.Add(column.ColumnName);
                columnNames.Add(column.ColumnName);
            }

            Regex numPat = new Regex("[0-9]");
            columnNames.RemoveAll(name => numPat.Match(name).Success);

            //Detect convention
            bool detected = naming.DetectConvention(columnNames);
            var invalidColumns = (from col in database.Columns
                                  where naming.IsValid(col.ColumnName) == false
                                  select col).ToList();
            float percentInvalid = (float)invalidColumns.Count / columnsTotal;
            if (detected == false || percentInvalid > invalidThreshold)
            {
                var issue = new Issue(this, NoNamingConventionSeverity.Value);
                issue.Name = this.nonExisting;
                issue.Context = new DatabaseContext(database);
                issue.Description = new Description("Unable to find a naming convention for columns");
                issueCollector.ReportIssue(issue);
            }
            else
            {
                //Raise an issue for all columns that don't use the convention
                foreach (Column col in invalidColumns)
                {
                    var issue = new Issue(this, this.DefaultSeverity.Value);
                    issue.Name = this.inconsistent;
                    issue.Context = new ColumnContext(col);
                    issue.Description = new Description("Column '{0}' in table {1} does not follow the naming convention", col, col.Table);
                    issueCollector.ReportIssue(issue);
                }
            }

            //Tables
            if (this.NamingConventionRepresentation.Value == DataTypes.NamingConventionRepresentation.Markov)
                naming = new MarkovConventionDetector(MarkovTolerance.Value / 100f);
            else
                naming = new TrieNameDetector(30);

            var tableNames = database.Tables.Select(t => t.TableName);
            detected = naming.DetectConvention(tableNames);

            var invalidTables = (from table in database.Tables
                                 where naming.IsValid(table.TableName) == false
                                 select table).ToList();
            float percentInvalidTables = (float)invalidTables.Count / database.Tables.Count;
            if (detected == false || percentInvalidTables > invalidThreshold)
            {
                var issue = new Issue(this, NoNamingConventionSeverity.Value);
                issue.Name = this.nonExisting;
                issue.Context = new DatabaseContext(database);
                issue.Description = new Description("Unable to find a naming convention for tables");
                issueCollector.ReportIssue(issue);
            }
            else
            {
                foreach (Table table in invalidTables)
                {
                    var issue = new Issue(this, this.DefaultSeverity.Value);
                    issue.Name = this.inconsistent;
                    issue.Context = new TableContext(table);
                    issue.Description = new Description("Table name '{0}' does not follow the naming convention", table);
                    issueCollector.ReportIssue(issue);
                }
            }
        }
    }
}
