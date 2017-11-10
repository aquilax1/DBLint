using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.ServiceModel;
using System.Reflection;

namespace DBLint.RuleControl
{
    public delegate bool ValidPropertyValue<T>(T value);

    public interface IProperty {
        String Name { get; }
        String Description { get; }
        Object GetValue();
        Object GetDefaultValue();
        void SetValue(Object value);
        Type[] GetSupportedTypes();
        bool isValidPropertyValue(Object value);
    }

    public static class PropertyKnownTypes
    {
        public static readonly Type[] Types = new[] { typeof(Property<int>), typeof(Property<float>), typeof(Property<string>), typeof(Property<bool>), typeof(Property<Severity>), typeof(Property<DBLint.DataTypes.NamingConventionRepresentation>), typeof(Property<SQLCode>) };
    }

    [KnownType("GetKnownTypes")]
    [DataContract(Name = "Property_{0}", Namespace = "dblint")]
    public class Property<T> : IProperty
    {
        //Static fields
        private static readonly Type[] supportedTypes = { typeof(int), typeof(bool), typeof(float), typeof(SQLCode), typeof(string), typeof(Severity), typeof(DBLint.DataTypes.NamingConventionRepresentation) };
        //Public data members
        [DataMember]
        public String Name { get; private set; }
        [DataMember]
        public String Description { get; private set; }
        [DataMember]
        public T Value { get; set; }
        //Public fields
        public T DefaultValue { get; set; }
        //Private fields
        private ValidPropertyValue<T> validPropertyDelegate;

        public Property(String name, T defaultValue, String description, ValidPropertyValue<T> validPropertyDelegate)
        {
            if (!supportedTypes.Contains(typeof(T)) && !typeof(T).IsEnum)
                throw new Exception("Property of type " + typeof(T) + " is not supported");

            this.Name = name;
            this.Description = description;
            this.DefaultValue = defaultValue;
            this.Value = defaultValue;
            this.validPropertyDelegate = validPropertyDelegate;
        }

        public Property(String name, T defaultValue, String description)
            : this(name, defaultValue, description, null)
        {
        }

        public Object GetValue()
        {
            return (Object)this.Value;
        }

        public Object GetDefaultValue()
        {
            return (Object)this.DefaultValue;
        }

        public void SetValue(Object value)
        {
            T parsed;
            if (this.tryParseValue(value, out parsed))
                Value = parsed;
        }

        public Type[] GetSupportedTypes()
        {
            return supportedTypes;
        }

        public bool isValidPropertyValue(Object value)
        {
            T parsed;
            if (!this.tryParseValue(value, out parsed))
                return false;

            if (this.validPropertyDelegate != null)
                return this.validPropertyDelegate(parsed);
            else
                return true;
        }

        public static Type[] GetKnownTypes()
        {
            return PropertyKnownTypes.Types;
        }

        //Tries to cast a value of any type to type T
        private bool tryParseValue(Object input, out T result)
        {
            if (input.GetType() == typeof(T)) //The input type matches, make a regular cast
            {
                result = (T)input;
                return true;
            }

            if (typeof(T).Equals(typeof(SQLCode)))
            {
                Object sqlCode = new SQLCode(input.ToString());
                result = (T)sqlCode;
                return true;
            }

            //Try to cast from string to T
            if (input is String)
            {
                Object res;
                bool succ = tryParseValue((String)input, out res);
                result = (T)res;
                return succ;
            }

            result = default(T);
            return false;
        }

        //Cast string to T
        private bool tryParseValue(String s, out Object result)
        {
            if (typeof(T) == typeof(int))
            {
                int res;
                bool succ = int.TryParse(s, out res);
                if (succ)
                {
                    result = (Object)res;
                    return true;
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                bool b;
                bool succ = bool.TryParse(s, out b);
                if (succ)
                {
                    result = (Object)b;
                    return true;
                }
            }
            else if (typeof(T) == typeof(float))
            {
                float f;
                bool succ = float.TryParse(s, out f);
                if (succ)
                {
                    result = (Object)f;
                    return true;
                }
            }
            else if (typeof(T) == typeof(string))
            {
                result = (Object)s;
                return true;
            }
            else if (typeof(T).IsEnum)
            {
                try
                {
                    result = Enum.Parse(typeof(T), s, true);
                    return true;
                }
                catch (Exception e)
                {   
                }
            }

            result = default(T);
            return false;
        }
    }
}
