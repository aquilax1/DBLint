using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using DBLint.Model;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Represents a dblint config file, including connection, rule configs and tables to check
    /// </summary>
    [XmlRoot("config")]
    public class XMLConfigurationModel
    {
        [XmlAttribute("version")]
        public string Version = "8";

        [XmlElement("connection")]
        public DBLint.DataAccess.Connection Connection { get; set; }

        [XmlArray("tablesToCheck"), XmlArrayItem("table")]
        public List<TableToCheck> TablesToCheck { get; set; }

        [XmlArray("ruleConfigurations"), XmlArrayItem("ruleConfiguration")]
        public List<RuleConfiguration> RuleConfigurations { get; set; }

        [XmlArray("userDefinedRules"), XmlArrayItem("rule")]
        public List<String> UserDefinedRules { get; set; }

        public XMLConfigurationModel()
        { 
        }

        public XMLConfigurationModel(Configuration config)
        {
            //Set connection
            this.Connection = config.Connection;

            //Set list of user-defined SQL rule names
            this.UserDefinedRules = config.AllRules.Where(r => r.IsSQLRule()).Select(r => r.Name).ToList();

            //Create a list of TableToCheck objects from list of Table objects
            this.TablesToCheck = config.TablesToCheck.Select(t => new TableToCheck(t)).ToList();
            
            //Rule configurations
            this.RuleConfigurations = new List<RuleConfiguration>();
            foreach (IExecutable rule in config.AllRules)
            { 
                bool runRule = config.RulesToRun.Any(r => r.Name == rule.Name);
                this.RuleConfigurations.Add(new RuleConfiguration(rule, runRule));
            }
        }
    }

    public class TableToCheck
    {
        [XmlAttribute("name")]
        public string TableName { get; set; }

        [XmlAttribute("schema")]
        public string SchemaName { get; set; }

        public TableToCheck() 
        { 
        }

        public TableToCheck(TableID table)
        {
            this.TableName = table.TableName;
            this.SchemaName = table.SchemaName;
        }
    }

    public class RuleConfiguration
    {
        [XmlAttribute("ruleName")]
        public string Name { get; set; }

        [XmlAttribute("runRule")]
        public bool RunRule { get; set; }
        
        [XmlArray("properties"), XmlArrayItem("property")]
        public List<RuleProperty> Properties { get; set; }

        public RuleConfiguration()
        {
            this.Properties = new List<RuleProperty>();
        }

        public RuleConfiguration(IExecutable rule, bool runRule)
        {
            this.Name = rule.IsSQLRule() ? rule.Name : rule.GetType().ToString();
            this.RunRule = runRule;

            //Add properties
            this.Properties = new List<RuleProperty>();
            foreach (IProperty property in rule.GetProperties())
            {
                this.Properties.Add(new RuleProperty(property)); 
            }
        }
    }

    public class RuleProperty
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("value")]
        public string Value { get; set; }

        public RuleProperty()
        { 
        }

        public RuleProperty(IProperty property)
        {
            this.Name = property.Name;
            this.Value = property.GetValue().ToString();
        }
    }
}
