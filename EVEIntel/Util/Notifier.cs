using System.ComponentModel;

namespace EVEIntel.Util
{
    public abstract class Notifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string PropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                                new PropertyChangedEventArgs(PropertyName));
            }
        }
    }
}