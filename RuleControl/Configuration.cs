using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;
using DBLint.RuleControl;
using DBLint.DataAccess;

namespace DBLint
{
    public class Configuration
    {
        public IEnumerable<IExecutable> RulesToRun { get; set; }
        public IEnumerable<IExecutable> AllRules { get; set; }
        public IEnumerable<TableID> TablesToCheck { get; set; }
        public Connection Connection { get; set; }
        public List<DataAccess.DBObjects.TableID> IgnoreTables { get; set; }

        public Configuration(IEnumerable<IExecutable> rulesToRun, IEnumerable<Table> tablesToCheck)
        {
            this.RulesToRun = rulesToRun;
            this.TablesToCheck = tablesToCheck;
            this.IgnoreTables = new List<DataAccess.DBObjects.TableID>();
        }

        public Configuration(IEnumerable<IExecutable> rulesToRun, IEnumerable<Table> tablesToCheck, IEnumerable<IExecutable> allRules, Connection connection)
        {
            this.RulesToRun = rulesToRun;
            this.TablesToCheck = tablesToCheck;
            this.Connection = connection;
            this.AllRules = allRules;
            this.IgnoreTables = new List<DataAccess.DBObjects.TableID>();
        }

        public Configuration()
        {
            this.RulesToRun = new List<IRule>();
            this.TablesToCheck = new List<Table>();
            this.AllRules = new List<IExecutable>();
            this.IgnoreTables = new List<DataAccess.DBObjects.TableID>();
        }
    }
}
