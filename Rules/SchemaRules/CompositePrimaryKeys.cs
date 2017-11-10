using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    /*
    public class CompositePrimaryKeys : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Composite primary key without foreignkeys"; }
        }
        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables)
            {
                if (table.PrimaryKey == null || table.PrimaryKey.Columns.Count == 1)
                    continue;

                List<Column> columnsNotInForeignKeys = new List<Column>();
                foreach (var column in table.PrimaryKey.Columns)
                {
                    var isUsedInForeignKey = table.ForeignKeys.Any(fk => fk.ColumnPairs.Any(pair => pair.FKColumn.Equals(column)));
                    if (!isUsedInForeignKey)
                        columnsNotInForeignKeys.Add(column);
                }
                if (columnsNotInForeignKeys.Count > 0)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("Colum", typeof(Column));
                    columnsNotInForeignKeys.ForEach(c => dt.Rows.Add(c));
                    var issue = new Issue(this, this.DefaultSeverity.Value)
                                    {
                                        Name = "Composite Primary Key, without foreign keys",
                                        Context = new TableContext(table),
                                        Description = new Description("{0} column(s) are not used in foreign keys", columnsNotInForeignKeys.Count),
                                        ExtendedDescription = new Description("The following columns are not used in foreign keys {0}", dt)
                                    };
                    issueCollector.ReportIssue(issue);
                }
            }
        }

        protected override Severity Severity
        {
            get { return Severity.Low; }
        }
    }
    */
}
