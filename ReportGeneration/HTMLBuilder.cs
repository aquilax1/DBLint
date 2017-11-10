using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NVelocity;
using NVelocity.App;
using NVelocity.Context;
using System.Diagnostics;
using DBLint;
using DBLint.RuleControl;
using DBLint.Model;
using DBLint.DataTypes;

namespace DBLint.ReportGeneration
{
    public class HTMLBuilder
    {
        private HTMLDescriptionFormatter formatter;
        private IScoring scoring;
        private DatabaseLint dblint;
        private Dictionary<Table, String> tableNames = new Dictionary<Table, String>();
        private Dictionary<Table, String> tableFiles = new Dictionary<Table, String>();

        public static void WriteReport(String dirName, DatabaseLint dblint)
        {
            new HTMLBuilder().Write(dirName, dblint);
        }

        public String IssueContextToHTML(IssueContext context)
        {
            var table_ids = context.GetTables();
            var tables = table_ids.Select(tid => this.dblint.DatabaseModel.GetTable(tid));
            if (tables.Count() > 0)
            {
                var formatted_tables = tables.Select(t => this.formatter.Format(t));
                return String.Join("<br/>", formatted_tables);
            }
            else
            {
                return "-";
            }
        }

        public String FormatTableID(TableID tableID)
        {
            Table table = this.dblint.DatabaseModel.GetTable(tableID);
            if (table != null)
                return this.formatter.Format(table);
            else
                return tableID.TableName;
        }

        private void Write(String dirName, DatabaseLint dblint)
        {
            this.scoring = new IScoringImpl();
            this.scoring.CalculateScores(dblint);

            if (DBLint.Settings.IsNormalContext)
            {
                //Save run for incremental viewing
                //File name is a timestamp
                DateTime now = DateTime.Now;
                String fileName = String.Format("{0}{1}{2}{3}{4}{5}.xml", now.Year, now.Month, now.Day, now.Hour,
                                                now.Minute, now.Second);
                //Folder, i.e.: runs/dbname/
                String folder = Settings.INCREMENTAL_FOLDER + "testtest"; // dblint.DatabaseModel.DatabaseName;
                String filePath = folder + "/" + fileName;
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);


                //Create run
                DBLint.IncrementalRuns.Run run = new IncrementalRuns.Run(dblint.DatabaseModel, dblint.IssueCollector, scoring.GetScores());
                //Write run
                using (FileStream writer = new FileStream(filePath, FileMode.Create))
                {
                    DataContractSerializer ser = new DataContractSerializer(typeof(DBLint.IncrementalRuns.Run));
                    ser.WriteObject(writer, run);
                    writer.Flush();
                }
            }

            DirectoryInfo dir = new DirectoryInfo(dirName);

            int tableNameCounter = 1;
            foreach (Table table in dblint.DatabaseModel.Tables)
            {
                String tName = "table" + tableNameCounter.ToString();
                this.tableNames.Add(table, tName);
                this.tableFiles.Add(table, "tables/" + tName + ".html");
                tableNameCounter++;
            }

            this.dblint = dblint;

            this.formatter = new HTMLDescriptionFormatter(this.tableFiles);

            IssueCollector issues = dblint.IssueCollector;

            //Create result directory if it does not exist
            if (!dir.Exists)
            {
                dir.Create();
            }

            VelocityContext context = new VelocityContext();
            context.Put("db", dblint.DatabaseModel);
            context.Put("totalScore", this.scoring.GetScore());
            context.Put("issuesTotal", issues.Count());
            context.Put("rulesExecuted", this.getRulesExecuted());
            context.Put("ruleTypes", this.getRuleTypes());
            context.Put("formatter", this.formatter);
            context.Put("HTMLBuilder", this);
            context.Put("summaries", this.dblint.ExecutionSummary);
            context.Put("executionTime", this.formatTimeSpan(this.dblint.ExecutionSummary.ExecutionTime));

            //Pagerank
            IProviderCollection providers = dblint.RuleController.ProviderCollection;
            var rank = providers.GetProvider<DBLint.Rules.SchemaProviders.ImportanceProvider>();

