using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint
{
    public interface IScoring
    {
        /// <summary>
        /// Calculate scores based on the given dblint object
        /// </summary>
        /// <param name="dblint"></param>
        void CalculateScores(DatabaseLint dblint);
        /// <summary>
        /// Returns the score of the database
        /// </summary>
        /// <returns>Database score</returns>
        int GetScore();
        /// <summary>
        /// Returns the score of a given schema
        /// </summary>
        /// <param name="schema">Schema</param>
        /// <returns>Score of schema</returns>
        int GetScore(Schema schema);
        /// <summary>
        /// Returns the score of a table
        /// </summary>
        /// <param name="table">Table</param>
        /// <returns>Score of table</returns>
        int GetScore(Table table);
        /// <summary>
        /// Return the score of a column
        /// </summary>
        /// <param name="column">Column</param>
        /// <returns>Score of column</returns>
        int GetScore(Column column);
        /// <summary>
        /// </summary>
        /// <returns>A dictionary mapping tables to scores</returns>
        Dictionary<TableID, int> GetScores();
    }

    /*
    public class SimpleScoring : IScoring
    {
        private IssueCollector issues;
        private Dictionary<Table, int> scores = new Dictionary<Table,int>();

        public void CalculateScores(DatabaseLint dblint)
        {
            this.issues = dblint.IssueCollector;
            foreach (var t in dblint.DatabaseModel.Tables)
            {
                double score = 100;
                int count = 0;
                foreach (var i in issues.GetIssues(t))
                {
                    count++;
                    int tableCount = i.Context.GetTables().Count();
                    double multFactor;
                    switch (i.Severity)
                    { 
                        case Severity.Critical:
                            multFactor = 0.50;
                            break;
                        case Severity.High:
                            multFactor = 0.65;
                            break;
                        case Severity.Medium:
                            multFactor = 0.80;
                            break;
                        case Severity.Low:
                            multFactor = 0.9;
                            break;
                        default:
                            multFactor = 1.0;
                            break;
                    }
                    if (tableCount > 0)
                        multFactor = 1-((1-multFactor)/tableCount);
                    score *= multFactor;
                }
                this.scores.Add(t, (int)score);
            }
        }

        public int GetScore(Table table)
        {
            return this.scores[table];
        }

        public int GetScore()
        {
            int tableCount = this.scores.Count;
            int totalScore = this.scores.ValueCount.Sum();
            int average = totalScore / tableCount;
            return average;
        }

        public Dictionary<Table, int> GetScores()
        {
            return scores;
        }
        */
    
}
