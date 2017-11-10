using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using DBLint.DataAccess;

namespace DBLint.DBLintGui
{
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
                catch
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