using System;
using System.Windows.Media;

namespace DBLint.DBLintGui
{
    public class Table : NotifyerClass
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                Notify("Name");
            }
        }

        private bool _include = false;
        public bool Include
        {
            get { return _include; }
            set
            {
                _include = value;
                Notify("Include");
            }
        }

        public DataAccess.DBObjects.Table DatabaseTable { get; private set; }

        private SolidColorBrush _color = Brushes.Black;
        public SolidColorBrush Color
        {
            get { return _color; }
            set
            {
                if (_color != value)
                {
                    _color = value;
                    Notify("Color");
                }
            }
        }

        public Table(DataAccess.DBObjects.Table tbl)
        {
            this.Name = tbl.TableName;
            this.DatabaseTable = tbl;
        }

        public Table Clone()
        {
            var t = new Table(this.DatabaseTable);
            t.Include = this.Include;
            t._name = this._name;
            return t;
        }
    }
}