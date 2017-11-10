using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace DBLint.RuleControl
{
    //THIS CLASS IS NO LONGER USED TO HANDLE SQL RULES AND SERIALIZATION
    //SHOULD BE REMOVED AT SOME POINT

    /// <summary>
    /// Manages the collection SQL rules used in the application (loading, serialization, etc)
    /// </summary>
    public static class SQLRuleCollection
    {
        private const String sqlRulesXMLFile = "user_defined_rules.xml";
        private static List<SQLRule> rules = new List<SQLRule>();

        public static IEnumerable<SQLRule> GetRules()
        {
            if (rules == null)
            {
                rules = LoadSQLRules().ToList();
            }

            return rules;
        }

        /// <summary>
        /// Adds an SQL rule to the rule collection using default parameters and persist the updated sql rule set
        /// </summary>
        /// <param name="ruleName">Name of the rule to be created</param>
        /// <returns>The created rule</returns>
        public static SQLRule AddSQLRule(String ruleName)
        {
            SQLRule newRule = new SQLRule();
            newRule.RuleName.Value = ruleName;
            rules.Add(newRule);
            saveSQLRules();
            return newRule;
        }

        /// <summary>
        /// Removes SQL rules with the specified name from the collection and persist the upated rule set
        /// </summary>
        /// <param name="ruleName">Name of the rule to be removed</param>
        public static void RemoveSQLRule(String ruleName)
        {
            IEnumerable<SQLRule> newRuleSet = rules.Where(r => r.Name != ruleName);
            rules = newRuleSet.ToList();
            saveSQLRules();
        }

        //Instantiates a list of SQL rules stored in the user_defined_rules.xml file
        //The config for each rule (containing the actual SQL code) is stored in the config file and is handled elsewhere
        private static IEnumerable<SQLRule> LoadSQLRules()
        {
            if (!File.Exists(sqlRulesXMLFile))
            {
                return new List<SQLRule>();
            }

            IEnumerable<String> SQLRuleNames = new List<String>();

            lock (sqlRulesXMLFile)
            {
                FileStream fs = new FileStream(sqlRulesXMLFile, FileMode.Open);
                XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                DataContractSerializer ser = new DataContractSerializer(typeof(IEnumerable<String>));
                SQLRuleNames = (IEnumerable<String>)ser.ReadObject(reader, true);
                reader.Close();
                fs.Close();
            }

            IList<SQLRule> returnValue = new List<SQLRule>();
            foreach (String ruleName in SQLRuleNames)
            {
                SQLRule rule = new SQLRule();
                rule.RuleName.Value = ruleName;
                rule.RuleName.DefaultValue = ruleName;
                returnValue.Add(rule);
            }

            return returnValue;
        }

        private static void saveSQLRules()
        {
            IEnumerable<String> RuleNames = rules.Select(r => r.Name);
            using (FileStream fileStream = new FileStream(sqlRulesXMLFile, FileMode.Create))
            {
                var writer = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true });
                DataContractSerializer ser = new DataContractSerializer(typeof(IEnumerable<String>));
                ser.WriteObject(writer, RuleNames);
                writer.Close();
            }
        }
    }
}