            //List all tables
            var tables = (from t in dblint.DatabaseModel.Tables
                          select new
                          {
                              Table = t,
                              Name = t.TableName,
                              IssueCount = issues.GetIssues(t).Count(),
                              Score = this.scoring.GetScore(t),
                              Importance = Math.Round(rank[t], 1)
                          }).ToList();
            context.Put("tables", tables);

            //Bottom tables
            var bottom = tables.OrderBy(t => t.Score).Take(5).ToList();
            context.Put("bottomTables", bottom);

            int groupId = 0; //Used in the template to identify a group of issues
            //Group issues by name
            var issueGroups = (from i in issues
                               group i by i.Name into g
                               orderby g.First().Severity
                               select new
                               {
                                   Name = g.Key,
                                   Count = g.Count(),
                                   Issues = g,
                                   GroupID = ++groupId,
                                   Severity = g.First().Severity
                               }).ToList();
            context.Put("issueGroups", issueGroups);

            //Put issueGroups into severity groups
            var severityGroups = (from issueGroup in issueGroups
                                  group issueGroup by issueGroup.Severity into g
                                  orderby g.First().Severity
                                  select new
                                  {
                                      Severity = g.First().Severity,
                                      IssueGroups = g
                                  }
                                  );
            context.Put("severityGroups", severityGroups);
            
            //Incremental runs list
            var diffs = new List<DBLint.IncrementalRuns.Diff>();

            if (DBLint.Settings.IsNormalContext)
            {
                //Incremental runs
                try
                {
                    var runs = DBLint.IncrementalRuns.Run.GetRuns(dblint.DatabaseModel.DatabaseName, 5).ToList();
                    for (int i = 1; i < runs.Count; i++)
                    {
                        var diff = new DBLint.IncrementalRuns.Diff();
                        diff.Compare(runs[i], runs[i - 1]);
                        diffs.Add(diff);
                    }
                }
                catch { }
                context.Put("diffs", diffs);
            }
            //Create template for the main html page
            Template template = Velocity.GetTemplate("mainpage.vm");

            //Create outputstream for the main page
            TextWriter htmlOut = new StreamWriter(Path.Combine(dir.FullName, "mainpage.html"));

            //Write template
            template.Merge(context, htmlOut);
            htmlOut.Close();

            //Write issue groups
            String issuePath = Path.Combine(dir.FullName, "issues");
            if (!Directory.Exists(issuePath))
                Directory.CreateDirectory(issuePath);
            Template issueGroupTemplate = Velocity.GetTemplate("issuegroup.vm");
            formatter.PathPrefix = "../";
            foreach (var g in issueGroups)
            {
                context.Put("groupIssues", g.Issues);
                TextWriter issueOut = new StreamWriter(Path.Combine(issuePath, g.GroupID.ToString() + ".html"));
                issueGroupTemplate.Merge(context, issueOut);
                issueOut.Close();
            }
            if (DBLint.Settings.IsNormalContext)
            {
                //Write diffs/increments to files:
                String incPath = Path.Combine(dir.FullName, "increments");
                if (!Directory.Exists(incPath))
                    Directory.CreateDirectory(incPath);
                Template incrementTemplate = Velocity.GetTemplate("increment.vm");
                int diffId = 0;
                foreach (var diff in diffs)
                {
                    diffId++;
                    context.Put("diff", diff);
                    TextWriter incOut = new StreamWriter(Path.Combine(incPath, diffId.ToString() + ".html"));
                    incrementTemplate.Merge(context, incOut);
                    incOut.Close();
                }
            }
            
            formatter.PathPrefix = "";
            writeTableViews(dirName);
        }

        private String formatTimeSpan(TimeSpan span)
        {
            if (span.TotalMilliseconds < 2000)
                return String.Format("{0:0} ms", span.TotalMilliseconds);
            else if (span.TotalMinutes < 2)
                return String.Format("{0:0} sec", span.TotalSeconds);
            else if (span.TotalHours < 2)
                return String.Format("{0:0} min", span.TotalMinutes);
            else
                return String.Format("{0:0} hours, {1:0} min", span.Hours, span.Minutes);
        }

