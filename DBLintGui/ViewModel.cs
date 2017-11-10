using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using DBLint.DataAccess;
using DBLint.RuleControl;

namespace DBLint.DBLintGui
{
    public class ViewModel : NotifyerClass
    {
        private static bool GenericEquals(object a, object b)
        {
            if (ReferenceEquals(b, a))
                return true;

            bool isEitherNull = (b == null || a == null);
            if (isEitherNull || (!b.Equals(a)))
                return false;

            return true;
        }

        private Connection _currentConnection;
        public Connection CurrentConnection
        {

            get { return _currentConnection; }
            set
            {
                if (GenericEquals(value, _currentConnection))
                    return;

                _currentConnection = value;
                Notify("CurrentConnection");
            }
        }

        private MetadataLight _metadataSelection;
        public MetadataLight MetadataSelection
        {
            get { return _metadataSelection; }
            set
            {
                if (GenericEquals(value, _metadataSelection))
                    return;

                _metadataSelection = value;
                this.Notify("MetadataSelection");
            }
        }
        public RulesConfiguration RulesConfiguration;
        public ViewModel Clone()
        {
            var vm = new ViewModel();
            vm._currentConnection = this._currentConnection;
            vm._metadataSelection = this.MetadataSelection.Clone();
            vm.RulesConfiguration = RulesConfiguration.Clone();
            return vm;
        }
    }
}
