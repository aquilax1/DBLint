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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DBLint.RuleControl;

namespace DBLint.DBLintGui
{
     public delegate void DeleteRuleHandler(Rule rule);

    /// <summary>
    /// Interaction logic for SelectRules.xaml
    /// </summary>
    public partial class SelectRules : ValidStateUserControl
    {
        private RulesConfiguration _ruleConf;
        public RulesConfiguration RuleConf
        {
            get { return _ruleConf; }
            set
            {
                _ruleConf = value;
                if (_ruleConf != null)
                {
                    this.ValidState = this.RuleConf.RuleSets.Any(r => !r.Include.HasValue || r.Include.Value);
                }
                else
                {
                    this.ValidState = false;
                }
            }
        }

        public event DeleteRuleHandler OnDeleteRule;

        public SelectRules()
        {
            ValidState = false;
            InitializeComponent();
        }

        private void MakeSelection(object sender, bool checkedState)
        {
            var chkBox = sender as CheckBox;
            if (chkBox == null)
                return;
            var ruleSet = chkBox.DataContext as RuleSet;
            if (ruleSet == null)
                return;
            this.rulesSetsView.SelectedValue = ruleSet;
            foreach (var rule in ruleSet.Rules)
            {
                rule.Include = checkedState;
            }
            chkBox.IsThreeState = false;
        }

        private void includeRuleSet_Checked(object sender, RoutedEventArgs e)
        {
            MakeSelection(sender, true);
            this.ValidState = true;
        }

        private void includeRuleSet_Unchecked(object sender, RoutedEventArgs e)
        {
            MakeSelection(sender, false);
            this.ValidState = this.RuleConf.RuleSets.Any(r => !r.Include.HasValue || r.Include.Value);
        }

        private void includeRuleSet_Indeterminate(object sender, RoutedEventArgs e)
        {
            this.ValidState = true;
        }

        private void includeRule_Click(object sender, RoutedEventArgs e)
        {
            var ruleset = rulesSetsView.SelectedValue as RuleSet;
            if (ruleset == null)
                return;
            if (ruleset.Rules.All(p => p.Include))
                ruleset.Include = true;
            else if (ruleset.Rules.All(p => !p.Include))
                ruleset.Include = false;
            else
                ruleset.Include = null;
        }

        private void configureLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var label = sender as Label;
            if (label != null)
            {
                var rule = label.DataContext as Rule;
                if (rule != null)
                {
                    var configureDialog = new ConfigureRule(rule.Executable);
                    if (rule.Executable.IsSQLRule())
                    {
                        configureDialog.Width = 750;
                        configureDialog.Height = 350;
                        configureDialog.OptionGridView.Columns[1].Width = 525;
                    }
                    configureDialog.ShowDialog();
                    List<Rule> rules = _ruleConf.RuleSets.SelectMany(rs => rs.Rules).ToList();
                    List<IConfigurable> configurables = rules.Where(r => r.Executable is IConfigurable).Select(r => (IConfigurable)r.Executable).ToList();
                    rule.ExecutableUpdated();
                }
            }
        }

        private void ResetConfigurationButton_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<Rule> test = this._ruleConf.RuleSets.SelectMany(r => r.Rules);
            foreach (var rule in test)
            {
                if (rule.IsConfigureable)
                {
                    var configurable = rule.Executable as IConfigurable;
                    if (configurable != null)
                        foreach (var p in configurable.GetProperties())
                            p.SetValue(p.GetDefaultValue());
                }
            }
            if (File.Exists(DBLint.Settings.CONFIG_FILE))
                File.Delete(DBLint.Settings.CONFIG_FILE);
        }

        private void deleteLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var label = sender as Label;
            if (label != null)
            {
                var rule = label.DataContext as Rule;
                if (this.OnDeleteRule != null)
                {
                    this.OnDeleteRule(rule);
                }
            }
        }
    }

    public class RulesConfiguration : NotifyerClass
    {
        public IEnumerable<RuleSet> RuleSets { get; set; }

        public RulesConfiguration Clone()
        {
            var rc = new RulesConfiguration();
            rc.RuleSets = (from rs in this.RuleSets
                           select rs.Clone()).ToList();
            return rc;
        }

        public RuleSet GetSQLRuleSet()
        {
            if (this.RuleSets != null)
            {
                return this.RuleSets.Where(r => r.IsSQLRuleSet).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
    }


    public class RuleSet : NotifyerClass
    {
        public string Name { get; set; }
        private bool? _include = true;
        private IEnumerable<Rule> rules;
        public bool IsSQLRuleSet { get; set; }

        public bool? Include
        {
            get { return _include; }
            set { _include = value; Notify("Include"); }
        }

        public IEnumerable<Rule> Rules
        {
            get { return this.rules; }
            set { this.rules = value; Notify("Rules"); }
        }

        public RuleSet Clone()
        {
            var rs = new RuleSet();
            rs.Name = this.Name;
            rs._include = this._include;
            rs.Rules = (from r in Rules
                       select r.Clone()).ToList();
            return rs;
        }
    }

    public class Rule : NotifyerClass
    {
        public string Name { get { return this.Executable.Name; } }
        private bool _include = true;
        public bool Include
        {
            get { return _include; }
            set { _include = value; Notify("Include"); }
        }

        public bool IsConfigureable { get { return this.Executable.GetProperties().Count() > 0; } }
        public bool IsDeletable { get { return this.Executable.IsSQLRule(); } }

        public readonly IExecutable Executable;
        public bool ScheduledForExecution { get; set; }

        public bool DataRule { get; set; }

        public Rule(IExecutable executable)
        {
            this.Executable = executable;
        }

        public Rule Clone()
        {
            var r = new Rule(this.Executable);
            r._include = this._include;
            r.DataRule = this.DataRule;
            return r;
        }

        public void ExecutableUpdated()
        {
            Notify("Name");
        }
    }
}
