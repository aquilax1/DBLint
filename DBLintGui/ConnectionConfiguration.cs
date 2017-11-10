using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using DBLint.DataAccess;

namespace DBLint.DBLintGui
{
    [DataContract]
    public class ConnectionConfiguration : INotifyPropertyChanged
    {

        [DataMember]
        private ObservableCollection<Connection> _connections;
        [DataMember]
        private Connection _lastConnection;

        public event PropertyChangedEventHandler PropertyChanged;

        public ConnectionConfiguration()
        {
            _connections = new ObservableCollection<Connection>();
            _lastConnection = null;
        }

        private void Notify(string property)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }

        public ObservableCollection<Connection> Connections
        {
            get { return _connections; }
            set
            {
                _connections = value;
                ConnectionSerializer.SaveConnectionConfiguration(this);
                Notify("Connections");
            }
        }

        public Connection LastConnection
        {
            get { return _lastConnection; }
            set
            {
                if (_lastConnection == value)
                    return;
                _lastConnection = value;
                ConnectionSerializer.SaveConnectionConfiguration(this);
                Notify("LastConnection");
            }
        }
    }
}