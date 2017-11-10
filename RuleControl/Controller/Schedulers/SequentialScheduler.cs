using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Schedules a list of executables such that everything is run sequentially
    /// </summary>
    public class SequentialScheduler : IScheduler
    {
        public ExecutionPlan GetExecutionPlan(IEnumerable<IExecutable> executables, IEnumerable<Table> tables)
        {
            var plan = new ExecutionPlan();
            executables = ExecutionPlan.RemoveDataProvidersIfSchema(executables);
            var executablesSorted = DependencyGraph.TopologicalSort(executables);
            int slot = 0;
            foreach (var e in executablesSorted)
            {
                if (e.IsSchema())
                {
                    plan.AddUnit(new ExecutionUnit(e), slot);
                    slot++;
                }
                else
                {
                    foreach (var table in tables)
                    {
                        if (((IDataExecutable)e).SkipTable(table) == false)
                        {
                            plan.AddUnit(new ExecutionUnit(e, table), slot);
                            slot++;
                        }
                    }
                    plan.AddUnit(new ExecutionUnit(e) { Finalize = true }, slot);
                    slot++;
                }
            }

            return plan;
        }
    }
}
