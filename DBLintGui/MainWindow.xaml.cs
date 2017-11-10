using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DBLint.DataAccess;
using DBLint.Model;
using DBLint.RuleControl;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if !DEBUG
        public static StartupScreen StartupScreen = new StartupScreen();
#endif
        ViewModel vm = new ViewModel();
        private readonly DatabaseLint dblint = new DBLint.DatabaseLint(Settings.XMLCONFIG);

        public MainWindow()
        {
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);

            InitializeComponent();
            this.DataContext = vm;
            SelectConnection.ViewModel = vm;
            
            this.selectRules.NewRuleButton.Click += new RoutedEventHandler(delegate(Object sender, RoutedEventArgs args)
            {
                String ruleName = "User-defined rule " + (dblint.AllExecutables.GetSQLRules().Count+1);
                SQLRule newRule = SQLRuleCollection.AddSQLRule(ruleName);
                this.dblint.AllExecutables.AddExecutable(newRule);
                this.updateSQLRuleSet();
                
            });

            this.selectRules.OnDeleteRule += new DeleteRuleHandler(delegate(Rule rule)
            {
                SQLRuleCollection.RemoveSQLRule(rule.Name);
                this.dblint.AllExecutables.RemoveExecutable(rule.Executable);
                this.updateSQLRuleSet();
            });

            this.selectRules.SaveConfigButon.Click += new RoutedEventHandler(delegate(Object sender, RoutedEventArgs args)
            {
                var saveConfigOptions = new SaveConfigOptions();
                var res = saveConfigOptions.ShowDialog();
                if (res.HasValue && res.Value == true)
                {
                    this.saveConfig(saveConfigOptions);
                }
            });

            this.selectRules.LoadConfigButon.Click += new RoutedEventHandler(delegate(Object sender, RoutedEventArgs args)
            {
                Microsoft.Win32.OpenFileDialog loadDialog = new Microsoft.Win32.OpenFileDialog();
                loadDialog.DefaultExt = ".xml";
                loadDialog.Filter = "XML Document (.xml)|*.xml";

                bool? result = loadDialog.ShowDialog();

                if (result == true)
                {
                    this.loadConfig(loadDialog.FileName);
                }
            });

            this.loadConfig(Settings.XMLCONFIG);

            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            MainWindow.StartupScreen.Hide();
