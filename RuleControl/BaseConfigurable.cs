using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DBLint.RuleControl
{
    public class BaseConfigurable : IConfigurable
    {
        private List<IProperty> properties = null;

        public IEnumerable<IProperty> GetProperties()
        {
            if (this.GetType().Name.ToLower().Contains("super"))
            {
                
            }

            if (this.properties == null)
            {
                this.properties = new List<IProperty>();
                foreach (var field in this.GetType().GetFields())
                {
                    Object fieldValue = field.GetValue(this);
                    if (fieldValue is IProperty)
                    {
                        this.properties.Add((IProperty)fieldValue);
                    }
                }
                var memberProperties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
                foreach (var prop in memberProperties)
                {
                    if (prop.GetIndexParameters().Length > 0 )
                        continue;
                    IProperty propValue = prop.GetValue(this, null) as IProperty;
                    if (propValue != null)
                    {
                        this.properties.Add(propValue);
                    }
                }
            }

            return this.properties;
        }
    }
}
