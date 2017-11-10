namespace DBLint.DBLintGui.ExecuteTab
{
    /// <summary>
    /// Status object, used as the glue between the GUI and the worker.
    /// </summary>
    internal class ExecutionStatus : NotifyerClass
    {
        private int _totalWork;
        public int TotalWork
        {
            get { return _totalWork; }
            set
            {
                _totalWork = value;
                Notify("TotalWork");
            }
        }

        private int _workDone = 0;
        public int WorkDone
        {
            get { return _workDone; }
            set
            {
                _workDone = value;
                if (_workDone == _totalWork)
                    _workDone = 0;
                Notify("WorkDone");
            }
        }

        private int _foundIssues;
        public int FoundIssues
        {
            get { return _foundIssues; }
            set
            {
                _foundIssues = value;
                Notify("FoundIssues");
            }
        }

        private int _tablesAnalyzed;
        public int TablesAnalyzed
        {
            get { return _tablesAnalyzed; }
            set
            {
                _tablesAnalyzed = value;
                Notify("TablesAnalyzed");
            }
        }

        private int _totalTables;
        public int TotalTables
        {
            get { return _totalTables; }
            set
            {
                _totalTables = value;
                Notify("TotalTables");
            }
        }
    }
}