#endif
        }

        private void loadConfig(string filename)
        {
            IConfigFile configFile = new XMLConfigFile(filename);

            if (!configFile.IsValid())
            {
                MessageBox.Show("Configuration file is not valid");
                return;
            }

            //Connection
            var connection = configFile.GetConnection();
            if (connection != null)
            {
                //Only add loaded connection if a similar connection doesnt already exist
                var existingConn = this.SelectConnection.ConnectionConfiguration.Connections.Where(c => c.Equals(connection)).FirstOrDefault();
                if (existingConn == null)
                {
                    this.SelectConnection.ConnectionConfiguration.Connections.Insert(0, connection);
                    DBLint.DBLintGui.ConnectionSerializer.SaveConnectionConfiguration(this.SelectConnection.ConnectionConfiguration);
                }
                else
                {
                    connection = existingConn;
                }


                this.SelectConnection.comboConnections.SelectedItem = connection;
                this.SelectConnection_ConnectionChanged(this, connection);

                foreach (var schema in this.performSelection.Schemas)
                {
                    schema.Include = false;
                    this.performSelection.schemaSelection.SelectedValue = schema;

                    var tables = schema.Tables.Value;
                    this.performSelection.tableSelection.DataContext = tables.Any() ? tables : null;
                }
                this.performSelection.ValidState = true;

            }

            //Configure rules
            foreach (var e in this.dblint.AllExecutables)
            {
                configFile.ConfigureRule(e);
            }

            //Rule selection
            if (configFile.GetRulesToRun().Count() > 0)
            {
                this.initializeSelectRules();

                var rulesets = this.selectRules.RuleConf.RuleSets;

                foreach (var ruleset in rulesets)
                {
                    ruleset.Include = false;
                    foreach (var rule in ruleset.Rules)
                    {
                        bool include = configFile.RunRule(rule.Executable);
                        if (include == true)
                        {
                            rule.Include = true;
                            ruleset.Include = true;
                        }
                        else
                        {
                            rule.Include = false;
                        }
                    }
                }
                this.selectRules.rulesSetsView.SelectedIndex = 0;
            }

            //Table selection
            if (configFile.GetTablesToCheck().Count() > 0)
            {
                foreach (var schema in this.vm.MetadataSelection.Schemas)
                {
                    foreach (var table in schema.Tables.Value)
                    {
                        if (configFile.CheckTable(table.Name, schema.Name))
                        {
                            table.Include = true;
                        }
                        else
                        {
                            table.Include = false;
                        }
                    }
                    if (schema.Tables.IsValueCreated && schema.Tables.Value.Any(t => t.Include == true))
                    {
                        schema.Include = true;
                    }
                }
            }
        }

        void saveConfig(SaveConfigOptions saveConfigOptions)
        {
            if (saveConfigOptions.FileName != null)
            {
                Configuration config = new Configuration();

                if (saveConfigOptions.ExportConnection.IsChecked == true)
                {
                    config.Connection = this.vm.CurrentConnection;
                }

                if (saveConfigOptions.ExportRules.IsChecked == true)
                {
                    config.AllRules = this.dblint.AllExecutables.Where(ex => ex.IsRule());
                    config.RulesToRun = this.getRulesToRun();
                }
                else
                {
                    config.AllRules = this.dblint.AllExecutables.Where(ex => ex.IsSQLRule() == true);
                    config.RulesToRun = this.getRulesToRun().Where(r => r.IsSQLRule() == true);
                }

                if (saveConfigOptions.ExportSQLRules.IsChecked == false)
                {
                    config.AllRules = config.AllRules.Where(r => r.IsSQLRule() == false);
                    config.RulesToRun = config.RulesToRun.Where(r => r.IsSQLRule() == false);
                }

                if (saveConfigOptions.ExportTables.IsChecked == true)
                {
                    config.TablesToCheck = this.getTablesToCheck();
                }

                IConfigFile file = new XMLConfigFile(saveConfigOptions.FileName);
                file.Save(config);
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.dblint.Config.Connection = this.vm.CurrentConnection;
            this.dblint.Config.AllRules = this.dblint.AllExecutables.Where(ex => ex.IsRule());
            this.dblint.Config.RulesToRun = this.getRulesToRun();
            this.dblint.Config.TablesToCheck = this.getTablesToCheck();
            this.dblint.SaveConfiguration();
        }

        private IEnumerable<TableID> getTablesToCheck()
        {
            List<TableID> tablesToCheck = new List<TableID>();
            if (vm.MetadataSelection == null || vm.MetadataSelection.Schemas == null)
            {
                return tablesToCheck;
            }
            var schemas = this.vm.MetadataSelection.Schemas;
            foreach (var schema in schemas)
            {
                foreach (var table in schema.Tables.Value)
                {
                    if (table.Include)
                    {
                        var tableID = new TableID(null, schema.Name, table.Name);
                        tablesToCheck.Add(tableID);
                    }
                }
            }
            return tablesToCheck;
        }

        private List<IExecutable> getRulesToRun()
        {
            var rulesToRun = new List<IExecutable>();
            if (vm.RulesConfiguration != null)
            {
                foreach (var ruleSet in this.vm.RulesConfiguration.RuleSets)
                    foreach (var rule in ruleSet.Rules)
                        if (rule.Include)
                            rulesToRun.Add(rule.Executable);
            }
            return rulesToRun;
        }

        private void updateSQLRuleSet()
        {
            //Add rule to gui rule set
            var sqlRuleSet = this.selectRules.RuleConf.GetSQLRuleSet();
            if (sqlRuleSet != null)
            {
                sqlRuleSet.Rules = (from r in dblint.AllExecutables.GetSQLRules()
                                    orderby r.Name
                                    select new Rule(r)).ToList();
            }

            //Change view to "user-defined rules"
            int sqlRulesIndex = this.selectRules.rulesSetsView.Items.IndexOf(this.selectRules.RuleConf.GetSQLRuleSet());
            this.selectRules.rulesSetsView.SelectedIndex = sqlRulesIndex;
        }

        private void selectRules_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            using (var ea = ExitAction.Create(orig => this.Cursor = orig, this.Cursor))
            {
                this.Cursor = Cursors.Wait;
                if (selectRules.IsVisible && selectRules.RuleConf == null)
                {
                    initializeSelectRules();
                }
            }
        }

        private void initializeSelectRules()
        {
            var rules = from r in dblint.AllExecutables
                        select new RuleConfiguration(r);
            selectRules.DataContext = rules;
            var rulesconf = new RulesConfiguration();
            rulesconf.RuleSets = new RuleSet[]  
                                         {
                                             new RuleSet()
                                             {
                                                 Include = true,
                                                 Name = "Metadata rules",
                                                 Rules =
                                                 (from r in dblint.AllExecutables.GetSchemaRules()
                                                  orderby r.Name
                                                  select new Rule(r)).ToList(),
                                                 IsSQLRuleSet = false
                                             },
                                             new RuleSet()
                                             {
                                                Include = true,
                                                Name = "User-defined rules",
                                                Rules = 
                                                (from r in dblint.AllExecutables.GetSQLRules()
                                                  orderby r.Name
                                                  select new Rule(r){Include = true}).ToList(),
                                                IsSQLRuleSet = true
                                             },
                                             new RuleSet()
                                             {
                                                 Include = false,
                                                 Name = "Data rules",
                                                 Rules =
                                                 (from r in dblint.AllExecutables.GetDataRules()
                                                  orderby r.Name
                                                  select new Rule(r){Include = false}).ToList(),
                                                 IsSQLRuleSet = false
                                             }
                                         };
            vm.RulesConfiguration = rulesconf;
            selectRules.DataContext = selectRules.RuleConf = rulesconf;
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (executeViewer.IsVisible)
            {
                executeButton_Click(sender, e);
                return;
            }
            bool setNext = false;
            foreach (var item in this.tabControl.Items)
            {
                if (setNext)
                {
                    tabControl.SelectedValue = item;
                    break;
                }

                if (item.Equals(tabControl.SelectedValue))
                    setNext = true;
            }
        }

        private void executeButton_Click(object sender, RoutedEventArgs e)
        {
            this.executeViewer.RunDBLint(this.dblint);
        }

        private void executeViewer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.executeViewer.ViewModel = vm;
        }

        private void SelectConnection_ConnectionChanged(object sender, Connection conn)
        {
            if (conn == null)
            {
                this.performSelection.DataContext = null;
                return;
            }
            try
            {
                if (vm.MetadataSelection == null || (vm.CurrentConnection != null && !this.vm.CurrentConnection.Equals(conn)))
                {
                    var metadataLight = new MetadataLight(conn);
                    this.performSelection.DataContext = vm.MetadataSelection = metadataLight;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void performSelection_ValidStateChanged(object sender, EventArgs e)
        {
            startupScreen.ValidState = performSelection.ValidState;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void selectRules_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
