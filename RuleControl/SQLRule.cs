using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace DBLint.RuleControl
{
    public class SQLRule : BaseRule, IRule
    {
        //The default SQL code when the user creates a new rule
        //It should be simple enough for new users to get an idea of the structure of an sql rule without reading the documentation
        private const String defaultSQLCode = 
@"SELECT
    'my issue description' AS description,
    'This is a slight longer description' AS extended_description
FROM
    your_table
WHERE
    something = 'wrong'";

        protected override Severity Severity { 
            get { return Severity.Medium; }
        }

        //The name of the SQL rule. This is typically a hardcoded string for other rule types, but the user should be able to change it in the GUI
        public Property<String> RuleName = new Property<String>("Rule Name", "", "");

        //The actual SQL code used to query the database
        public Property<SQLCode> SQLCode = new Property<SQLCode>("SQL Code", new SQLCode(defaultSQLCode), "");

        //Refer to the configurable "RuleName" property
        public override String Name {
            get {
                return (this.RuleName.Value == null) ? this.RuleName.DefaultValue : this.RuleName.Value;
            }
        }

        /// <summary>
        /// //Executes the SQL code specified in the "SQLCode" property and constructs issues which are added to the issueCollector parameter
        /// </summary>
        public void Execute(Model.Database database, IssueCollector issueCollector)
        {
            try
            {
                System.Data.DataTable sqlResult = database.Query(this.SQLCode.Value.Code);
                this.HandleSQLResult(sqlResult, issueCollector, database);
            }
            catch (Exception e)
            {
                //Rule execution failed (probably because of bad SQL). Raise an issue.
                Issue sqlErrorIssue = new Issue(this, this.DefaultSeverity.Value);
                sqlErrorIssue.Name = "Failed to execute SQL rule";
                sqlErrorIssue.Description = new Description("Failed to execute SQL rule '{0}'", this.Name);
                sqlErrorIssue.ExtendedDescription = new Description("The error is:\n\n{0}\n\nThe SQL code is:\n\n{1}", e.Message, this.SQLCode.Value);
                sqlErrorIssue.Context = new DatabaseContext(database);
                issueCollector.ReportIssue(sqlErrorIssue);
            }
        }

        //Constructs issues from the result of the SQL query
        private void HandleSQLResult(DataTable result, IIssueCollector issueCollector, Model.Database database)
        {
            var columns = result.Columns;
            
            //Check for missing description column (which is the only required column to exist in the result set)
            if (!columns.Contains("description")) {
                Issue missingDescIssue = new Issue(this, this.DefaultSeverity.Value);
                missingDescIssue.Name = "Failed to execute SQL rule";
                missingDescIssue.Description = new Description("Failed to execute SQL rule '{0}'. Column 'description' is missing", this.Name);
                missingDescIssue.Context = new DatabaseContext(database);
                issueCollector.ReportIssue(missingDescIssue);
            }

            //The extended description is optional
            bool hasExtendedDescription = columns.Contains("extended_description");

            //Iterate through each row in the result and add an issue for every row
            if (result.Rows.Count > 0)
            {
                foreach (DataRow row in result.Rows)
                {
                    Issue issue = new Issue(this, this.Severity);
                    issue.Name = this.Name;
                    issue.Context = getContextFromSQLIssue(result, row, database);
                    issue.Description = new Description(row["description"].ToString());
                    if (hasExtendedDescription)
                    {
                        issue.ExtendedDescription = new Description(row["extended_description"].ToString());
                    }
                    issueCollector.ReportIssue(issue);
                }
            }
        }
        
        //The issue context is specified by including one of the following columns in the result set: schema_context, table_context or column_context
        //Default context is database context (also used if e.g. one of the specified tables does not exist in the model)
        private static IssueContext getContextFromSQLIssue(DataTable result, DataRow issueRow, Model.Database database)
        {
            //Schema context
            if (result.Columns.Contains("schema_context"))
            {
                String schemaName = (String)issueRow["schema_context"];
                Model.Schema schema = database.Schemas.Where(s => s.SchemaName == schemaName).FirstOrDefault();
                if (schema != null)
                {
                    return new SchemaContext(schema);
                }
            }
            //Table context
            else if (result.Columns.Contains("table_context"))
            {
                String tableName = (String)issueRow["table_context"];
                Model.Table table = database.Tables.Where(t => t.TableName == tableName).FirstOrDefault();
                if (table != null)
                {
                    return new TableContext(table);
                }
            }
            //Column context
            else if (result.Columns.Contains("column_context"))
            {
                String columnName = (String)issueRow["column_context"];
                Model.Column column = database.Columns.Where(c => c.TableName == columnName).FirstOrDefault();
                if (column != null)
                {
                    return new ColumnContext(column);
                }
            }

            //Default context
            return new DatabaseContext(database);
        }
    }
}
