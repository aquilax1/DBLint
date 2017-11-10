using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Model;

namespace DBLint.RuleControl
{
    class ProgressTracker
    {
        public int Progress { get; private set; }
        private ulong total_tuples = 0;
        private ulong tuples_checked = 0;

        public ProgressTracker(IEnumerable<IExecutable> executables, IEnumerable<Table> tables)
        {
            foreach (var e in executables)
            {
                if (e.IsData())
                {
                    var dataE = (IDataExecutable)e;

                    foreach (var table in tables)
                    {
                        if (!dataE.SkipTable(table) && table.Cardinality > 0)
                        {
                            this.total_tuples += (ulong)table.Cardinality;
                        }
                    }
                }
                else
                {
                    this.total_tuples += 1;
                }
            }
        }

        public void UpdateProgress(IExecutable executable, Table table)
        {
            if (executable.IsData())
            {
                if (table.Cardinality > 0)
                {
                    this.tuples_checked += (ulong)table.Cardinality;
                }
            }
            else
            {
                this.tuples_checked += 1;
            }

            this.Progress = (int)(((float)this.tuples_checked / this.total_tuples) * 100);
        }
    }
}
