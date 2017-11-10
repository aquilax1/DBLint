using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.RuleControl
{
    public static class StandardProperties
    {
        public static Property<int> MinimumRows(int rows)
        {
            return new Property<int>("Minimum Rows", rows, "The minimum number of rows in a table for this analysis to be considered valid.", v => v > 0);            
        }
    }
}
