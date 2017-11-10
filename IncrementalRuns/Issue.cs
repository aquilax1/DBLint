using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using DBLint.RuleControl;

namespace DBLint.IncrementalRuns
{
    [DataContract(Name = "issue", Namespace = "dblint")]
    public class Issue : IExtensibleDataObject
    {
        [DataMember]
        public String IssueName { get; private set; }
        [DataMember]
        public String Description { get; private set; }
        [DataMember]
        public IEnumerable<TableID> tables { get; private set; }
        [DataMember]
        public int? Hash { get; private set; }
        [DataMember]
        public int ExtendedDescriptionHash { get; private set; }

        public Issue(DBLint.RuleControl.Issue issue)
        {
            this.IssueName = issue.Name;
            List<TableID> tableIDs = new List<TableID>();
            issue.Context.GetTables().ToList().ForEach(t => tableIDs.Add(new TableID(t)));
            this.tables = tableIDs;
            var formatter = new GenericDescriptionFormatter();
            this.Description = formatter.Format(issue.Description);
            if (issue.ExtendedDescription != null)
                this.ExtendedDescriptionHash = formatter.Format(issue.ExtendedDescription).GetHashCode();
            else
                this.ExtendedDescriptionHash = 0;
            this.Hash = this.GetHashCode();
        }

        public ExtensionDataObject ExtensionData { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is Issue))
                return false;

            var issue = (Issue)obj;

            if (this.Hash != issue.Hash)
                return false;

            if (issue.tables.Count() != this.tables.Count())
                return false;

            var c = issue.tables.Where(t => this.tables.Contains(t) == false);
            return (c.Count() == 0);
        }

        public override int GetHashCode()
        {
            if (!this.Hash.HasValue)
            {
                var h1 = this.Description.GetHashCode();
                var h2 = this.ExtendedDescriptionHash;
                var h3 = String.Join("", this.tables.Select(t => t.TableName)).GetHashCode();

                this.Hash = h1 * 19 + h2 + 29 * h3;
            }

            return this.Hash.Value;
        }
    }
}
