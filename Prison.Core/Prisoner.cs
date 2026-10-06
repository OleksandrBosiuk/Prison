using System;
using System.ComponentModel;

namespace Prison.Core
{
    public abstract class Prisoner : INotifyPropertyChanged
    {
        public string Name { get; }

        private int _energy;
        public int Energy
        {
            get { return _energy; }
            protected set
            {
                _energy = Math.Max(0, Math.Min(100, value));
                OnPropertyChanged(nameof(Energy));
            }
        }

        public Prisoner(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(Resources.ErrorEmptyName);
            }
            Name = name;
            Energy = 100;
        }

        public virtual string NormalAction()
        {
            Energy -= 5;
            return string.Format(Resources.NormalAction, Name, Energy);
        }

        public virtual string Rest()
        {
            Energy += 40;
            return string.Format(Resources.RestAction, Name, Energy);
        }

        public abstract string CrazyAction();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}