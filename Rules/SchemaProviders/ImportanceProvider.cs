using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.Rules.DataRules;
using DBLint.Rules.Utils;

namespace DBLint.Rules.SchemaProviders
{
    public class ImportanceProvider : BaseSchemaProvider, ITableImportance
    {
        private ITableImportance _importanceBackend;

        public override string Name
        {
            get { return "Importance Provider"; }
        }

        public override DependencyList Dependencies
        {
            get
            {
                return new DependencyList(typeof(PageRank), typeof(TableDataImportance));
            }
        }

        public override void Execute(Database database, IProviderCollection providers)
        {
            ITableImportance importance = providers.GetProvider<TableDataImportance>();
            if (importance == null)
            {
                importance = providers.GetProvider<PageRank>();
            }

            this._importanceBackend = importance;
        }

        public double this[TableID tableID]
        {
            get { return _importanceBackend[tableID]; }
        }

        public override bool RunAlways
        {
            get
            {
                return true;
            }
        }
    }
}
