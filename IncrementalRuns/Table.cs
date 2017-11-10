using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Linq;

namespace DBLint.IncrementalRuns
{
    [DataContract(Name = "tableID", Namespace = "dblint")]
    public class TableID : IExtensibleDataObject
    {
        [DataMember]
        public String DatabaseName { get; private set; }
        [DataMember]
        public String SchemaName { get; private set; }
        [DataMember]
        public String TableName { get; private set; }

        public TableID(DBLint.Model.TableID tableID)
        {
            this.DatabaseName = tableID.DatabaseName;
            this.SchemaName = tableID.SchemaName;
            this.TableName = tableID.TableName;
        }

        public ExtensionDataObject ExtensionData { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is TableID))
                return false;

            var id = ((TableID)obj);
            return (this.TableName == id.TableName &&
                    this.SchemaName == id.SchemaName &&
                    this.DatabaseName == id.DatabaseName
                    );
        }

        public override int GetHashCode()
        {
            int h1 = this.TableName.GetHashCode();
            int h2 = this.SchemaName.GetHashCode();

            return h1 * 19 + h2;
        }
    }

    [DataContract(Name = "table", Namespace = "dblint")]
    public class Table : IExtensibleDataObject
    {
        [DataMember]
        public TableID TableID { get; private set; }
        [DataMember]
        public int Score { get; private set; }

        public Table(DBLint.Model.Table table, int score)
        {
            this.TableID = new TableID(table);
            this.Score = score;
        }

        public ExtensionDataObject ExtensionData { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is Table))
                return false;

            return (this.TableID.Equals(((Table)obj).TableID));
        }

        public override int GetHashCode()
        {
            return this.TableID.GetHashCode();
        }
    }
}
