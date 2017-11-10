using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.DataRules
{
    public class DuplicationRows : BaseDataRule
    {
        public override string Name
        {
            get { return "Duplicate Rows in a Table"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.Critical; }
        }

        protected override string DefaultSeverityName
        {
            get
            {
                return "Duplicate rows without PK";
            }
        }

        public Property<Severity> WithPKSeverity = new Property<Severity>("Duplicate rows with auto-increment PK", Severity.Medium, 
                                                                    "Severity of duplicate rows on tables with a single auto-increment/sequence column");

        public override bool SkipTable(Table table)
        {
            //Skip table if it has a PK which is not a single-columns sequence columns
            return (table.PrimaryKey != null &&
                (table.PrimaryKey.Columns.Count > 1 ||
                table.PrimaryKey.Columns[0].IsSequence==false));
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            //Two cases:
            //if table has no PK, check for real duplications (all columns). 
            //If it has a surrogate (auto increment-column) key, check for duplications of rows excluding the PK

            IList<Column> columns = table.QueryableColumns.ToList();
            if (table.PrimaryKey != null)
            {
                columns.Remove(table.PrimaryKey.Columns[0]);
            }

            var rows = new Dictionary<int, int>(); //rowhash -> rownumber
            var collisions = new Dictionary<int, List<int>>(); //rowhash -> row numbers            

            //First pass: calculcate hash for all columns and find hash collisions
            int rowNum = 1;
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    int hash = 0;
                    foreach (var column in columns)
                    {
                        Object val = row[column.ColumnName];
                        hash = hash * 31 + val.GetHashCode();
                    }

                    if (!rows.ContainsKey(hash))
                    {
                        rows.Add(hash, rowNum);
                    }
                    else
                    {
                        if (!collisions.ContainsKey(hash))
                            collisions.Add(hash, new List<int> { rows[hash] });
                        collisions[hash].Add(rowNum);
                    }

                    rowNum++;
                }

            rows.Clear();

            //Second pass, get all rows in collions
            var rowValues = new Dictionary<int, List<Object>>();
            var collisionRows = new HashSet<int>();
            collisions.Values.SelectMany(v => v).ToList().ForEach(v => collisionRows.Add(v));
            rowNum = 1;
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    if (collisionRows.Contains(rowNum))
                    {
                        rowValues.Add(rowNum, new List<Object>());
                        foreach (var column in columns)
                        {
                            Object val = row[column.ColumnName];
                            rowValues[rowNum].Add(val);
                        }
                    }
                    rowNum++;
                }

            int duplicates = 0;

            var samples = new List<List<Object>>(); //List of sample rows
            var rowsInSample = new HashSet<int>(); //
            
            //Third step: find duplicates
            foreach (var collision in collisions)
            {
                //Compare all rows in collision
                for (int i = 1; i < collision.Value.Count; i++)
                {
                    for (int j = 0; j < collision.Value.Count; j++)
                    {
                        int rowNumI = collision.Value[i];
                        int rowNumJ = collision.Value[j];

                        if (i != j && isEqual(rowValues[rowNumI], rowValues[rowNumJ]))
                        {
                            duplicates++;

                            if (samples.Count < 15)
                            {
                                if (!rowsInSample.Contains(rowNumI))
                                {
                                    samples.Add(rowValues[rowNumI]);
                                    rowsInSample.Add(rowNumI);
                                }
                                if (!rowsInSample.Contains(rowNumJ))
                                {
                                    samples.Add(rowValues[rowNumJ]);
                                    rowsInSample.Add(rowNumJ);
                                }
                            }

                            break;
                        }
                    }
                }
            }

            if (duplicates > 0)
            {
                System.Data.DataTable sampleTable = new System.Data.DataTable();
                foreach(var column in columns)
                {
                    sampleTable.Columns.Add(column.ColumnName, typeof(String));
                }
                foreach (var sampleRow in samples)
                {
                    var tableRow = sampleTable.NewRow();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        tableRow[i] = sampleRow[i].ToString();
                    }
                    sampleTable.Rows.Add(tableRow);
                }

                if (table.PrimaryKey == null)
                {
                    Issue issue = new Issue(this, this.Severity);
                    issue.Name = "Duplicate Rows";
                    issue.Context = new TableContext(table);
                    issue.Description = new Description("Table {0} contains {1} duplicate rows", table, duplicates);
                    issue.ExtendedDescription = new Description("Sample rows:\n\n{0}", sampleTable);
                    issueCollector.ReportIssue(issue);
                }
                else 
                {
                    Issue issue = new Issue(this, this.WithPKSeverity.Value);
                    issue.Name = "Duplicate Rows";
                    issue.Context = new TableContext(table);
                    issue.Description = new Description("Table {0} contains {1} duplicate rows (excluding the primary key)", table, duplicates);
                    issue.ExtendedDescription = new Description("Sample rows:\n\n{0}", sampleTable);
                    issueCollector.ReportIssue(issue);
                }
            }
        }

        private bool isEqual(IEnumerable<Object> set1, IEnumerable<Object> set2)
        {
            return set1.Intersect(set2).Count() == set1.Union(set2).Count();
        }
    }
}
