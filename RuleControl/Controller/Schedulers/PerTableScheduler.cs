using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Schedules a list of executables such that they run on table at a time
    /// </summary>
    public class PerTableScheduler : IScheduler
    {
        private IEnumerable<Table> tables;

        public ExecutionPlan GetExecutionPlan(IEnumerable<IExecutable> executables, IEnumerable<Table> tables)
        {
            this.tables = tables;
            var plan = new ExecutionPlan();
            executables = ExecutionPlan.RemoveDataProvidersIfSchema(executables);
            var sortedExecutables = DependencyGraph.TopologicalSort(executables);
            var passes = DependencyGraph.CreatePasses(sortedExecutables);
            int slot = 0;
            foreach (var pass in passes)
            {
                //Add all schema executables to the current slot
                foreach(var e in pass.Where(e => e.IsSchema() ||e.IsSQLRule()))
                {
                    ExecutionUnit unit = new ExecutionUnit(e);
                    plan.AddUnit(unit, slot);
                }

                var dataExes = pass.Where(e => e.IsData()).ToList();
                foreach (var table in tables)
                {
                    foreach (var dataE in dataExes)
                    {
                        if (((IDataExecutable)dataE).SkipTable(table) == false)
                            plan.AddUnit(new ExecutionUnit(dataE, table), slot);
                    }
                    slot++;
                }
                //Add finalizers
                dataExes.ForEach(e => plan.AddUnit(new ExecutionUnit(e) { Finalize = true }, slot));
                if (plan[slot] != null)
                    slot++;
            }

            return plan;
        }
    }
}
