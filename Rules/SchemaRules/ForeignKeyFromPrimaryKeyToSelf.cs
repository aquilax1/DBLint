using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    class ForeignKeyFromPrimaryKeyToSelf : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Self-Referencing Primary Key"; }
        }

        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables.Where(t => t.ForeignKeys.Count > 0 && t.PrimaryKey != null))
            {
                foreach (var fk in table.ForeignKeys.Where(fk => TableID.TableEquals(fk.PKTable, fk.FKTable)))
                {
                    bool isContained = true;
                    foreach (var pair in fk.ColumnPairs)
                    {
                        if (!table.PrimaryKey.Columns.Contains(pair.FKColumn))
                            isContained = false;
                    }

                    if (isContained)
                    {
                        issueCollector.ReportIssue(new Issue(this, DefaultSeverity.Value)
                            {
                                Name = this.Name,
                                Context = new TableContext(table),
                                Description = new Description("The primary key '{0}' in table '{1}' references itself", table.PrimaryKey.PrimaryKeyName, table),
                                ExtendedDescription = new Description("Having a self-referencing primary key is likely not what is intended."),
                            });
                    }
                }
            }
        }
    }
}
