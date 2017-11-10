using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DBLint.DataAccess;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for AddConnectionWindow.xaml
    /// </summary>
    public partial class AddConnectionWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _validState;
        public bool ValidState
        {
            get { return _validState; }
            set
            {
                if (value != _validState)
                {
                    _validState = value;
                    if (this.PropertyChanged != null)
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ValidState"));
                }
            }
        }

        public AddConnectionWindow()
        {
            InitializeComponent();
        }



        private void buttonTestConnection_Click(object sender, RoutedEventArgs e)
        {
            var conn = this.DataContext as Connection;
            this.ValidState = false;

            if (conn != null)
            {
                var tc = new ConnectionTester(conn);
                bool validConnection = tc.TestConnection();
                if (validConnection)
                {
                    this.ValidState = true;
                    MessageBox.Show("Success");
                } 

            }
        }

        private void addConnection_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var conn = this.DataContext as Connection;
            if (conn == null) return;

            conn.Password = this.passwordBox.Password;
            this.ValidState = false;
        }

        private void dbmsSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dbmsSelector.SelectedValue == null)
                return;
            var dbms = (DBMSs)dbmsSelector.SelectedValue;
            var conn = this.DataContext as Connection;
            this.ValidState = false;
            if (conn == null)
                return;
            switch (dbms)
            {
                case DBMSs.MSSQL:
                    conn.Port = "1433";
                    break;
                case DBMSs.MYSQL:
                    conn.Port = "3306";
                    break;
                case DBMSs.POSTGRESQL:
                    conn.Port = "5432";
                    break;
                case DBMSs.ORACLE:
                    conn.Port = "1521";
                    break;
                case DBMSs.FIREBIRD:
                    conn.Port = "3050";
                    break;
                case DBMSs.DB2:
                    conn.Port = "50000";
                    break;
            }

            this.portTextbox.DataContext = conn;
        }

        private void authenticationTypeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
