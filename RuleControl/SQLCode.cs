using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;

namespace DBLint.RuleControl
{
    [DataContract()]
    /**
     * Represents a piece of SQL code.
     * Used as a property type by SQL rules, allowing the GUI to display an appropriate template for SQL properties
     */
    public class SQLCode
    {
        [DataMember]
        public string Code { get; set; }

        public SQLCode(String code) {
            this.Code = code;
        }

        public override string ToString()
        {
            return this.Code;
        }
    }
}
