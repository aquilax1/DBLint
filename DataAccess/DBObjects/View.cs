using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.DataAccess.DBObjects
{
    public class View : ViewID
    {
        public View(string schemaName, string viewName)
        {
            this.SchemaName = schemaName;
            this.ViewName = viewName;
        }
    }
}
