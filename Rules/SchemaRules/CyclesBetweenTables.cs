using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.DataTypes;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class CyclesBetweenTables : BaseSchemaRule
    {
        private DatabaseDictionary<TableID, NodeProperties> _nodeProps;
        private List<List<Table>> _foundCycles;
        private Stack<Table> _stack;
        private int _index;
        public Property<Severity> CycleWithoutDeferabilityProblems = new Property<Severity>("Deferrable Cycle Severity", Severity.Low, "Severity for cycles in which the deferability cannot cause a bootstrap problem.");
        public override string Name
        {
            get
            {
                return "Cycles Between Tables";
            }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            this._nodeProps = DictionaryFactory.CreateTableID<NodeProperties>();
            this._foundCycles = new List<List<Table>>();
            this._stack = new Stack<Table>();
            this._index = 0;
            foreach (var table in database.Tables)
            {
                if (!this._nodeProps.ContainsKey(table))
                    this.Tarjan(table);
            }

            foreach (var cycle in this._foundCycles)
            {
                var canInsert = false;
                var foundTables = new Dictionary<Table, List<ForeignKey>>();
                foreach (var table in cycle)
                {
                    var fksValues = table.ForeignKeys.Where(p => cycle.Contains(p.PKTable)).ToList();
                    foundTables.Add(table, fksValues);
                    foreach (var fkValue in fksValues)
                    {
                        if (fkValue.ColumnPairs.All(f => f.FKColumn.IsNullable) ||
                       (fkValue.Deferrability == Deferrability.Deferrable && fkValue.InitialMode == InitialMode.InitiallyDeferred))
                            canInsert = true;
                    }
                }
                var issue = new Issue(this, this.DefaultSeverity.Value);
                issue.Name = "Cycle Dependency Between Tables";
                issue.Context = IssueContext.Create(cycle);
                string tableList = String.Join(", ", cycle.Select(c => c.TableName));
                if (tableList.Length > 20)
                {
                    tableList = tableList.Substring(0, 20) + "...";
                }
                issue.Description = new Description("Cycle dependency between {0} tables: {1}", cycle.Count, tableList);
                var str = new StringBuilder();
                // Delete rules is no longer included in the check because of complex cycles
                if (!canInsert)
                {
                    str.Append("There is a cycle dependency, where the referential constraints could yield problems.");
                    str.Append("\n- Deferability and initially-deferred configurations could complicate insert, update and delete statements.");
                    issue.ExtendedDescription = new Description("{0}\n\nThe cycle contains the tables:\n\n{1}", str.ToString(), GetTable(foundTables));
                }
                else
                {
                    issue.Severity = CycleWithoutDeferabilityProblems.Value;
                    issue.ExtendedDescription = new Description("There is a cyclic dependency, but there are no deferability constraints that could yield problems. Consider if the cyclic dependency is needed.\n\nThe cycle contains the tables:\n\n{0}", GetTable(foundTables));
                }
                issueCollector.ReportIssue(issue);
            }
        }

        protected override string DefaultSeverityName
        {
            get
            {
                return "Cyclic Dependency Between Tables";
            }
        }
        protected override Severity Severity
        {
            get { return Severity.Medium; }
        }


        public DataTable GetTable(Dictionary<Table, List<ForeignKey>> foreignKeys)
        {
            var dt = new DataTable();
            dt.Columns.Add("Table Name");
            dt.Columns.Add("Foreign-Key Name(s) in the Cycle", typeof(IEnumerable<String>));
            dt.Columns.Add("Referenced Table(s) in the Cycle", typeof(IEnumerable<Table>));
            foreach (var foreignKey in foreignKeys)
            {
                var row = dt.NewRow();
                row[0] = foreignKey.Key;
                row[1] = foreignKey.Value.Select(f => f.ForeignKeyName).ToList();
                row[2] = foreignKey.Value.Select(f => f.PKTable).ToList();
                dt.Rows.Add(row);
            }
            return dt;
        }

        private void Tarjan(Table table)
        {
            this._nodeProps.Add(table, new NodeProperties(this._index, this._index));
            this._index++;
            this._stack.Push(table);
            foreach (var fk in table.ForeignKeys)
            {
                if (!this._nodeProps.ContainsKey(fk.PKTable))
                {
                    this.Tarjan(fk.PKTable);
                    this._nodeProps[table].Lowlink = Math.Min(this._nodeProps[table].Lowlink, this._nodeProps[fk.PKTable].Lowlink);
                }
                else if (this._stack.Contains(fk.PKTable))
                {
                    this._nodeProps[table].Lowlink = Math.Min(this._nodeProps[table].Lowlink, this._nodeProps[fk.PKTable].Index);
                }
            }
            if (this._nodeProps[table].Lowlink == this._nodeProps[table].Index)
            {
                var cycle = new List<Table>();
                Table cNode = null;
                do
                {
                    cNode = this._stack.Pop();
                    cycle.Add(cNode);
                }
                while (cNode != table);
                if (cycle.Count > 1)
                {
                    cycle.Reverse();
                    this._foundCycles.Add(cycle);
                }
            }
        }
    }

    public class NodeProperties
    {
        public int Index;
        public int Lowlink;

        public NodeProperties(int index, int lowLink)
        {
            this.Index = index;
            this.Lowlink = lowLink;
        }
    }
}
