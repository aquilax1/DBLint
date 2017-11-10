using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for SendReport.xaml
    /// </summary>
    public partial class SendReport : Window
    {
        private readonly string _reportDir;
        private readonly string _reportName;
        private readonly string _mainPageWithPath;
        private readonly string _reducedPageWithPath;

        public bool Encrypt { get; set; }
        public SendReport(string reportName)
        {
            Encrypt = true;
            this._reportDir = Path.Combine("reports", reportName);
            this._reportName = reportName;
            _mainPageWithPath = Path.Combine(_reportDir, "mainpage.html");
            _reducedPageWithPath = Path.Combine(_reportDir, "mainpage-reduced.html");
            File.Delete(_reducedPageWithPath);
            InitializeComponent();
        }

        private IEnumerable<string> GetMainPages(bool limited)
        {
            if (limited)
            {
                MessageBox.Show(_mainPageWithPath);
                var content = Util.Utils.ExtractReportSummary(_mainPageWithPath);
                File.WriteAllText(_reducedPageWithPath, content);
                yield return _reducedPageWithPath;
            }
            else
            {
                yield return _mainPageWithPath;
            }
            yield break;
        }

        private IEnumerable<string> GetTables()
        {
            var tablesDir = Path.Combine(_reportDir, "tables");

            foreach (var file in Directory.EnumerateFiles(tablesDir))
            {
                yield return Path.Combine(file);
            }
            yield break;
        }

        private IEnumerable<string> GetIssues()
        {
            var tablesDir = Path.Combine(_reportDir, "issues");

            foreach (var file in Directory.EnumerateFiles(tablesDir))
            {
                yield return Path.Combine(file);
            }
            yield break;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (ExitAction ea = ExitAction.Create(orig => this.Cursor = orig, this.Cursor))
                {
                    this.Cursor = Cursors.Wait;

                    var list = new List<string>();
                    if (sender == tables)
                    {
                        list.AddRange(GetMainPages(false));
                        list.AddRange(GetIssues());
                        list.AddRange(GetTables());
                    }
                    else if (sender == issue)
                    {
                        list.AddRange(GetMainPages(false));
                        list.AddRange(GetIssues());
                    }
                    else if (sender == summary)
                    {
                        list.AddRange(GetMainPages(true));
                    }

                    var files = from file in list
                                select new KeyValuePair<string, string>(file, File.ReadAllText(file));
                    files = files.ToArray();

                    var archive = _reportName + "-report.zip";
                    Util.Zipper.ZipFiles(files, archive, Encrypt);

                    //Some evil mail clients overrides currentDirectory. Yes we are looking at you Outlook.
                    using (var pathSetter = ExitAction.Create(origPath => Environment.CurrentDirectory = origPath, Environment.CurrentDirectory))
                    {
                        var mapi = new Util.MAPI();
                        mapi.AddAttachment(Path.GetFullPath(archive));
                        mapi.AddRecipientTo("bkrogh@cs.aau.dk");
                        string content = string.Format("Report for database '{0}' attached", _reportName);
                        mapi.SendMailPopup("DBLint Report", content);
                    }
                }
                this.Close();
                MessageBox.Show("Thank you for sending feedback of our tool.\nWe hope that you found DBLint useful\n",
                                "Thank you!");

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.StackTrace, exception.GetType().Name);
                throw;
            }
        }
    }
}
