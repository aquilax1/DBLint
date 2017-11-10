using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Data;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.Rules.Utils;

namespace DBLint.Rules.DataRules
{
    class InformationContent : BaseDataProvider, ITableImportance
    {
        public double this[ColumnID columnID]
        {
            get
            {
                if (_columnInformationContent.ContainsKey(columnID))
                    return this._columnInformationContent[columnID];
                return 0;
            }
        }

        public double this[TableID tableID]
        {
            get
            {
                if (_tableInformationContent.ContainsKey(tableID))
                    return this._tableInformationContent[tableID];
                return 0;
            }
        }

        DatabaseDictionary<ColumnID, double> _columnInformationContent = DictionaryFactory.CreateColumnID<double>();
        DatabaseDictionary<TableID, double> _tableInformationContent = DictionaryFactory.CreateTableID<double>();

        DataType[] validTypes = new[]
            {   DataType.BIGINT, 
                DataType.CHAR, 
                DataType.DECIMAL, 
                DataType.DOUBLE, 
                DataType.FLOAT, 
                DataType.INTEGER, 
                DataType.NCHAR,    
                DataType.NUMERIC, 
                DataType.NVARCHAR, 
                DataType.REAL, 
                DataType.VARCHAR
            };


        public double GetMultiColumnEntropy(DataTable table, params Column[] columns)
        {
            try
            {
                IEscaper escaper = table.Database.Escaper;

                //Select statement is actually just "SELECT SUM(pcount * log10({2} / pcount)) / (log10(2) * {2}) "
                //Obfuscated due to postgresql different log function names.
                //The .0 after the first {2} is to force float type
                string sourceQuery = @"SELECT " + escaper.RoundFunction("SUM(pcount * " + escaper.Log10Function("{2}.0 / pcount") + @") / ( " + escaper.Log10Function("2") + @" * {2})", 10) +
                                       @"FROM (SELECT (Count(*)) as pcount 
                                         FROM {0}
                                         GROUP BY {1}) exp1";

                StringBuilder columnsString = new StringBuilder(columns.Length * 10);
                bool first = true;
                foreach (var column in columns)
                {
                    var escapedColumn = escaper.Escape(column);
                    if (first)
                    {
                        columnsString.Append(escapedColumn);
                        first = false;
                    }
                    else
                    {
                        columnsString.AppendFormat(", {0}", escapedColumn);
                    }
                }
                var query = string.Format(sourceQuery, escaper.Escape(table), columnsString.ToString(), table.Cardinality);
                var res = table.QueryTable(query);
                if (res is DBNull)
                    return 0;
                else
                    return Math.Max(0, Convert.ToDouble(res));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 0;
            }
        }

        public override void Execute(DataTable table, IProviderCollection providers)
        {
            if (table.Cardinality <= 1)
                return;

            double tableInformationContent = Math.Log(table.Cardinality, 2);

            foreach (var column in table.Columns.Where(c => validTypes.Contains(c.DataType)))
            {
                double res = GetMultiColumnEntropy(table, column);
                if (res < 0)
                {
                    
                }
                _columnInformationContent[column] = res;
                tableInformationContent += res;
            }
            this._tableInformationContent[table] = tableInformationContent;
        }

        public override bool SkipTable(Table table)
        {
            return false;
        }

        public override string Name
        {
            get { return "Information Content Provider"; }
        }
    }
}
