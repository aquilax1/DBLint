using System;
using System.Collections.Generic;
using System.Linq;
using DBLint.DataAccess;

namespace DBLint.DBLintGui
{
    public class MetadataLight
    {
        private IEnumerable<Schema> _schemas;
        [NonSerialized]
        public Extractor Extractor;
        [NonSerialized]
        public Connection Connection;

        [NonSerialized]
        private bool _validConnection = false;

        public IEnumerable<Schema> Schemas
        {
            get
            {
                if (!_validConnection)
                    return Enumerable.Empty<Schema>();
                if (_schemas == null)
                {
                    var schemas = Extractor.Database.GetSchemas();
                    
                    this._schemas = (from s in schemas
                                     where this.Connection.DBMS != DBMSs.MYSQL || s.SchemaName==this.Connection.Database
                                     select new Schema(s, Extractor.Database)).ToList();
                    
                }
                return _schemas;
            }
            set { _schemas = value; }
        }

        public MetadataLight(Connection conn)
        {
            Extractor = new DBLint.DataAccess.Extractor(conn);
            _validConnection = new ConnectionTester(conn).TestConnection();
            
            //Extractor.Database.TestConnection();
            this.Connection = conn;
        }

        public MetadataLight Clone()
        {
            var mdl = new MetadataLight(this.Connection);
            mdl._schemas = (from s in this._schemas
                            select s.Clone()).ToList();
            mdl.Extractor = Extractor;
            return mdl;
        }
    }
}