using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.DataTypes;

namespace DBLint.Rules.DataRules
{
    public class WrongBooleanRepresentation : BaseDataRule
    {
        protected override Severity Severity
        {
            get { return Severity.Critical; }
        }

        public Property<float> DirtinessFactor = new Property<float>("Ignored Dirtiness Percentage", 1, "Allow a column to be detected as boolean, even though a percentage smaller than this number have non boolean values", v => v >= 0 && v <= 100);

        public override string Name
        {
            get { return "Wrong Representation of Boolean Values"; }
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        public override DependencyList Dependencies
        {
            get { return DependencyList.Create<InformationContent>(); }
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var dataTypes = new[] { DataType.CHAR, DataType.NCHAR, DataType.NVARCHAR, DataType.VARCHAR };

            var informationContent = providers.GetProvider<InformationContent>();

            var columnsToCheck = (from c in table.Columns
                                  where dataTypes.Contains(c.DataType) && informationContent[c] < 3 // Avoid checking columns with more than 8 unique values
                                  select c).ToArray();

            if (columnsToCheck.Length == 0)
                return;

            var columnNotBooleanCount = DictionaryFactory.CreateColumnID<int>();

            foreach (var col in columnsToCheck)
                columnNotBooleanCount[col] = 0;

            int rowCount = 0;
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    rowCount++;
                    foreach (var column in columnsToCheck)
                    {
                        var value = row[column.ColumnName];
                        if (value is DBNull || !Classifier.IsBool(value.ToString()))
                            columnNotBooleanCount[column] += 1;
                    }
                    // Foreach 128th row, check that all columns are likely to be boolean
                    if ((rowCount & 127) == 0)
                    {
                        var allowedDirtiness = rowCount * DirtinessFactor.Value / 100f;
                        columnsToCheck = columnsToCheck.Where(c => columnNotBooleanCount[c] < allowedDirtiness).ToArray();
                        if (columnsToCheck.Length == 0)
                            return;
                    }
                }
            foreach (var column in columnsToCheck)
            {
                var allowedDirtiness = rowCount * DirtinessFactor.Value / 100f;
                if (columnNotBooleanCount[column] < allowedDirtiness)
                {
                    issueCollector.ReportIssue(new Issue(this, this.Severity)
                                                   {
                                                       Name = "Text Column Used for Boolean Values",
                                                       Context = new ColumnContext(column),
                                                       Description = new Description("The column '{0}' contains boolean values. Consider using another data type", column),
                                                       Severity = this.Severity
                                                   });
                }
            }

        }
    }
}
