using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DBLint.RuleControl
{
    public enum Status { Queued }

    public class ExecutableCollection : IEnumerable<IExecutable>
    {
        private Dictionary<IExecutable, Status> executables = new Dictionary<IExecutable, Status>();
        private ReaderWriterLockSlim dictLock = new ReaderWriterLockSlim();

        public ExecutableCollection()
        { 
        
        }

        public ExecutableCollection(IEnumerable<IExecutable> executables)
        {
            this.AddExecutables(executables);
        }

        public Status GetStatus(IExecutable executable)
        {
            return this.executables[executable];
        }

        public void SetStatus(IExecutable executable, Status status)
        {
            this.executables[executable] = status;
        }

        public IList<IExecutable> GetExecutables()
        {
            return this.executables.Keys.ToList();
        }

        public IList<IExecutable> GetExecutables(params Status[] status)
        {
            return (from kvp in this.executables
                    where status.Contains(kvp.Value)
                    select kvp.Key).ToList();
        }

        public void RemoveExecutable(IExecutable executable)
        {
            this.executables.Remove(executable);
        }

        public void AddExecutable(IExecutable exec)
        {
            this.executables.Add(exec, Status.Queued);
        }

        public void AddExecutables(IEnumerable<IExecutable> execs)
        {
            foreach (var e in execs)
            {
                this.AddExecutable(e);
            }
        }

        public IList<IExecutable> GetRules()
        {
            return this.GetExecutables().Where(e => e.IsRule()).ToList();
        }

        public IList<IExecutable> GetSchemaRules()
        {
            return this.GetRules().Where(e => e.IsSchema()).ToList();
        }

        public IList<IExecutable> GetDataRules()
        {
            return this.GetRules().Where(e => e.IsData()).ToList();
        }

        public IList<IExecutable> GetProviders()
        {
            return this.GetExecutables().Where(e => e.IsProvider()).ToList();
        }

        public IList<IExecutable> GetSchemaProviders()
        {
            return this.GetProviders().Where(e => e.IsSchema()).ToList();
        }

        public IList<IExecutable> GetDataProviders()
        {
            return this.GetProviders().Where(e => e.IsData()).ToList();
        }

        public IList<IExecutable> GetDataExecutables()
        {
            return this.GetExecutables().Where(e => e.IsData()).ToList() ;
        }
        
        public IList<IExecutable> GetSchemaExecutables()
        {
            return this.GetExecutables().Where(e => e.IsSchema()).ToList();
        }

        public IList<IExecutable> GetSQLRules()
        {
            return this.GetExecutables().Where(e => e.IsSQLRule()).ToList();
        }

        public IEnumerator<IExecutable> GetEnumerator()
        {
            return this.executables.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.executables.Keys.GetEnumerator();
        }
    }
}
