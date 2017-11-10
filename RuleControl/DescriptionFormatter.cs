using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using DBLint.RuleControl;
using DBLint.Model;
using System.Data.Common;

namespace DBLint.RuleControl
{
    public abstract class DescriptionFormatter
    {
        public abstract String Format(DataTable dataTable);
        public abstract String Format(Table table);
        public abstract String Format(String str);
        public abstract String Format(IEnumerable<String> list);
        public abstract String Format(SQLCode sqlCode);

        public String Format(Object obj)
        {
            if (obj == null)
                return String.Empty;
            else if (obj is String)
                return Format((String)obj);
            else if (obj is DataTable)
                return Format((DataTable)obj);
            else if (obj is Table)
                return Format((Table)obj);
            else if (obj is SQLCode)
                return Format((SQLCode)obj);
            else if (obj is IEnumerable<Object>)
            {
                IEnumerable<String> formattedObjs = ((IEnumerable<Object>)obj).Select(o => Format(o));
                return Format(formattedObjs);
            }
            else if (obj is IEnumerable)
            {
                List<Object> objs = new List<Object>();
                foreach (Object o in (IEnumerable)obj)
                {
                    objs.Add(o);
                }
                return Format(objs);
            }

            return obj.ToString();
        }

        public String Format(Description description)
        {
            //Descriptions with no parameters
            if (description.Parameters == null)
            {
                return Format(description.Text);
            }

            //Format all parameters
            Object[] parameters = description.Parameters;
            Object[] formattedParameters = new Object[parameters.Count()];
            for (int i = 0; i < parameters.Length; i++)
            {
                formattedParameters[i] = Format(parameters[i]);
            }

            String text = Format(description.Text);

            return String.Format(text, formattedParameters);
        }
    }
}
