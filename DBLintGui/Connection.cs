using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DBLintGui
{
    [DataContract]
    public class Connection
    {
        [DataMember]
        public string Database { get; set; }
        [DataMember]
        public string Host { get; set; }
        [DataMember]
        public string DBMS { get; set; }
        [DataMember]
        public string Port { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string Password { get; set; }
        public override bool Equals(object obj)
        {
            var conn = obj as Connection;
            if (conn == null)
                return false;
            return conn.Database == Database && conn.Host == Host && conn.DBMS == DBMS && conn.Port == Port &&
                   conn.UserName == UserName && conn.Password == Password;
        }
    }

    [DataContract]
    internal class ConnectionConfiguration : INotifyPropertyChanged
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
                _lastConnection = value;
                ConnectionSerializer.SaveConnectionConfiguration(this);
                Notify("LastConnection");
            }
        }
    }

    internal static class ConnectionSerializer
    {
        private static DataContractSerializer dcs = new DataContractSerializer(typeof(ConnectionConfiguration));
        private static string filename = "ConnectionStrings.xml";

        public static ConnectionConfiguration GetConnectionConfiguration()
        {
            ConnectionConfiguration res;
            lock (filename)
                try
                {
                    using (var fs = File.OpenRead(filename))
                    {

                        res = (ConnectionConfiguration)dcs.ReadObject(fs);
                        return res;
                    }
                }
                catch (Exception e)
                {
                    return new ConnectionConfiguration();

                }

        }

        public static void SaveConnectionConfiguration(ConnectionConfiguration conf)
        {
            lock (filename)
                using (var fs = File.Open(filename, FileMode.Create))
                {
                    dcs.WriteObject(fs, conf);
                }
        }

        public static Connection GetLastConnection()
        {
            var res = GetConnectionConfiguration();
            return res.LastConnection;
        }

        public static void SetLastConnection(Connection last)
        {
            var res = GetConnectionConfiguration();
            res.LastConnection = last;
            SaveConnectionConfiguration(res);
        }

        public static void SaveConnections(IEnumerable<Connection> connections)
        {
            ConnectionConfiguration conf = GetConnectionConfiguration();
            conf.Connections = new ObservableCollection<Connection>(connections);
            SaveConnectionConfiguration(conf);
        }
    }
}
