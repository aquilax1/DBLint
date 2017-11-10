using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.DataTypes;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.Rules.SchemaRules
{
    public class TooBigIndex : BaseSchemaRule
    {
        public Property<int> MaxSize = new Property<int>("Maximum Key Size", 200, "The maximum number of bytes allowed for an index key", v => v > 0);
        public Property<int> MaxColumns = new Property<int>("Maximum Columns in Index", 7, "The maximum number of columns allowed in an index, which is not used by a foreign key", v => v > 0);
        public Property<int> MaxColumnsUnique = new Property<int>("Maximum Columns in Unique Index", 5, "The maximum number of columns allowed in an index, which is not used by a foreign key", v => v > 0);
        public Property<int> VarcharSizeReductionFactor = new Property<int>("Estimated Varchar Fill Rate (%)", 20, "The average fill rate of varchars in indices. Used to adjust key-size to approximate average size");
        public override string Name
        {
            get { return "Too Big Indices"; }
        }

        protected override Severity Severity
        {
            get { return Severity.High; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            var indices = database.Tables.SelectMany(t => t.Indices);

            foreach (var index in indices)
            {
                if (index.IsUnique && index.Columns.Count > this.MaxColumnsUnique.Value)
                {
                    issueCollector.ReportIssue(
                        new Issue(this, this.DefaultSeverity.Value)
                            {
                                Name = "Too Big Index",
                                Context = new TableContext(index),
                                Description = new Description("Too many columns in unique index '{0}' in table '{1}'", index.IndexName, index.Table),
                                ExtendedDescription =
                                    new Description("Index '{0}' on the colums below has {2} columns which is more than maximally allowed in a unique/primary key index. {1}",
                                    index.IndexName, index.Columns.ToArray(), index.Columns.Count)
                            });
                }
                else if (index.Columns.Count > this.MaxColumns.Value)
                {
                    issueCollector.ReportIssue(
                        new Issue(this, this.DefaultSeverity.Value)
                            {
                                Name = "Too Big Index",
                                Context = new TableContext(index),
                                Description = new Description("Too many columns in index '{0}' in table '{1}'", index.IndexName, index.Table),
                                ExtendedDescription =
                                    new Description("Index '{0}' on the colums below has {2} columns which is more than maximally allowed in an index. {1}",
                                    index.IndexName, index.Columns.ToArray(), index.Columns.Count)
                            });
                }
                else if (GetIndexSize(index) > this.MaxSize.Value)
                {
                    var size = GetIndexSize(index);
                    issueCollector.ReportIssue(new Issue(this, this.DefaultSeverity.Value)
                        {
                            Name = "Too Big Index",
                            Context = new TableContext(index.Table),
                            Description = new Description("Too big key size in index '{0}' in table '{1}'", index.IndexName, index.Table),
                            ExtendedDescription =
                                new Description("Index '{0}' on the colums below has an approximate size of {2} bytes. Reduce this index if possible. {1}",
                                index.IndexName, index.Columns.ToArray(), size)
                        });
                }
            }
        }

        private long GetIndexSize(Index index)
        {
            long size = 0;
            foreach (var col in index.Columns)
            {
                DataType[] varcharTypes = new[] { DataType.VARCHAR, DataType.NVARCHAR };
                if (varcharTypes.Contains(col.DataType))
                    size += col.CharacterMaxLength * this.VarcharSizeReductionFactor.Value / 100;
                else if (col.CharacterMaxLength > 0)
                    size += Convert.ToInt64(col.CharacterMaxLength);
                else
                {
                    var maxSize = Math.Pow(10, col.NumericPrecision);
                    var bitsRequired = Math.Log(maxSize, 2);
                    var bytesRequired = Math.Round(bitsRequired / 4);
                    size += Convert.ToInt64(bytesRequired);
                }
            }
            return size;
        }
    }
}
