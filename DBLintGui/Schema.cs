using System;
using System.Collections.Generic;
using System.Linq;
using DBLint.DataAccess;

namespace DBLint.DBLintGui
{
    public class Schema : NotifyerClass
    {
        public string Name { get; set; }

        private bool? _include = false;

        public bool? Include
        {
            get { return _include; }
            set { _include = value; Notify("Include"); }
        }

        private IEnumerable<Table> GetTables()
        {
            try
            {
                var tables = _provider.GetTables(this.Name);
                var res = (from t in tables
                           orderby t.TableName
                           select new Table(t));
                return res.ToList();
            }
            catch
            {
                return Enumerable.Empty<Table>();
            }
        }

        private Lazy<IEnumerable<Table>> _tables;
        private Factory _provider;

        public Lazy<IEnumerable<Table>> Tables
        {
            get
            {
                if (_tables == null)
                    _tables = new Lazy<IEnumerable<Table>>(GetTables);
                return _tables;
            }
        }
        public Schema(DataAccess.DBObjects.Schema schema, Factory provider)
        {
            this._provider = provider;
            this.Name = schema.SchemaName;
            this.DBSchema = schema;
        }

        public DataAccess.DBObjects.Schema DBSchema { get; set; }

        public Schema Clone()
        {
            var s = new Schema(this.DBSchema, this._provider);
            s._include = this._include;
            s.Name = this.Name;

            s._tables = new Lazy<IEnumerable<Table>>(() => (from t in this._tables.Value
                                                            select t.Clone()).ToList());
            if (!_include.HasValue || _include.Value == true)
            {
                var list = s._tables.Value; //Force loading the lazy value if it is to be included.
            }
            return s;
        }
    }
}