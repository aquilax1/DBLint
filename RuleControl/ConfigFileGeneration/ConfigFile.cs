using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using DBLint.Model;
using DBLint.DataAccess;
using DBLint;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Provides functionality to save and load dblint configurations
    /// </summary>
    public interface IConfigFile
    {
        /// <summary>
        /// Physical location of config file
        /// </summary>
        String FileLocation { get; set; }

        /// <summary>
        /// Saves/serializes the specified configuration
        /// </summary>
        void Save(Configuration config);

        /// <summary>
        /// Get connection from config file
        /// </summary>
        Connection GetConnection();
        
        /// <summary>
        /// Determines whether the specified rule should be run according to the config file
        /// </summary>
        bool RunRule(IExecutable rule);

        /// <summary>
        /// Determines whether the specified table should be checked
        /// </summary>
        bool CheckTable(string tableName, string schemaName);

        /// <summary>
        /// Determines whether the specified table should be checked
        /// </summary>
        bool CheckTable(TableID table);

        /// <summary>
        /// Gets a list of tables from database model which should be run
        /// </summary>
        IEnumerable<TableID> GetTablesToCheck();

        /// <summary>
        /// Configures the properties of the specified rule
        /// </summary>
        void ConfigureRule(IExecutable rule);

        /// <summary>
        /// Get list of schemas to check (derived from  tablesToCheck)
        /// </summary>
        IEnumerable<String> GetSchemaNames();

        /// <summary>
        /// Returns a list of SQL rules (ready configured)
        /// </summary>
        IEnumerable<SQLRule> GetSQLRules();

        /// <summary>
        /// List of types of the rules which should be run
        /// </summary>
        IEnumerable<String> GetRulesToRun();

        bool IsValid();
    }

    public class XMLConfigFile : IConfigFile
    {
        public String FileLocation { get; set; }

        private bool isValid = true;
        private XMLConfigurationModel xmlConfig = null;
        private XMLConfigurationModel XMLConfig
        {
            get {
                return this.xmlConfig;
            }
            set {
                this.xmlConfig = value;
            }
        }

        public XMLConfigFile(String fileDestination)
        {
            this.FileLocation = fileDestination;
            this.xmlConfig = this.loadConfig();
        }

        public IEnumerable<SQLRule> GetSQLRules()
        {
            var sqlRules = new List<SQLRule>();

            if (this.XMLConfig.UserDefinedRules != null)
            { 
                foreach(string ruleName in this.XMLConfig.UserDefinedRules)
                {
                    SQLRule rule = new SQLRule();
                    rule.RuleName.Value = ruleName;
                    rule.RuleName.DefaultValue = ruleName;
                    this.ConfigureRule(rule);

                    sqlRules.Add(rule);
                }
            }

            return sqlRules;
        }

        public void Save(Configuration config)
        {
            //Create XML configuration model
            XMLConfigurationModel xmlconfig = new XMLConfigurationModel(config);
            
            //Write XML file to FileLocation
            try
            {
                var serializer = new XmlSerializer(xmlconfig.GetType());
                var stream = new FileStream(this.FileLocation, FileMode.Create);
                serializer.Serialize(stream, xmlconfig);
                stream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public Connection GetConnection()
        {
            return this.XMLConfig.Connection;
        }

        public bool RunRule(IExecutable rule)
        {
            if (this.XMLConfig.RuleConfigurations != null)
            {
                //Rule names in config file are types names for normal rules, but friendly-name for SQL rules
                var ruleName = rule.IsSQLRule() ? rule.Name : rule.GetType().ToString();

                return this.XMLConfig.RuleConfigurations.Any(c => c.RunRule == true && c.Name == ruleName);
            }
            else
            {
                return false;
            }
        }

        public bool CheckTable(TableID table)
        {
            if (this.XMLConfig.TablesToCheck != null)
            {
                return this.XMLConfig.TablesToCheck.Any(t => t.TableName == table.TableName && t.SchemaName == table.SchemaName);
            }
            else
            {
                return false;
            }
        }

        public bool CheckTable(string tableName, string schemaName)
        {
            if (this.XMLConfig.TablesToCheck != null)
            {
                return this.XMLConfig.TablesToCheck.Any(t => t.TableName == tableName && t.SchemaName == schemaName);
            }
            else
            {
                return false;
            }
        }

        public IEnumerable<TableID> GetTablesToCheck()
        {
            var tableIDs = new List<TableID>();
            if (this.XMLConfig.TablesToCheck != null)
            {
                foreach (var tableToCheck in this.XMLConfig.TablesToCheck)
                {
                    tableIDs.Add(new TableID(null, tableToCheck.SchemaName, tableToCheck.TableName));
                }
            }
            
            return tableIDs;
            
        }

        public void ConfigureRule(IExecutable rule)
        {
            if (this.XMLConfig.RuleConfigurations == null)
            {
                return;
            }

            var ruleName = rule.IsSQLRule() ? rule.Name : rule.GetType().ToString();
            var ruleConfig = this.XMLConfig.RuleConfigurations.Where(r => r.Name == ruleName).FirstOrDefault();
            if (ruleConfig == null) //No rule configuration in config file for the specified rule
            {
                return;
            }

            foreach (IProperty property in rule.GetProperties())
            { 
                //Find matching property from config file
                RuleProperty ruleProperty = ruleConfig.Properties.Where(p => p.Name == property.Name).FirstOrDefault();
                if (ruleProperty != null)
                {
                    if (property.isValidPropertyValue(ruleProperty.Value))
                    {
                        property.SetValue(ruleProperty.Value);
                    }
                }
            }
        }

        public IEnumerable<String> GetSchemaNames()
        {
            if (this.XMLConfig.TablesToCheck == null)
            {
                return new List<String>();
            }

            var schemas = new List<String>();

            foreach (var tableToCheck in this.XMLConfig.TablesToCheck)
            {
                if (!schemas.Contains(tableToCheck.SchemaName))
                {
                    schemas.Add(tableToCheck.SchemaName);
                }
            }

            return schemas;
        }

        public IEnumerable<String> GetRulesToRun()
        {
            var result = new List<String>();
            foreach(var ruleConf in this.XMLConfig.RuleConfigurations)
            {
                if (ruleConf.RunRule) 
                {
                    result.Add(ruleConf.Name);
                }
            }
            return result;
        }

        private XMLConfigurationModel loadConfig()
        {
            XMLConfigurationModel config = new XMLConfigurationModel();

            if (!File.Exists(this.FileLocation))
            {
                this.createDefaultConfig();
            }

            try
            {
                var serializer = new XmlSerializer(config.GetType());
                var stream = new FileStream(this.FileLocation, FileMode.Open);
                config = (XMLConfigurationModel)serializer.Deserialize(stream);
                stream.Close();
            }
            catch (Exception e)
            {
                this.isValid = false;
            }

            return config;
        }

        private void createDefaultConfig()
        {
            Configuration c = new Configuration();
            this.Save(c);
        }

        public bool IsValid()
        {
            return this.isValid;
        }
    }
}
