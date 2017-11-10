using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace DBLint.DataAccess
{
    public enum AuthenticationMethod
    {
        [DisplayNameAttribute("SQL Authentication")]
        SQLAuthentication,
        [DisplayNameAttribute("Windows Authentication")]
        WindowsAuthentication
    }

    [DataContract]
    public class Connection : INotifyPropertyChanged
    {
        private string _name;

        [DataMember]
        private AuthenticationMethod _authenticationMethod = AuthenticationMethod.SQLAuthentication;

        [XmlElement("databasesystem")]
        public DBMSs DBMS
        {
            get { return _dbms; }
            set { _dbms = value; this.notify("DBMS"); }
        }

        [DataMember]
        [XmlElement("database")]
        public string Database { get; set; }

        [DataMember]
        [XmlElement("host")]
        public string Host { get; set; }

        [DataMember]
        private string _port = "5432";

        [XmlElement("port")]
        public string Port
        {
            get { return _port; }
            set
            {
                _port = value;
                notify("Port");
            }
        }

        [DataMember]
        [XmlElement("username")]
        public string UserName { get; set; }
        
        [DataMember]
        [XmlElement("password")]
        public string Password { get; set; }

        [XmlElement("authmode")]
        public AuthenticationMethod AuthenticationMethod
        {
            get { return _authenticationMethod; }
            set { _authenticationMethod = value; notify("AuthenticationMethod"); }
        }

        [DataMember]
        [XmlAttribute("name")]
        public string Name
        {
            get { return _name ?? this.Database; }
            set { _name = value; }
        }

        [DataMember]
        private DBMSs _dbms = DBMSs.NONE;

        private string _maxConnections;

        [DataMember]
        [XmlElement("maxconnections")]
        public string MaxConnections
        {
            get { return this._maxConnections ?? "4"; }
            set
            {
                this._maxConnections = value;
            }
        }

        public override bool Equals(object obj)
        {
            var conn = obj as Connection;
            if (conn == null)
                return false;
            return conn.Database == Database && conn.Host == Host && conn.DBMS == DBMS && conn.Port == Port &&
                   conn.UserName == UserName && conn.Password == Password;
        }
        public override int GetHashCode()
        {
            return this._dbms.GetHashCode() * 3 + (this._name ?? "").GetHashCode() * 5 + (this.Password ?? "").GetHashCode() * 7 +
                   (this.Database ?? "").GetHashCode() * 11 + (this.Host ?? "").GetHashCode() * 13;
        }
        private void notify(string property)
        {
            if (this.PropertyChanged == null)
                return;
            this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
