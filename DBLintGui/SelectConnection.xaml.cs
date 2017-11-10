using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DBLint.DataAccess;

namespace DBLint.DBLintGui
{
    /// <summary>
    /// Interaction logic for SelectConnection.xaml
    /// </summary>
    public partial class SelectConnection : ValidStateUserControl
    {
        public delegate void ConnectionChangedEvent(object sender, Connection conn);
        public event ConnectionChangedEvent ConnectionChanged;

        private readonly ConnectionConfiguration _connectionConfiguration;

        public ConnectionConfiguration ConnectionConfiguration 
        {
            get { return this._connectionConfiguration; }
        }

        public override bool ValidState
        {
            get
            {
                return base.ValidState;
            }
            set
            {
                if (value == true && this.ViewModel != null && _connectionConfiguration.LastConnection != null)
                {
                    this.ViewModel.CurrentConnection = _connectionConfiguration.LastConnection;
                    base.ValidState = true;
                    return;
                }
                if (this.ViewModel != null)
                    this.ViewModel.CurrentConnection = null;
                base.ValidState = false;
            }
        }

        private ViewModel _viewModel;
        public ViewModel ViewModel
        {
            get { return _viewModel; }
            set
            {
                _viewModel = value;
                SetValidState();
            }
        }

        private void SetValidState()
        {
            var last = _connectionConfiguration.LastConnection;
            this.ValidState = last != null;
        }

        public SelectConnection()
        {
            _connectionConfiguration = ConnectionSerializer.GetConnectionConfiguration();

            InitializeComponent();
            comboConnections.DataContext = _connectionConfiguration;
        }

        private void addNewConnection_Click(object sender, RoutedEventArgs e)
        {
            var w = new AddConnectionWindow();
            var conn = new Connection();
            w.DataContext = conn;
            var res = w.ShowDialog();
            if (res.HasValue && res.Value)
            {
                _connectionConfiguration.Connections.Insert(0, conn);
                DBLint.DBLintGui.ConnectionSerializer.SaveConnectionConfiguration(_connectionConfiguration);
                comboConnections.SelectedItem = conn;
            }
        }

        private void deleteConnection_Click(object sender, RoutedEventArgs e)
        {
            var conn = comboConnections.SelectedValue as Connection;
            if (conn == null)
                return;
            ValidState = false;
            this._connectionConfiguration.Connections.Remove(conn);
        }

        private void comboConnections_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var conn = this.comboConnections.SelectedValue as Connection;
            if (this.ConnectionChanged != null)
                this.ConnectionChanged.Invoke(this, this._connectionConfiguration.LastConnection);
            if (conn == null)
            {
                this.ValidState = false;
            }
            else
            {
                this.ValidState = true;
            }
        }

        private void comboConnections_Loaded(object sender, RoutedEventArgs e)
        {
            if (this._connectionConfiguration.LastConnection != null)
            {
                ValidState = true;
            }
        }

        private void editConnection_Click(object sender, RoutedEventArgs e)
        {
            var conn = comboConnections.SelectedValue as Connection;
            if (conn == null)
                return;
            var w = new AddConnectionWindow();
            w.DataContext = conn;
            w.ShowDialog();
            comboConnections.SelectedItem = null;
            this._connectionConfiguration.Connections.Remove(conn);
            this._connectionConfiguration.Connections.Insert(0, conn);
            comboConnections.SelectedItem = conn;
            ConnectionSerializer.SaveConnectionConfiguration(this._connectionConfiguration);
        }
    }
}
