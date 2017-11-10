using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class ReservedWords : BaseSchemaRule
    {
        private enum SqlStandard { SQL99, SQL92, BOTH };
        private Dictionary<string, SqlStandard> reservedWords;

        public override string Name
        {
            get { return "Use of Reserved Words From SQL"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            this.BuildReservedWordList();
            var ignorePrimaryKeyNames = ((from table in database.Tables
                where table.PrimaryKey != null && table.PrimaryKey.PrimaryKeyName.ToLower() == "primary"
                select table).Count() == database.Tables.Where(p => p.PrimaryKey != null).Count());
            foreach (var schema in database.Schemas)
            {
                /*if (CheckString(schema.SchemaName))
                    issueCollector.ReportIssue(GetIssue(schema));*/

                foreach (var table in schema.Tables)
                {
                    if (CheckString(table.TableName))
                        issueCollector.ReportIssue(GetIssue(table));

                    foreach (var column in table.Columns)
                    {
                        if (CheckString(column.ColumnName))
                            issueCollector.ReportIssue(GetIssue(column));
                    }

                    if (!ignorePrimaryKeyNames && table.PrimaryKey != null)
                    {
                        if (CheckString(table.PrimaryKey.PrimaryKeyName))
                            issueCollector.ReportIssue(GetIssue(table.PrimaryKey));
                    }

                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        if (CheckString(foreignKey.ForeignKeyName))
                            issueCollector.ReportIssue(GetIssue(foreignKey));
                    }
                }
            }
        }

        private Issue GetIssue(DatabaseID context)
        {
            var identifierName = String.Empty;
            var dbObject = String.Empty;
            var issue = new Issue(this, this.DefaultSeverity.Value);
            issue.Name = "Identifier is a Reserved Word From SQL";
            Table table = null;
            if (context is Schema)
            {
                var sid = context as Schema;
                identifierName = sid.SchemaName;
                dbObject = "schema";
            }
            else if (context is Table)
            {
                var sid = context as Table;
                identifierName = sid.TableName;
                dbObject = "table";
            }
            else if (context is Column)
            {
                var sid = context as Column;
                identifierName = sid.ColumnName;
                dbObject = "column";
                table = sid.Table;
            }
            else if (context is PrimaryKey)
            {
                var sid = context as PrimaryKey;
                identifierName = sid.PrimaryKeyName;
                dbObject = "primary-key";
                table = sid.Table;
            }
            else if (context is ForeignKey)
            {
                var sid = context as ForeignKey;
                identifierName = sid.ForeignKeyName;
                dbObject = "foreign-key";
            }

            if (table != null)
            {
                issue.Description = new Description("The {0} name '{1}' in table '{2}' is a reserved SQL word.", dbObject, identifierName, table);
            }
            else
            {
                issue.Description = new Description("The {0} name '{1}' is a reserved word in SQL.", dbObject, identifierName);
            }
            
            issue.ExtendedDescription = new Description("{0}", GetStandardViolation(identifierName));
            issue.Context = IssueContext.Create(context);
            return issue;
        }

        protected override Severity Severity
        {
            get { return Severity.Low; }
        }


        private string GetStandardViolation(string name)
        {
            var result = String.Empty;
            switch (this.reservedWords[name.ToUpper()])
            {
                case SqlStandard.SQL92: result = "It violates the SQL-92 standard"; break;
                case SqlStandard.SQL99: result = "It violates the SQL-99 standard"; break;
                case SqlStandard.BOTH: result = "It violates both the SQL-92 and SQL-99 standard"; break;
            }
            return result;
        }

        private void BuildReservedWordList()
        {
            this.reservedWords = new Dictionary<string, SqlStandard>();
            foreach (var word in Resource.ReservedWordsSQL99.Split(','))
                this.reservedWords.Add(word, SqlStandard.SQL99);
            foreach (var word in Resource.ReservedWordsSQL92.Split(','))
            {
                if (this.reservedWords.ContainsKey(word))
                    this.reservedWords[word] = SqlStandard.BOTH;
                else
                    this.reservedWords.Add(word, SqlStandard.SQL92);
            }
        }

        private bool CheckString(String s)
        {
            if (this.reservedWords.ContainsKey(s.ToUpper()))
                return true;
            return false;
        }
    }
}
