using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    /// <summary>
    /// Interface for executable schedulers
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Gets an execution plan scheduling the given executables
        /// </summary>
        /// <param name="executables">List of executables to be executed</param>
        /// <param name="tables">The tables to be executed on</param>
        /// <returns>An execution plan</returns>
        ExecutionPlan GetExecutionPlan(IEnumerable<IExecutable> executables, IEnumerable<Table> tables);
    }
}
