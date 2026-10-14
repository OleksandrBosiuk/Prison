using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Prison.Core
{
    public abstract class Prisoner : INotifyPropertyChanged
    {
        private int _energy;
        public string Name { get; set; }

        public int Energy
        {
            get => _energy;
            set
            {
                if (_energy != value)
                {
                    _energy = Math.Clamp(value, 0, 100);
                    OnPropertyChanged();
                }
            }
        }

        public string TypeName => GetType().Name;

        public event Action<string>? ActionLogged;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected Prisoner(string name, int initialEnergy = 100)
        {
            Name = name;
            Energy = initialEnergy;
        }

        public virtual string NormalAction()
        {
            return $"{Name} is staying in the cell and following the daily routine.";
        }

        public virtual string Rest()
        {
            Energy = Math.Min(100, Energy + 20);
            return $"{Name} rested on the bunk. Energy is now {Energy}%.";
        }

        public abstract string CrazyAction();

        public virtual void OnNeighborJoined(Prisoner newcomer)
        {
            Log($"{Name} ({TypeName}) greets newcomer {newcomer.Name}.");
        }

        public virtual void OnNeighborLeft(Prisoner leaving)
        {
            Log($"{Name} ({TypeName}) says goodbye to {leaving.Name}.");
        }

        public virtual void OnNeighborWentCrazy(Prisoner instigator)
        {
            Energy -= 5;
            Log($"{Name} gets stressed by {instigator.Name}'s crazy action! Energy dropped to {Energy}%.");
        }

        protected void Log(string message) => ActionLogged?.Invoke(message);

        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}