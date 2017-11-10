using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.DataTypes;

namespace DBLint.Rules.SchemaRules
{
    public class ForeignKeyRefColumnDataType : BaseSchemaRule
    {
        public override string Name
        {
            get { return "Different Data Type Between Source and Target Columns in a Foreign Key"; }
        }

        public override void Execute(Database database, IIssueCollector issueCollector, IProviderCollection providers)
        {
            foreach (var table in database.Tables)
            {
                foreach (var foreignKey in table.ForeignKeys)
                {
                    foreach (var reference in foreignKey.ColumnPairs)
                    {
                        if ((!reference.FKColumn.DataType.Equals(reference.PKColumn.DataType)) ||
                            reference.FKColumn.DataType.Equals(reference.PKColumn.DataType) &&
                            (!reference.FKColumn.CharacterMaxLength.Equals(reference.PKColumn.CharacterMaxLength) ||
                             !reference.FKColumn.NumericPrecision.Equals(reference.PKColumn.NumericPrecision) ||
                             !reference.FKColumn.NumericScale.Equals(reference.PKColumn.NumericScale)))
                        {
                            issueCollector.ReportIssue(new Issue(this, this.DefaultSeverity.Value)
                                    {
                                        Name = this.Name,
                                        Description = new Description("Different data types between source and target columns in foreign key from table '{0}' to table '{1}'", reference.FKColumn.Table, reference.PKColumn.Table),
                                        ExtendedDescription = new Description("The table below shows the column name and data type for the columns.\n{0}", this.GetTable(reference.FKColumn, reference.PKColumn)),
                                        Context = new ColumnContext(reference.FKColumn)
                                    });
                        }
                    }
                }
            }
        }

        private DataTable GetTable(Column fk, Column pk)
        {
            var dt = new DataTable();
            dt.Columns.Add("Type");
            dt.Columns.Add("Column Name");
            dt.Columns.Add("Data Type");
            dt.Columns.Add("Size");

            var row = dt.NewRow();
            row[0] = "FK";
            row[1] = fk.ColumnName;
            row[2] = fk.DataType;
            row[3] = (DataTypes.DataTypesLists.TextTypes().Contains(fk.DataType) ? Convert.ToString(fk.CharacterMaxLength) : String.Format("({0},{1})", fk.NumericPrecision, fk.NumericScale));
            dt.Rows.Add(row);

            row = dt.NewRow();
            row[0] = "PK";
            row[1] = pk.ColumnName;
            row[2] = pk.DataType;
            row[3] = (DataTypes.DataTypesLists.TextTypes().Contains(pk.DataType) ? Convert.ToString(pk.CharacterMaxLength) : String.Format("({0},{1})", pk.NumericPrecision, pk.NumericScale));
            dt.Rows.Add(row);

            return dt;
        }
        
        protected override Severity Severity
        {
            get { return Severity.Critical; }
        }
    }
}
