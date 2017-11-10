using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DBLint.DBLintGui.ExecuteTab;
using ExecutionStatus = DBLint.DBLintGui.ExecuteTab.ExecutionStatus;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for ExecuteDBLint.xaml
    /// </summary>
    public partial class ExecuteDBLint : ValidStateUserControl
    {
        private string _reportName;
        public string ReportName
        {
            get { return _reportName; }
            set
            {
                _reportName = value;
                this.SendButton.IsEnabled = true;
            }
        }

        internal readonly ExecutionStatus ExecutionStatus = new ExecutionStatus();

        private ViewModel _viewModel;
        public ViewModel ViewModel
        {
            get { return _viewModel; }
            set
            {
                _viewModel = value;
                if (_viewModel == null)
                    return;
                this.ExecutionStatus.TotalTables = _viewModel.MetadataSelection.Schemas.Sum(s => (s.Include.HasValue && !s.Include.Value) ? 0 : s.Tables.Value.Count(t => t.Include));
                this.ExecutionStatus.TotalWork = 100;
            }
        }


        public void RunDBLint(DatabaseLint dblint)
        {
            this.ExecutionStatus.FoundIssues = 0;
            this.ExecutionStatus.TablesAnalyzed = 0;
            this.ExecutionStatus.TotalTables = _viewModel.MetadataSelection.Schemas.Sum(s => (s.Include.HasValue && !s.Include.Value) ? 0 : s.Tables.Value.Count(t => t.Include));
            this.ExecutionStatus.TotalWork = 100;

            var worker = new ExecuteWorker(new DBLintExecuter(this), dblint);
            worker.StartWork();
        }

        public ExecuteDBLint()
        {
            ExecuteDBLint dis = this;
            this.ValidState = true;
            InitializeComponent();
            this.infoView.DataContext = ExecutionStatus;
        }

        private void SendButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            new SendReport(ReportName).ShowDialog();
        }
    }
}
