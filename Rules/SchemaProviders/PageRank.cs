using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.Rules.Utils;

namespace DBLint.Rules.SchemaProviders
{
    public class PageRank : BaseSchemaProvider, ITableImportance
    {
        private class DoubleWrapper
        {
            public double Value;
        }

        private DatabaseDictionary<TableID, double> _ranks = DictionaryFactory.CreateTableID<double>();
        public override string Name
        {
            get { return "PageRank"; }
        }

        public override bool RunAlways
        {
            get { return true; }
        }

        public double GetRank(TableID tableID)
        {
            return _ranks[tableID];
        }

        public override void Execute(Database database, IProviderCollection providers)
        {
            double damping = 0.85;

            DatabaseDictionary<TableID, DoubleWrapper> ranks = DictionaryFactory.CreateTableID<DoubleWrapper>();
            DatabaseDictionary<TableID, DoubleWrapper> rankCalculator = DictionaryFactory.CreateTableID<DoubleWrapper>();
            DatabaseDictionary<TableID, Table[]> neighborMatrix = DictionaryFactory.CreateTableID<Table[]>();

            var initialRank = 1f / database.Tables.Count;
            foreach (var table in database.Tables)
            {
                ranks.Add(table, new DoubleWrapper { Value = 1 });
                var referencedBy = new List<Table>();
                foreach (var fkTable in table.ReferencedBy)
                {
                    foreach (var foreignKey in fkTable.ForeignKeys)
                    {
                        if (foreignKey.PKTable.Equals(table))
                            referencedBy.Add(fkTable);
                    }
                }
                neighborMatrix.Add(table, referencedBy.ToArray());
            }

            foreach (var tableID in ranks.Keys)
            {
                rankCalculator[tableID] = new DoubleWrapper { Value = 0 };
                ranks[tableID] = new DoubleWrapper { Value = 0 };
            }

            for (int i = 0; i < 100; i++)
            {
                double error = 0;
                foreach (var tableID in ranks.Keys)
                {
                    double value = 0;
                    var references = neighborMatrix[tableID];
                    foreach (var reference in references)
                    {
                        var referenceRank = ranks[reference];
                        value += referenceRank.Value / reference.ForeignKeys.Count;
                    }
                    double newRank;
                    rankCalculator[tableID].Value = newRank = value * damping + initialRank * (1 - damping);
                    error += Math.Abs(ranks[tableID].Value - newRank);
                }

                {
                    var tmp = rankCalculator;
                    rankCalculator = ranks;
                    ranks = tmp;
                }
                
                if (error < 0.001)
                    break;
                  
            }

            double sum = 0;
            foreach (var key in ranks.Keys)
                sum += ranks[key].Value;

            foreach (var key in ranks.Keys)
            {
                _ranks[key] = ranks[key].Value / sum * 100;
            }
        }

        public double this[TableID tableID]
        {
            get { return _ranks[tableID]; }
        }
    }
}
