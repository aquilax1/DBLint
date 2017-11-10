using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Schedules a list of executables such that there is no synchronization with respect to tables
    /// </summary>
    public class AllAtOnceScheduler : IScheduler
    {
        public ExecutionPlan GetExecutionPlan(IEnumerable<IExecutable> executables, IEnumerable<Table> tables)
        {
            var plan = new ExecutionPlan();
            executables = ExecutionPlan.RemoveDataProvidersIfSchema(executables);
            var executablesSorted = DependencyGraph.TopologicalSort(executables);
            var passes = DependencyGraph.CreatePasses(executablesSorted);

            int slot = 0;
            foreach (Pass pass in passes)
            {
                foreach (var executable in pass)
                {
                    if (executable.IsSchema())
                        plan.AddUnit(new ExecutionUnit(executable), slot);
                    else
                        foreach (var table in tables)
                        {
                            if (((IDataExecutable)executable).SkipTable(table))
                                continue;
                            var unit = new ExecutionUnit(executable, table);
                            plan.AddUnit(unit, slot);
                        }
                }
                slot++;
                //Finalize all data executables in the previous slot
                pass.Where(e => e.IsData()).ToList().ForEach(e => plan.AddUnit(new ExecutionUnit(e){Finalize=true}, slot));
                slot++;
            }

            return plan;
        }
    }
}
