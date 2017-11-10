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
using System.Windows.Shapes;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for SaveConfigOptions.xaml
    /// </summary>
    public partial class SaveConfigOptions : Window
    {
        public string FileName { get; set; }

        public SaveConfigOptions()
        {
            InitializeComponent();
        }

        private void ChooseFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
            saveDialog.FileName = "DBLintConfig";
            saveDialog.DefaultExt = ".xml";
            saveDialog.Filter = "XML Document (.xml)|*.xml";

            bool? result = saveDialog.ShowDialog();

            if (result == true)
            {
                this.FileName = saveDialog.FileName;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                this.DialogResult = false;
            }
        }
    }
}
