using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace DBLint.DBLintGui
{
    public class ValidStateUserControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _validState;
        public virtual bool ValidState
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
                InvokeValidStateChanged(null);
            }
        }

        public delegate void ValidStateChangedDeletegate(object sender, EventArgs e);

        public event ValidStateChangedDeletegate ValidStateChanged;

        public void InvokeValidStateChanged(EventArgs e)
        {
            ValidStateChangedDeletegate handler = ValidStateChanged;
            if (handler != null) handler(this, e);
        }
    }
}