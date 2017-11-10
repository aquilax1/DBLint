using System;
using System.Collections.Generic;
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

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for NextButton.xaml
    /// </summary>
    public partial class NextButton : UserControl
    {
        public NextButton()
        {
            InitializeComponent();
        }

        private TabControl _tabControl;
        public TabControl TabControl
        {
            get { return _tabControl; }
            set
            {
                DataContext = _tabControl = value;
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            TabControl tabcontrol = this.TabControl;
            DependencyObject current = this;

            while (tabcontrol == null)
            {
                if (current == null)
                    return;
                current = VisualTreeHelper.GetParent(current);
                tabcontrol = current as TabControl;
                if (tabcontrol != null)
                    break;
            }

            bool setNext = false;
            foreach (var item in tabcontrol.Items)
            {
                if (setNext)
                {
                    tabcontrol.SelectedValue = item;
                    break;
                }
                if (item.Equals(tabcontrol.SelectedValue))
                    setNext = true;
            }
        }
    }
}
