using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class ForeignKeyWithoutIndex : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Foreign Key Without Index"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables)
            {
                foreach (var fk in table.ForeignKeys)
                {
                    bool doesIndexExist = table.Indices.Any(ix =>
                                                                 {
                                                                     if (ix.Columns.Count < fk.ColumnPairs.Count)
                                                                         return false;
                                                                     for (int i = 0; i < fk.ColumnPairs.Count; i++)
                                                                     {
                                                                         var fkColumn = fk.ColumnPairs[i].FKColumn;
                                                                         if (!ix.Columns[i].Equals(fkColumn))
                                                                             return false;
                                                                     }
                                                                     return true;
                                                                 });
                    if (!doesIndexExist)
                    {
                        var dt = new DataTable();
                        dt.Columns.Add("Column", typeof(Column));
                        foreach (var col in fk.ColumnPairs)
                            dt.Rows.Add(col.FKColumn);
                        issueCollector.ReportIssue(new Issue(this, this.DefaultSeverity.Value)
                                                       {
                                                           Name = "Missing Index on Foreign-Key Column(s)",
                                                           Context = new TableContext(table),
                                                           Description = new Description("Missing index on the columns used in foreign-key '{0}' in table '{1}'", fk.ForeignKeyName, table),
                                                           ExtendedDescription = new Description("{0}", dt)
                                                       });
                    }
                }
            }
        }
        protected override Severity Severity
        {
            get { return Severity.Low; }
        }

    }
}
