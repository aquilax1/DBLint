using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.Rules.SchemaProviders;
using DBLint.Rules.Utils;

namespace DBLint.Rules.DataRules
{
    class TableDataImportance : BaseDataProvider, ITableImportance
    {
        private class JoinEdge
        {
            public Table Table;
            public Column[] Columns;
            public double EdgeEntropy;
        }

        private DependencyList _depList = DependencyList.Create<InformationContent>();
        private DatabaseDictionary<TableID, double> _importanceVector;

        public override bool RunAlways
        {
            get
            {
                return true;
            }
        }

        public override string Name
        {
            get { return "Table importance from data and graph"; }
        }

        public override DependencyList Dependencies
        {
            get { return _depList; }
        }

        public override void Finalize(Model.Database database, IProviderCollection providers)
        {
            var informationContent = providers.GetProvider<InformationContent>();
            var fks = database.Tables.SelectMany(t => t.ForeignKeys);

            DatabaseDictionary<TableID, List<JoinEdge>> dbJoinEdges = DictionaryFactory.CreateTableID<List<JoinEdge>>();
            DatabaseDictionary<TableID, double> tableTotalEntropyTransfer = DictionaryFactory.CreateTableID<double>();
            foreach (var tbl in database.Tables)
            {
                dbJoinEdges[tbl] = new List<JoinEdge>(4);
                tableTotalEntropyTransfer[tbl] = 0;
            }

            foreach (var foreignKey in fks)
            {
                var pkColumns = (from cp in foreignKey.ColumnPairs
                                 select cp.PKColumn).ToArray();
                var fkColumns = (from cp in foreignKey.ColumnPairs
                                 select cp.FKColumn).ToArray();

                double fkEdgeEntropy;
                if (foreignKey.IsSingleColumn)
                    fkEdgeEntropy = informationContent[foreignKey.FKColumn];
                else
                    fkEdgeEntropy = informationContent.GetMultiColumnEntropy((DataTable)foreignKey.FKTable, fkColumns);

                var pkEdgeEntropy = Math.Log(Math.Max(foreignKey.PKTable.Cardinality, 1), 2); // Primary key guarantees uniqueness across pkcolumns, hence entropy equals log of cardinality.
                dbJoinEdges[foreignKey.PKTable].Add(new JoinEdge { Table = foreignKey.FKTable, Columns = fkColumns, EdgeEntropy = fkEdgeEntropy });
                dbJoinEdges[foreignKey.FKTable].Add(new JoinEdge { Table = foreignKey.PKTable, Columns = pkColumns, EdgeEntropy = pkEdgeEntropy });

                tableTotalEntropyTransfer[foreignKey.PKTable] += pkEdgeEntropy;
                tableTotalEntropyTransfer[foreignKey.FKTable] += fkEdgeEntropy;
            }

            DatabaseDictionary<TableID, DatabaseDictionary<TableID, double>> pmatrix = DictionaryFactory.CreateTableID<DatabaseDictionary<TableID, double>>();
            foreach (var tbl in database.Tables)
                pmatrix[tbl] = DictionaryFactory.CreateTableID<double>();

            foreach (var toTable in database.Tables)
            {
                var joinEdges = dbJoinEdges[toTable];
                foreach (var joinEdge in joinEdges)
                {
                    var fromTable = joinEdge.Table;
                    var columnsEntropy = joinEdge.EdgeEntropy;
                    var tableInformationContent = informationContent[fromTable];
                    var tableTotalTransfer = tableTotalEntropyTransfer[fromTable];
                    var todic = pmatrix[toTable];
                    if (!todic.ContainsKey(fromTable))
                        todic[fromTable] = 0;

                    if (tableInformationContent + tableTotalTransfer > 0)
                        todic[fromTable] += columnsEntropy / (tableInformationContent + tableTotalTransfer);
                }
                var toTableRow = pmatrix[toTable];
                
            }

            foreach (var keyRow in pmatrix.Keys)
            {
                double selfLoopValue = 1d;
                foreach (var keyColumn in pmatrix.Keys)
                {
                    var row = pmatrix[keyColumn];
                    if (row.ContainsKey(keyRow))
                        selfLoopValue -= row[keyRow];
                }
                if (selfLoopValue < 0)
                {
                }
                pmatrix[keyRow][keyRow] = selfLoopValue;
            }


            DatabaseDictionary<TableID, double> importanceVector = DictionaryFactory.CreateTableID<double>();
            DatabaseDictionary<TableID, double> calculateVector = DictionaryFactory.CreateTableID<double>();
            foreach (var table in database.Tables)
                importanceVector[table] = Math.Max(informationContent[table], 0);

            for (int i = 0; i < 100; i++)
            {
                foreach (var table in importanceVector.Keys)
                {
                    double newRank = 0;
                    var fromTables = pmatrix[table];
                    foreach (var fromTable in fromTables)
                    {
                        var fromRank = importanceVector[fromTable.Key];
                        newRank += fromRank * fromTable.Value;
                    }
                    calculateVector[table] = newRank;
                }

                {
                    var tmp = calculateVector;
                    calculateVector = importanceVector;
                    importanceVector = tmp;
                }
            }

            var totalEntropy = importanceVector.Values.Sum();

            foreach (var k in importanceVector.Keys)
            {
                if (importanceVector[k] < 0)
                {
                }
                if (totalEntropy == 0)
                    importanceVector[k] = 100f / database.Tables.Count;
                else
                    importanceVector[k] *= 100f / totalEntropy;
            }
            this._importanceVector = importanceVector;
        }

        public double this[TableID tid]
        {
            get
            {
                return this._importanceVector[tid];
            }
        }

        public override void Execute(DataTable table, IProviderCollection providers)
        {

        }
    }
}
