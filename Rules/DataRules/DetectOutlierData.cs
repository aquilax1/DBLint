using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;
using DBLint.Model;
using DBLint.DataTypes;
using DBLint.Data;
using DBLint.Rules.OutlierDetection;
using DBLint.Rules.Utils.RTree;

namespace DBLint.Rules.DataRules
{

    public class DetectOutlierData : BaseDataRule
    {
        public Property<int> MinRows = StandardProperties.MinimumRows(100);
        public Property<int> MaxEntries = new Property<int>("Max Node Entries in the RTree", 10, "The max number of entires that each node in the RTree can contain", p => p > 0);
        public Property<int> K = new Property<int>("Min points (k)", 5, "The k value for LOF", p => p > 0);
        public Property<int> Diemensions = new Property<int>("Number of dimensions", 2, "Number of dimensions in the RTree (2 or 3)", p => p == 2 || p == 3);
        public Property<float> Threshold = new Property<float>("Threshold for outlier data", 3.0f, "Data with LOF value close to 1.0 it not considered outlier", p => p > 0.0);

        public override bool SkipTable(Table table)
        {
            return false;
        }

        public override string Name
        {
            get { return "Outlier Data In Column"; }
        }

        protected override Severity Severity
        {
            get { return RuleControl.Severity.Low; }
        }

        public override void Execute(DataTable table, IIssueCollector issueCollector, IProviderCollection providers)
        {
            if (table.Cardinality < this.MinRows.Value)
                return;

            var textVector = new TextSizes(); // new TextType(); //
            var dateVector = new DayVector();
            var numberVector = new OneDimVector();

            var diemension = (Dimensions)this.Diemensions.Value;
            var outlier = new Dictionary<string, OutlierAnalysis>();

            foreach (var column in table.QueryableColumns)
                outlier.Add(column.ColumnName, new OutlierAnalysis(diemension, this.MaxEntries.Value, this.K.Value));
            var start = DateTime.Now;
            using (var rowEnumerable = table.GetTableRowEnumerable())
                foreach (var row in rowEnumerable)
                {
                    foreach (var column in table.QueryableColumns)
                    {
                        if (row[column.ColumnName] == DBNull.Value)
                            continue;
                        if (column.DataType == DataType.DECIMAL)
                            outlier[column.ColumnName].Insert(new Point(numberVector.GetVector(diemension, Convert.ToDecimal(row[column.ColumnName]))), row[column.ColumnName].ToString());
                        else if (column.DataType == DataType.VARCHAR)
                            outlier[column.ColumnName].Insert(new Point(textVector.GetVector(diemension, row[column.ColumnName].ToString())), row[column.ColumnName].ToString());
                        else if (column.DataType == DataType.DATE)
                            outlier[column.ColumnName].Insert(new Point(dateVector.GetVector(diemension, Convert.ToDateTime(row[column.ColumnName]))), row[column.ColumnName].ToString());
                    }
                }
            var end = DateTime.Now;
            Console.WriteLine("RTree build for {0} in time: {1}", table.TableName, (end - start));
            /*
            foreach (var item in outlier)
            {
                if (item.Value.RTree.LeafCount > 1)
                {
                    Console.WriteLine("\tColumn: {0}, Hight: {1}, Nodes {2}, Leafs {3}, Phy leafs {4}", item.Key, item.Value.RTree.TreeHeight, item.Value.RTree.NodeCount, item.Value.RTree.LeafCount, item.Value.RTree.Leafs.Count);
                }
            }*/
            foreach (var item in outlier)
            {
                //Console.WriteLine("Analysing {0}", item.Key);
                var issue = new Issue(this, this.DefaultSeverity.Value);
                issue.Name = "Outlier Data";
                issue.Context = ColumnContext.Create(table.Columns.Single(c => c.ColumnName == item.Key));
                var dt = new System.Data.DataTable();
                dt.Columns.Add("LOF");
                //dt.Columns.Add("Value example");
                dt.Columns.Add("Examples of outliers");
                foreach (var val in item.Value.RTree.Leafs)
                {
                    float lof = item.Value.LOF(val.Value);
                    //Console.WriteLine("Column: {0}, Value {1}, lof {2}", item.Key, val.Value.ToString(), lof);
                    if (lof > this.Threshold.Value)
                    {
                        var row = dt.NewRow();
                        row[0] = item.Value.LOF(val.Value);
                        row[1] = String.Join(", ", item.Value.leafs[val.Value.NodeId].RowValues[0]);
                        //row[1] = item.Value.leafs[val.Value.NodeId].RowValues.Count;
                        dt.Rows.Add(row);
                    }
                }
                issue.Description = new Description("Outlier data is found in column: '{0}'", item.Key);
                issue.ExtendedDescription = new Description("Below is the outlier probability shown together with the number of occurences.\n{0}", dt);
                if (dt.Rows.Count > 0)
                    issueCollector.ReportIssue(issue);
            }
        }
    }

}
