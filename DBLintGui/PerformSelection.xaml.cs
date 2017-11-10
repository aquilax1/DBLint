using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace DBLint.DBLintGui
{

    /// <summary>
    /// Interaction logic for PerformSelection.xaml
    /// </summary>
    public partial class PerformSelection : ValidStateUserControl
    {
        public IEnumerable<Schema> Schemas = null;
        public PerformSelection()
        {
            InitializeComponent();
            schemaSelection.SelectedValue = null;
        }

        private void MakeSelection(object sender, bool checkedStatus)
        {
            var chkBox = sender as CheckBox;
            if (chkBox == null)
                return;
            var schema = chkBox.DataContext as Schema;
            if (schema == null)
                return;
            foreach (var tbl in schema.Tables.Value)
                tbl.Include = checkedStatus;
            chkBox.IsThreeState = false;
        }

        private void IncludeSchema_Checked(object sender, RoutedEventArgs e)
        {
            MakeSelection(sender, true);
            ValidState = true;
            var fe = sender as FrameworkElement;
            if (fe != null)
            {
                var schema = fe.DataContext as Schema;
                if (schema != null)
                {
                    schemaSelection.SelectedValue = schema;
                    var tables = schema.Tables.Value;
                    tableSelection.DataContext = tables.Any() ? tables : null;

                    if (schema.Tables.Value.Count() == 0)
                        schema.Include = false;
                }
            }
        }

        private void IncludeSchema_Unchecked(object sender, RoutedEventArgs e)
        {
            MakeSelection(sender, false);
            ValidState = Schemas.Any(s => !s.Include.HasValue || s.Include.Value);
        }

        private void includeTable_Click(object sender, RoutedEventArgs e)
        {
            var schema = this.schemaSelection.SelectedItem as Schema;
            if (schema != null)
            {
                if (schema.Tables.Value.All(p => p.Include))
                    schema.Include = true;
                else if (schema.Tables.Value.All(p => !p.Include))
                    schema.Include = false;
                else
                    schema.Include = null;
            }
        }

        private void IncludeSchema_Indeterminate(object sender, RoutedEventArgs e)
        {
            ValidState = true;
        }

        private void ValidStateUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            tableSelection.DataContext = null;
            var metadatalight = e.NewValue as MetadataLight;
            if (metadatalight != null)
            {
                var list = metadatalight.Schemas;
                Schemas = list;
            }
            else
                Schemas = null;
            ValidState = false;
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var elem = sender as FrameworkElement;
            if (elem != null)
            {
                var schema = elem.DataContext as Schema;
                if (schema != null)
                {
                    var tables = schema.Tables.Value;
                    tableSelection.DataContext = tables.Any() ? tables : null;
                }
                else
                    this.tableSelection.DataContext = null;
            }
        }

        private void ChangeSelection_Click(object sender, RoutedEventArgs e)
        {
            bool includeValue = sender == SelectMatches;
            var tables = this.tableSelection.DataContext as IEnumerable<Table>;
            if (tables == null)
                return;

            Regex re;
            try
            {
                re = new Regex(Regex.Text, RegexOptions.Compiled);
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid regex");
                return;
            }
            foreach (var table in tables)
            {
                if (re.IsMatch(table.Name))
                {
                    table.Include = includeValue;
                }
            }
            var schema = schemaSelection.SelectedValue as Schema;
            if (schema != null)
            {
                bool all = tables.All(t => t.Include);
                bool some = tables.Any(t => t.Include);
                if (all)
                    schema.Include = true;
                else if (some)
                    schema.Include = null;
                else
                    schema.Include = false;
            }
        }

        private void Regex_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex re;
            try
            {
                re = new Regex(Regex.Text, RegexOptions.Compiled);
                Regex.Foreground = Brushes.Black;
            }
            catch (Exception)
            {
                Regex.Foreground = Brushes.Red;
                return;
            }
            if (tableSelection == null)
                return;
            var tables = this.tableSelection.DataContext as IEnumerable<Table>;
            if (tables == null)
                return;

            foreach (var table in tables)
            {
                if (re.IsMatch(table.Name))
                    table.Color = Brushes.Blue;
                else
                    table.Color = Brushes.Black;
            }
        }
    }
}
