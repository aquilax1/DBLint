using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;

namespace DBLint.RuleControl
{
    [DataContract(Name = "ExecutableConfiguration", Namespace = "dblint")]
    [KnownType("GetKnownTypes")]
    public class ExecutableConfiguration
    {
        [DataMember]
        public String TypeName { get; private set; }
        [DataMember]
        public String RuleName { get; private set; }
        [DataMember]
        public List<IProperty> Properties { get; private set; }

        public ExecutableConfiguration(String typeName, List<IProperty> properties, String ruleName)
        {
            this.TypeName = typeName;
            this.Properties = properties;
            this.RuleName = ruleName;
        }

        public static Type[] GetKnownTypes()
        {
            return PropertyKnownTypes.Types;
        }
    }
}