        private String getRulesExecuted()
        {
            int totalRules = dblint.AllExecutables.GetRules().Count;
            int rulesExecuted = dblint.Config.RulesToRun.Count();
            return String.Format("{0}/{1}", rulesExecuted, totalRules);
        }

        private String getRuleTypes()
        {
            var rules = dblint.Config.RulesToRun;
            List<String> ruleTypes = new List<String>();
            if (rules.FirstOrDefault(r => r.IsSchema()) != null)
                ruleTypes.Add("Schema");
            if (rules.FirstOrDefault(r => r.IsData()) != null)
                ruleTypes.Add("Data");
            if (rules.FirstOrDefault(r => r.IsSQLRule()) != null)
                ruleTypes.Add("SQL");
            return String.Join(", ", ruleTypes);
        }

        private void writeTableViews(String reportDir)
        {
            this.formatter.PathPrefix = "../";
            String dir = Path.Combine(reportDir, "tables");
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);

            Template template = Velocity.GetTemplate("tableview.vm");
            foreach (Table table in this.dblint.DatabaseModel.Tables)
            {
                this.writeTableView(table, reportDir, this.tableFiles[table], template);
            }
            this.formatter.PathPrefix = "";
        }

        private void writeTableView(Table table, String reportDir, String fileName, Template template)
        {
            VelocityContext context = new VelocityContext();
            context.Put("table", table);

            //Find parents (tables that this table is referencing)
            Dictionary<Column, Table> parents = new Dictionary<Column, Table>();
            foreach (var fk in table.ForeignKeys)
            {
                if (fk.IsSingleColumn && !parents.ContainsKey(fk.FKColumn))
                {
                    parents.Add(fk.FKColumn, fk.PKColumn.Table);
                }
                else if (!fk.IsSingleColumn)
                {
                    foreach (var colPair in fk.ColumnPairs)
                    {
                        if (!parents.ContainsKey(colPair.FKColumn))
                            parents.Add(colPair.FKColumn, colPair.PKColumn.Table);
                    }
                }
            }

            //Find children (tables that reference this table)
            Dictionary<Column, List<Table>> children = new Dictionary<Column, List<Table>>();
            table.Columns.ToList().ForEach(c => children.Add(c, new List<Table>()));
            var fks = (from t in this.dblint.DatabaseModel.Tables
                       from fk in t.ForeignKeys
                       where fk.PKTable == table
                       select fk);
            foreach (var fk in fks)
            {
                if (fk.IsSingleColumn)
                {
                    children[fk.PKColumn].Add(fk.FKTable);
                }
                else
                {
                    foreach (var colPair in fk.ColumnPairs)
                    {
                        children[colPair.PKColumn].Add(colPair.FKColumn.Table);
                    }
                }
            }

            //Find issues
            IEnumerable<Issue> issues = this.dblint.IssueCollector.GetIssues(table).OrderBy(i => i.Severity);
            context.Put("children", children);
            context.Put("parents", parents);
            context.Put("issues", issues);
            context.Put("formatter", this.formatter);
            context.Put("HTMLBuilder", this);

            StringWriter output = new StringWriter();
            template.Merge(context, output);
            //this is a hack to prevent links on tableviews to open in a new windows
            String outputReplaced = output.ToString().Replace("target=\"_blank\"", "");
            output.Close();
            StreamWriter fileWriter = File.CreateText(Path.Combine(reportDir, fileName));
            fileWriter.Write(outputReplaced);
            fileWriter.Close();
        }

        public String GetColumnSize(Column col)
        {
            if (col.DataType == DataType.CHAR || col.DataType == DataType.VARCHAR || col.DataType == DataType.VARBINARY)
                return col.CharacterMaxLength.ToString();
            else if (col.DataType == DataType.NUMERIC || col.DataType == DataType.DECIMAL || col.DataType == DataType.FLOAT)
                return String.Format("{0},{1}", col.NumericPrecision, col.NumericScale);
            else if (col.DataType == DataType.BIGINT)
                return "8";
            else if (col.DataType == DataType.INTEGER)
                return "4";
            else if (col.DataType == DataType.SMALLINT)
                return "2";
            else if (col.DataType == DataType.TINYINT)
                return "1";
            return String.Empty;
        }
    }
}

