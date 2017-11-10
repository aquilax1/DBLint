using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBLint.IncrementalRuns
{
    public enum TableStatus { New, Removed, Changed }

    public class TableDiff
    {
        public DBLint.Model.TableID TableID { get; private set; }
        public TableStatus Status { get; private set; }
        public int OldScore { get; private set; }
        public int NewScore { get; private set; }

        public TableDiff(TableID tableID, TableStatus status, int oldScore, int newScore)
        {
            this.TableID = new DBLint.Model.TableID(tableID.DatabaseName, tableID.SchemaName, tableID.TableName);
            this.Status = status;
            this.OldScore = oldScore;
            this.NewScore = newScore;
        }
    }
}
