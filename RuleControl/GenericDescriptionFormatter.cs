using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DBLint.Model;

namespace DBLint.RuleControl
{
    public class GenericDescriptionFormatter : DescriptionFormatter
    {
        public override string Format(Table table)
        {
            return table.TableName;
        }

        public override string Format(String str)
        {
            return str;
        }

        public override string Format(DataTable dataTable)
        {
            StringBuilder s = new StringBuilder();
            foreach (DataRow row in dataTable.Rows)
            {
                List<String> values = new List<String>();
                row.ItemArray.ToList().ForEach(i => values.Add(this.Format(i)));
                s.Append(String.Join(", ", values));
                s.Append("\n");
            }
            
            return s.ToString();
        }

        public override string Format(SQLCode sqlCode)
        {
            return "<pre>" + sqlCode + "</pre>";
        }

        public override string Format(IEnumerable<string> list)
        {
            return String.Join("\n", list);
        }
    }
}
