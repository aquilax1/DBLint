using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.Rules.Naming
{
    public class MarkovEdge
    {
        public String From { get; private set; }
        public String To { get; private set; }
        public double Label { get; private set; }

        public MarkovEdge(String from, String to, double label)
        {
            this.From = from;
            this.To = to;
            this.Label = label;
        }
    }
}
