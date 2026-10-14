using System;
using System.Collections.Generic;

namespace Prison.Core
{
    public interface IRepository<T> where T : Prisoner
    {
        void Add(T item);
        void Remove(T item);
        IEnumerable<T> GetAll();
        T? Find(Func<T, bool> predicate);

        event EventHandler<InmateEventArgs>? ItemAdded;
        event EventHandler<InmateEventArgs>? ItemRemoved;
        event EventHandler<InmateEventArgs>? ItemWentCrazy;

        void TriggerCrazyAction(T item);
    }
}
