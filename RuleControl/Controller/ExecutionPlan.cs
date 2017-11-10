using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.Diagnostics;
using DBLint.Model;

namespace DBLint.RuleControl
{
    public delegate void Executor(ExecutionUnit unit);

    /// <summary>
    /// An execution plan specifices in which order and on what tables rules should be executed.
    /// The execution plan is divided into "slots" representing a schedule. Each slot contains a list of executable units. 
    /// All executables in one slot must be finished before moving to the next slot when executing.
    /// </summary>
    public class ExecutionPlan
    {
        private Dictionary<int, List<ExecutionUnit>> slots = new Dictionary<int,List<ExecutionUnit>>();
        private Executor executor;
        private IEnumerator<List<ExecutionUnit>> slotEnumerator;
        private Action finishedCallback;
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        /// <summary>
        /// Add an executable unit to the execution plan
        /// </summary>
        /// <param name="unit">Executable unit</param>
        /// <param name="slot">The slot where the unit is placed</param>
        public void AddUnit(ExecutionUnit unit, int slot)
        {
            if (!this.slots.ContainsKey(slot))
                this.slots.Add(slot, new List<ExecutionUnit>());
            this.slots[slot].Add(unit);
        }

        /// <summary>
        /// Gets a list of execution units in the specified slot
        /// </summary>
        /// <param name="slot">Slot</param>
        /// <returns>List of execution units</returns>
        public List<ExecutionUnit> this[int slot]
        {
            get {
                if (this.slots.ContainsKey(slot))
                    return this.slots[slot];
                else
                    return null;
            }
        }

        /// <summary>
        /// Creates an empty execution plan
        /// </summary>
        /// <param name="executor">Method used to execute execution units</param>
        /// <param name="finishedCallback">Method called when execution is finished</param>
        public void ExecutePlan(Executor executor, Action finishedCallback)
        {
            this.executor = executor;
            this.finishedCallback = finishedCallback;
            this.slotEnumerator = this.slots.Values.GetEnumerator();
            this.slotEnumerator.MoveNext();
            locker.EnterWriteLock();
            this.Run();
            locker.ExitWriteLock();
        }

        private void Run()
        {
            var slotItems = this.slotEnumerator.Current;
            var ready = slotItems.Where(u => u.Status == ExecutionStatus.Ready);
            var running = slotItems.Where(u => u.Status == ExecutionStatus.Running);
            if (ready.Count() > 0)
            {
                foreach (var r in ready)
                {
                    r.Status = ExecutionStatus.Running;
                    Thread t = new Thread(ExecuteUnit);
                    t.Start(r);
                    
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteUnit), r);
                    //ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(ExecuteUnit), r);
                }
            }
            else if (ready.Count() == 0 && running.Count() == 0)
            {
                //Move to next slot
                this.slotEnumerator.MoveNext();
                if (this.slotEnumerator.Current == null)
                    this.finishedCallback();
                else
                    this.Run();
            }
        }

        private void ExecuteUnit(Object unitObj)
        {
            var unit = unitObj as ExecutionUnit;
            this.executor(unit);
            locker.EnterWriteLock();
            unit.Status = ExecutionStatus.Done;
            this.Run();
            locker.ExitWriteLock();
        }

        public IEnumerable<IExecutable> GetExecutables()
        {
            List<IExecutable> result = new List<IExecutable>();
            foreach (var slot in this.slots.Values)
            {
                foreach (var unit in slot)
                {
                    result.Add(unit.Executable);
                }
            }
            return result;
            
        }

        //Remove all data providers (if there are no data rules selected)
        public static IEnumerable<IExecutable> RemoveDataProvidersIfSchema(IEnumerable<IExecutable> executables)
        {
            if (!executables.Any(e => e.IsDataRule()))
            {
                return executables.Where(e => e.IsSchema() || e.IsSQLRule()).ToList();
            }
            return executables;
        }
    }

    public enum ExecutionStatus { Ready, Running, Done }

    /// <summary>
    /// Represents a piece of "work" to be scheduled
    /// </summary>
    public class ExecutionUnit
    {
        public IExecutable Executable { get; set; }
        public ExecutionStatus Status { get; set; }
        public Table Table { get; set; }
        public bool Finalize { get; set; }

        public ExecutionUnit(IExecutable executable)
        {
            this.Status = ExecutionStatus.Ready;
            this.Executable = executable;
            Finalize = false;
        }

        public ExecutionUnit(IExecutable executable, Table table) : this(executable)
        {
            this.Table = table;
        }
    }
}
