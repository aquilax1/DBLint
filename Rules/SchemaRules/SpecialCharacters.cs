using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DBLint.RuleControl;
using DBLint.Model;

namespace DBLint.Rules.SchemaRules
{
    public class SpecialCharacters : BaseSchemaRule
    {
        public Property<String> RegexConfig;
        private Regex regex;

        public SpecialCharacters()
        {
            this.RegexConfig = new Property<String>("Characters Not Allowed in Identifiers", @"[^a-zA-Z0-9_\-]", "Regular expression recognizing not allowed characters", v => validRegex(v));
        }

        public override string Name
        {
            get
            {
                return "Use of Special Characters in Identifiers";
            }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            this.regex = new Regex(this.RegexConfig.Value, RegexOptions.Compiled);

            Match match;
            foreach (var schema in database.Schemas)
            {
                if (IsInvalid(schema.SchemaName, out match))
                    issueCollector.ReportIssue(GetIssue(schema, match));

                foreach (var table in schema.Tables)
                {
                    if (IsInvalid(table.TableName, out match))
                        issueCollector.ReportIssue(GetIssue(table, match));

                    foreach (var column in table.Columns)
                    {
                        if (IsInvalid(column.ColumnName, out match))
                            issueCollector.ReportIssue(GetIssue(column, match));
                    }

                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        if (IsInvalid(foreignKey.ForeignKeyName, out match))
                            issueCollector.ReportIssue(GetIssue(foreignKey, match));
                    }

                    foreach (var index in table.Indices)
                    {
                        if (IsInvalid(index.IndexName, out match))
                            issueCollector.ReportIssue(GetIssue(index, match));
                    }
                }
            }
        }

        private Issue GetIssue(DatabaseID context, Match match)
        {
            var identifierName = String.Empty;
            var dbObject = String.Empty;
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
            else if (context is Index)
            {
                var sid = context as Index;
                identifierName = sid.IndexName;
                dbObject = "index";
                table = sid.Table;
            }
            var issue = new Issue(this, this.DefaultSeverity.Value);
            issue.Context = IssueContext.Create(context);
            issue.Name = "Use of Special Character in Identifier";
            
            if (table != null)
            {
                issue.Description = new Description("The special character '{0}' is used in the {2} name: '{3}' in table in table '{1}'", match.Value, table, dbObject, identifierName);
            }
            else
            {
                issue.Description = new Description("The special character '{0}' is used in the {1} name: '{2}'", match.Value, dbObject, identifierName);
            }
            
            return issue;
        }

        protected override Severity Severity
        {
            get { return Severity.Low; }
        }

        private bool IsInvalid(String s, out Match match)
        {

            match = this.regex.Match(s);
            if (match.Success)
                return true;
            return false;
        }

        private bool validRegex(String regex)
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
