using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;
using System.IO;

namespace DBLint
{
    public class IScoringImpl : IScoring
    {
        private DatabaseLint dblint;
        private int databaseScore;
        private Dictionary<SchemaID, float> schemaScores = new Dictionary<SchemaID, float>();
        private Dictionary<TableID, float> tableScores = new Dictionary<TableID, float>();
        private Dictionary<ColumnID, float> columnScores = new Dictionary<ColumnID, float>();
        IIssuePenalty issuePenalties = new IssuePenalties();

        private float dividePenalty(float penalty, int count)
        {
            return 1 - ((1 - penalty) / count);
        }

        private void calScore(Column column)
        {
            float score = 100;
            var issues = this.dblint.IssueCollector.GetColumnIssues(column);
            issues.ToList().ForEach(i => score *= this.issuePenalties.GetColumnIssuePenalty(i.Severity));

            //Substract table level issues concerning this column
            foreach (var tableIssue in this.dblint.IssueCollector.GetTableIssues(column.Table))
            {
                var contextColumns = ((TableContext)tableIssue.Context).Columns;
                if (contextColumns != null && contextColumns.Contains(column))
                {
                    var p = this.issuePenalties.GetColumnIssuePenalty(tableIssue.Severity);
                    score *= p;
                }
            } 
            
            this.columnScores.Add(column, score);
        }

        private void calScore(Table table)
        {
            //Calculcate column average
            float score;
            var cscores = table.Columns.Select(c => this.columnScores[c]);
            score = cscores.Sum() / table.Columns.Count;

            //Substract schema level issues concerning this table
            foreach (var schemaIssue in this.dblint.IssueCollector.GetSchemaIssues(table.Schema))
            {
                var contextTables = schemaIssue.Context.GetTables();
                if (contextTables.Contains(table))
                {
                    var p = this.issuePenalties.GetTableIssuePenalty(schemaIssue.Severity);
                    score *= dividePenalty(p, contextTables.Count());
                }
            }
            //Substract table issues
            foreach (var issue in dblint.IssueCollector.GetTableIssues(table))
            {
                if (((TableContext)issue.Context).Columns == null)
                    score *= this.issuePenalties.GetTableIssuePenalty(issue.Severity);
            }

            this.tableScores.Add(table, score);
        }


        private void calScore(Schema schema)
        {
            float score = 100;
            var importanceProvider = dblint.RuleController.ProviderCollection.GetProvider<DBLint.Rules.SchemaProviders.ImportanceProvider>();
            if (schema.Tables.Count > 0)
            {
                double weightSum = 0;
                double scoreSum = 0;
                foreach (var table in schema.Tables)
                {
                    var weight = Math.Sqrt(importanceProvider[table]);
                    weight = Math.Max(weight, 1);
                    scoreSum += (this.tableScores[table] * weight);
                    weightSum += weight;
                }                
                score = (float)(scoreSum / weightSum); //Score now contains weighted average of tables
            }

            //Calculate database level issues concerning this schema
            foreach (var dbIssue in this.dblint.IssueCollector.GetDatabaseIssues(schema.Database))
            {
                var contextSchemas = ((DatabaseContext)dbIssue.Context).Schemas;
                if (contextSchemas != null && contextSchemas.Contains(schema))
                {
                    var p = this.issuePenalties.GetSchemaIssuePenalty(dbIssue.Severity);
                    score *= dividePenalty(p, contextSchemas.Count());
                }
            }

            foreach (var issue in dblint.IssueCollector.GetSchemaIssues(schema))
            {
                if (((SchemaContext)issue.Context).Tables == null)
                    score *= issuePenalties.GetSchemaIssuePenalty(issue.Severity);
            }

            this.schemaScores.Add(schema, score);
        }

        private void calScore(Database db)
        {
            float columnAverage = (columnScores.Values.Sum() / columnScores.Count);
            float tableAverage = (tableScores.Values.Sum() / tableScores.Count);
            float schemaAverage = (schemaScores.Values.Sum() / schemaScores.Count);
 
            var sscores = db.Schemas.Select(s => this.schemaScores[s]);
            float score = sscores.Sum() / db.Schemas.Count;
            foreach (var issue in dblint.IssueCollector.GetDatabaseIssues(db))
            {
                if (((DatabaseContext)issue.Context).Schemas == null)
                    score *= issuePenalties.GetSchemaIssuePenalty(issue.Severity);
            }

            /*using (StreamWriter sw = File.AppendText("scores.txt"))
            {
                sw.WriteLine(String.Format("{0},{1},{2},{3}", Math.Round(columnAverage, 0), Math.Round(tableAverage, 0), Math.Round(schemaAverage, 0), Math.Round(score, 0)));
            }*/

            this.databaseScore = (int)score;
        }

        public void CalculateScores(DatabaseLint dblint)
        {
            this.dblint = dblint;

            dblint.DatabaseModel.Columns.ToList().ForEach(c => calScore(c));
            dblint.DatabaseModel.Tables.ToList().ForEach(t => calScore(t));
            dblint.DatabaseModel.Schemas.ToList().ForEach(s => calScore(s));
            this.calScore(dblint.DatabaseModel);
        }

        public int GetScore()
        {
            return this.databaseScore;
        }

        public int GetScore(Schema schema)
        {
            return (int)this.schemaScores[schema];
        }

        public int GetScore(Table table)
        {
            return (int)this.tableScores[table];
        }

        public int GetScore(Column column)
        {
            return (int)this.columnScores[column];
        }

        public Dictionary<TableID, int> GetScores()
        {
            Dictionary<TableID, int> s = new Dictionary<TableID, int>();
            this.tableScores.ToList().ForEach(t => s.Add(t.Key, (int)t.Value));
            return s;
        }
    }
}
