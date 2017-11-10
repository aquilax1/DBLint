using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;
using DBLint;
using DBLint.RuleControl;

namespace DBLint.IncrementalRuns
{
    [DataContract(Name = "run", Namespace = "dblint")]
    public class Run
    {
        [DataMember]
        public IEnumerable<Table> Tables { get; private set; }
        [DataMember]
        public IEnumerable<Table> IgnoredTables { get; private set; }
        [DataMember]
        public IEnumerable<Issue> Issues { get; private set; }
        [DataMember]
        public String DatabaseName { get; private set; }
        [DataMember]
        public DateTime Timestamp { get; private set; }

        public Run(DBLint.Model.Database model, IssueCollector issues, Dictionary<DBLint.Model.TableID, int> scores)
        {
            this.DatabaseName = model.DatabaseName;
            this.Timestamp = DateTime.Now;

            //Add tables
            List<Table> tables = new List<Table>();            
            foreach (var table in model.Tables)
            {
                tables.Add(new Table(table, scores[table]));
            }
            this.Tables = tables;

            List<Table> ignoredTables = new List<Table>(); 
            foreach (var table in model.IgnoredTables)
            {
                ignoredTables.Add(new Table(table, -1));
            }
            this.IgnoredTables = ignoredTables;

            //Add issues
            List<Issue> iss = new List<Issue>();
            issues.ForEach(i => iss.Add(new Issue(i)));
            this.Issues = iss;
        }

        public ExtensionDataObject ExtensionData { get; set; }

        public static IEnumerable<Run> GetRuns(String databaseName, int count)
        {
            String folder = Settings.INCREMENTAL_FOLDER + databaseName;
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            IEnumerable<FileInfo> files = dirInfo.GetFiles();

            var runs = (from f in files
                         let r = GetRun(f.FullName)
                         orderby r.Timestamp descending
                         select r).Take(count);
            return runs;
        }

        public static Run GetRun(String filePath)
        {
            //Based on example: http://msdn.microsoft.com/en-us/library/system.runtime.serialization.datacontractserializer.aspx
            
            FileStream fs = new FileStream(filePath, FileMode.Open);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
            DataContractSerializer ser = new DataContractSerializer(typeof(Run));
            Run run = (Run)ser.ReadObject(reader, true);
            reader.Close();
            fs.Close();
            return run;
        }
    }
}
