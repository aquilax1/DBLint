using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace DBLint.DBLintGui.ExecuteTab
{
    /// <summary>
    /// This class handles all interfacing between the executionStatus/GUI object and the worker
    /// </summary>
    public class DBLintExecuter : IDisposable
    {
        /// <summary>
        /// Encapsulates a logging entry an gives it a unique id
        /// </summary>
        private class ItemContainer
        {
            private static int _number = 0;
            public ItemContainer(string value)
            {
                this.Value = value;
                this.Number = _number++;
            }

            public string Value { get; private set; }
            public int Number { get; private set; }
            public override bool Equals(object obj)
            {
                var ic = obj as ItemContainer;
                return ic != null && ic.Number == this.Number;
            }
            public override int GetHashCode()
            {
                return this.Number;
            }
        }

        private readonly ExecuteDBLint _executeDbLint;
        private ViewModel _viewModel;
        public ViewModel ViewModel { get { return _viewModel; } }

        internal void Update(Action<ExecutionStatus> updateAction)
        {
            updateAction(this._executeDbLint.ExecutionStatus);
        }

        public void SetFinished(string reportName)
        {
            ThreadStart ts = () =>
            {
                _executeDbLint.ReportName = reportName;
            };
            _executeDbLint.Dispatcher.BeginInvoke(ts);
        }

        /// <summary>
        /// Method used to post messages to the logging listview
        /// </summary>
        /// <param name="msg"></param>
        public void PostMessage(string msg)
        {
            ThreadStart ts = () =>
                                 {
                                     var lv = _executeDbLint.logviewer;
                                     var ic = new ItemContainer(msg);
                                     lv.Items.Add(ic);
                                     lv.ScrollIntoView(ic);
                                 };
            _executeDbLint.Dispatcher.BeginInvoke(ts);
        }

        public DBLintExecuter(ExecuteDBLint executeDbLint)
        {
            var vm = executeDbLint.ViewModel;
            this._viewModel = vm.Clone();
            
            _executeDbLint = executeDbLint;
            _executeDbLint.logviewer.Items.Clear();
            Application.Current.MainWindow.Cursor = Cursors.Wait;
            _executeDbLint.ValidState = false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            ThreadStart ts = () =>
                                 {
                                     _executeDbLint.ValidState = true;
                                     Application.Current.MainWindow.Cursor = Cursors.Arrow;
                                     _executeDbLint.ValidState = true;
                                 };
            _executeDbLint.Dispatcher.BeginInvoke(ts);
        }
    }
}