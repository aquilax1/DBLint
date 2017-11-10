using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DBLint.RuleControl;
using NVelocity;
using NVelocity.App;
using NVelocity.Context;
using System.IO;
using DBLint.Model;

namespace DBLint.ReportGeneration
{
    class HTMLDescriptionFormatter : DescriptionFormatter
    {
        private Dictionary<Table, String> tableFiles;
        private Template descTableTemplate = null;

        public String PathPrefix { get; set;  }

        public HTMLDescriptionFormatter(Dictionary<Table, String> tableFiles)
        {
            this.tableFiles = tableFiles;
            this.descTableTemplate = Velocity.GetTemplate("description_table.vm");
            this.PathPrefix = "";
        }

        public override String Format(DataTable dataTable)
        {
            VelocityContext context = new VelocityContext();
            context.Put("table", dataTable);
            context.Put("formatter", this);
            return renderTemplate(this.descTableTemplate, context);
        }

        public override String Format(Table table)
        {
            String url = (tableFiles.ContainsKey(table)) ? tableFiles[table] : "#";
            return String.Format("<a href=\"{0}{1}\" target=\"_blank\">{2}</a>", this.PathPrefix, url, table.TableName);
        }

        public String FormatWrapper(Object obj)
        {
            return this.Format(obj);
        }

        public override string Format(String str)
        {
            str = str.Replace("\n", "<br/>");
            return str;
        }

        public override string Format(SQLCode sqlCode)
        {
            return "<pre>"+sqlCode+"</pre>";
        }

        public override string Format(IEnumerable<string> list)
        {
            StringBuilder s = new StringBuilder();
            s.Append("<ul>");
            foreach (String str in list)
            {
                s.Append("<li>"+str+"</li>");
            }
            s.Append("</ul>");
            return s.ToString();
        }

        private String renderTemplate(Template template, VelocityContext context)
        { 
            StringWriter html = new StringWriter();
            template.Merge(context, html);
            html.Close();
            html.Flush();
            return html.ToString();
        }
    }
}
