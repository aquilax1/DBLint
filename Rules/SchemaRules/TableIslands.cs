using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    class TableIslands : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Table Islands and Missing Foreign Keys"; }
        }
        public Property<Severity> LargeTableIslandSeverity = new Property<Severity>("Large Table Island Severity", Severity.Medium, "Severity for table islands containing more than a single table");
        public Property<int> MaxTableIslandCount = new Property<int>("Maximum Tables in an Island", 6, "Maximum number of tables in a table island. Islands with more tables than this are filtered out", v => v >= 0);
        public Property<float> MaxFractionTables = new Property<float>("Maximum Percentage of Tables", 50, "Maximum percentage of all tables in an island. If a table islands contains a higher percentage than this, it is filtered out", v => v >= 0 && v <= 100);

            public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var schema in database.Schemas)
            {
                if (!schema.Tables.Any(t => t.ForeignKeys.Count > 0))
                {
                    Issue s = new Issue(this, Severity.Critical);
                    s.Name = "No Foreign Keys";
                    s.Description = new Description("The schema '{0}' has no foreign keys", schema.SchemaName);
                    s.Context = new SchemaContext(schema);
                    issueCollector.ReportIssue(s);
                    continue;
                }

                DatabaseDictionary<TableID, Table> seenTables = DictionaryFactory.CreateTableID<Table>();
                DatabaseDictionary<TableID, Table> notSeen = DictionaryFactory.CreateTableID<Table>();

                foreach (var t in schema.Tables)
                    notSeen.Add(t, t);
                List<List<Table>> clusters = new List<List<Table>>();
                while (notSeen.Count > 0)
                {
                    var table = notSeen.First().Value;
                    var cluster = new List<Table>(schema.Tables.Count);
                    BrowseTables(table, seenTables, notSeen, cluster);
                    clusters.Add(cluster);
                }

                foreach (var cluster in clusters)
                {
                    if (cluster.Count == 1)
                    {
                        Issue issue = new Issue(this, this.DefaultSeverity.Value)
                        {
                            Context = new TableContext(cluster[0]),
                            Name = "Table Island",
                            Description = new Description("Table {0} does not reference anything and is not referenced by anything", cluster[0]),
                        };
                        issueCollector.ReportIssue(issue);
                    }
                    else
                    {
                        if (cluster.Count > schema.Tables.Count * this.MaxFractionTables.Value / 100f || schema.Tables.Count > this.MaxTableIslandCount.Value)
                            continue;

                        string tableList = String.Join(", ", cluster.Select(c => c.TableName));
                        if (tableList.Length > 20)
                        {
                            tableList = tableList.Substring(0, 20) + "...";
                        }

                        Issue issue = new Issue(this, LargeTableIslandSeverity.Value)
                        {
                            Context = IssueContext.Create(cluster),
                            Name = "Table Island",
                            Description = new Description("There is a table island containing {0} tables: {1}", cluster.Count, tableList),
                            ExtendedDescription = new Description("Tables: {0}", cluster),
                        };
                        issueCollector.ReportIssue(issue);
                    }
                }
            }
        }

        protected override Severity Severity
        {
            get { return Severity.Low; }
        }


        private static void BrowseTables(Table table, DatabaseDictionary<TableID, Table> seenTables, DatabaseDictionary<TableID, Table> notSeen, List<Table> cluster)
        {
            cluster.Add(table);
            notSeen.Remove(table);
            seenTables.Add(table, table);
            foreach (var reference in table.References.Concat(table.ReferencedBy))
            {
                if (seenTables.ContainsKey(reference) || !SchemaID.SchemaEquals(reference.Schema, table.Schema))
                    continue;
                else
                    BrowseTables(reference, seenTables, notSeen, cluster);
            }
        }
    }
}
