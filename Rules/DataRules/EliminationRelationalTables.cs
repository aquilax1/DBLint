using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.DataRules
{
    public class EliminationRelationalTables : BaseDataRule
    {
        public override string Name
        {
            get { return "Unnecessary One-to-One Relational Tables"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.Low; }
        }

        public override bool SkipTable(Table table)
        {
            //There must be exactly two FKs
            if (table.ForeignKeys.Count != 2)
                return true;
            //The foreign keys must point to two different tables
            if (TableID.TableEquals(table.ForeignKeys[0].PKTable, table.ForeignKeys[1].PKTable))
                return true;

            if (table.PrimaryKey == null)
                return true;
            //Both foreignkeys must be part of PK
            var fkColums = (from fk in table.ForeignKeys
                            from colPair in fk.ColumnPairs
                            select colPair.FKColumn);

            if (!fkColums.All(c => table.PrimaryKey.Columns.Contains(c)))
            {
                return true;
            }

            return false;
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var values = new Dictionary<Column, HashSet<Object>>();
            var columns = (from fk in table.ForeignKeys
                           from colPair in fk.ColumnPairs
                           select colPair.FKColumn).ToList();

            if (!columns.All(c => table.QueryableColumns.Contains(c)))
                return;

            columns.ForEach(c => values.Add(c, new HashSet<Object>()));

            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var column in columns)
                    {
                        Object val = row[column.ColumnName];
                        if (values[column].Contains(val))
                        {
                            return;
                        }
                        else
                        {
                            values[column].Add(val);
                        }
                    }
                }

            Issue issue = new Issue(this, this.Severity);
            issue.Name = "Unnecessary One-to-One Relational Table";
            issue.Context = new TableContext(table);
            issue.Description = new Description("The data in table {0} represents a one-to-one relationship.", table);
            issueCollector.ReportIssue(issue);
        }
    }
}
