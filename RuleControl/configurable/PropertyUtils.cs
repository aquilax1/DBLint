using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;

namespace DBLint.RuleControl
{
    public static class PropertyUtils
    {
        private static Type serializedType = typeof(List<ExecutableConfiguration>);

        public static void SaveProperties(IEnumerable<IConfigurable> configurables, String targetFile)
        {
            List<ExecutableConfiguration> configs = new List<ExecutableConfiguration>();
            foreach (var configuable in configurables)
            {
                IEnumerable<IProperty> properties = configuable.GetProperties();
                if (properties.Count() > 0)
                {
                    var config = new ExecutableConfiguration(configuable.GetType().ToString(), properties.ToList(), ((IExecutable)configuable).Name);
                    configs.Add(config);
                }

            }

            using (FileStream fileStream = new FileStream(targetFile, FileMode.Create))
            {
                var writer = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true });
                DataContractSerializer ser = new DataContractSerializer(serializedType);
                ser.WriteObject(writer, configs);
                writer.Close();
            }
        }

        public static void LoadProperties(IEnumerable<IConfigurable> configurables, String filePath)
        {
            lock (filePath)
            {
                List<ExecutableConfiguration> configs = new List<ExecutableConfiguration>();
                //Load configurations
                FileStream fs = new FileStream(filePath, FileMode.Open);
                XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                DataContractSerializer ser = new DataContractSerializer(serializedType);
                configs = (List<ExecutableConfiguration>)ser.ReadObject(reader, true);
                reader.Close();
                fs.Close();

                //Set properties of configurables
                foreach (var config in configs)
                {
                    var configurable = configurables.Where(c => c.GetType().ToString() == config.TypeName && 
                                                           ((IExecutable)c).Name == config.RuleName).FirstOrDefault();
                    if (configurable != null)
                    {
                        var propertyPairs = (from execProp in configurable.GetProperties()
                                             from configProp in config.Properties
                                             where execProp.Name == configProp.Name
                                             select new { ExecProperty = execProp, ConfigProperty = configProp });

                        foreach (var propertyPair in propertyPairs)
                        {
                            var execP = propertyPair.ExecProperty;
                            var configP = propertyPair.ConfigProperty;
                            execP.SetValue(configP.GetValue());
                        }
                    }
                }
            }
        }
    }
}
