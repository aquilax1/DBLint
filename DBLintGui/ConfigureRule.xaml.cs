using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using DBLint.RuleControl;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for ConfigureRule.xaml
    /// </summary>
    public partial class ConfigureRule : Window
    {
        private List<Property> _properties;
        private IExecutable executable;

        private class TemplateSelector : DataTemplateSelector
        {
            private ConfigureRule _configureRule;

            public TemplateSelector(ConfigureRule configureRule)
            {
                this._configureRule = configureRule;
            }

            public override DataTemplate SelectTemplate(object item, DependencyObject container)
            {
                var prop = item as Property;
                var elem = container as FrameworkElement;
                if (elem == null || prop == null)
                    return null;

                Type[] boolTypes = new[] { typeof(bool) };

                DataTemplate template;
                if (boolTypes.Any(t => t.Equals(prop.ValueType)))
                    template = elem.FindResource("boolTemplate") as DataTemplate;
                else if (prop.ValueType.IsEnum)
                    template = elem.FindResource("enumsTemplate") as DataTemplate;
                else if (prop.ValueType == typeof(SQLCode))
                    template = elem.FindResource("SQLTemplate") as DataTemplate;
                else
                    template = elem.FindResource("stringTemplate") as DataTemplate;
                return template;
            }
        }
        private class Property : IDataErrorInfo
        {
            private IProperty _property;
            public Type ValueType { get { return this._property.GetValue().GetType(); } }
            public string _value;
            public object ValueDirect
            {
                get
                {
                    return _property.GetValue();
                }
                set
                {
                    if (value != null && _property.isValidPropertyValue(value))
                        _property.SetValue(value);
                }
            }
            public string Value
            {
                get { return _property.GetValue().ToString(); }
                set
                {
                    _value = value;
                    if (value != null && _property.isValidPropertyValue(value))
                        _property.SetValue(value);
                }
            }
            public string Name { get { return _property.Name; } }
            public string Description { get { return _property.Description; } }

            public IEnumerable<Object> Values
            {
                get
                {
                    var values = new List<object>();
                    foreach (var item in this.ValueType.GetEnumValues())
                        values.Add(item);
                    return values;
                }
            }

            public Property(IProperty property)
            {
                this._property = property;
                this._value = property.GetValue().ToString();
            }

            public string this[string columnName]
            {
                get
                {
                    if (columnName == "Value" && !_property.isValidPropertyValue(_value))
                        return _property.Description;
                    return null;
                }
            }

            public string Error
            {
                get { return ""; }
            }

            public void Reset()
            {
                this._property.SetValue(_property.GetDefaultValue());
            }
        }

        public ConfigureRule(IExecutable executable)
        {            
            InitializeComponent();
            this.executable = executable;
            this.Title = string.Format("Configure '{0}'", executable.Name);
            ValueColumn.CellTemplateSelector = new TemplateSelector(this);
            this._properties = (from p in executable.GetProperties()
                                select new Property(p)).ToList();
            this.DataContext = this._properties;

            ThreadStart setColWidth = () => OptionColumn.Width = OptionColumn.ActualWidth;
            Dispatcher.BeginInvoke(setColWidth);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.executable is SQLRule)
            {
                var sqlRule = (SQLRule)this.executable;
                var oldName = sqlRule.RuleName.DefaultValue;
                var newName = sqlRule.RuleName.Value;
                if (oldName != newName)
                {
                    SQLRuleCollection.RemoveSQLRule(oldName);
                    sqlRule.RuleName.DefaultValue = newName;
                }
            }
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var elem = (sender as FrameworkElement);

            if (elem != null)
            {
                var property = elem.DataContext as Property;
                if (property != null)
                    property.Reset();
            }
            this.DataContext = null;
            this.DataContext = this._properties;
        }

    }
}
