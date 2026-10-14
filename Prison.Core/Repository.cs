using System;
using System.Collections.Generic;
using System.Linq;

namespace Prison.Core
{
    public class Repository<T> : IRepository<T> where T : Prisoner
    {
        private readonly List<T> _items = new();

        public event EventHandler<InmateEventArgs>? ItemAdded;
        public event EventHandler<InmateEventArgs>? ItemRemoved;
        public event EventHandler<InmateEventArgs>? ItemWentCrazy;

        public void Add(T item)
        {
            if (_items.Contains(item)) return;

            ItemAdded?.Invoke(this, new InmateEventArgs(item, $"{item.Name} entered the block."));

            ItemAdded += item.HandleItemAdded;
            ItemRemoved += item.HandleItemRemoved;
            ItemWentCrazy += item.HandleItemWentCrazy;

            _items.Add(item);
        }

        public void Remove(T item)
        {
            if (!_items.Contains(item)) return;

            ItemAdded -= item.HandleItemAdded;
            ItemRemoved -= item.HandleItemRemoved;
            ItemWentCrazy -= item.HandleItemWentCrazy;

            _items.Remove(item);

            ItemRemoved?.Invoke(this, new InmateEventArgs(item, $"{item.Name} was moved out of the block."));
        }

        public void TriggerCrazyAction(T item)
        {
            if (!_items.Contains(item)) return;

            string crazyMsg = item.CrazyAction();
            ItemWentCrazy?.Invoke(this, new InmateEventArgs(item, crazyMsg));
        }

        public IEnumerable<T> GetAll() => _items.ToList();

        public T? Find(Func<T, bool> predicate) => _items.FirstOrDefault(predicate);
    }

    internal static class PrisonerEventHandlerExtensions
    {
        public static void HandleItemAdded(this Prisoner self, object? sender, InmateEventArgs e)
        {
            if (e.Actor != self) self.OnNeighborJoined(e.Actor);
        }

        public static void HandleItemRemoved(this Prisoner self, object? sender, InmateEventArgs e)
        {
            if (e.Actor != self) self.OnNeighborLeft(e.Actor);
        }

        public static void HandleItemWentCrazy(this Prisoner self, object? sender, InmateEventArgs e)
        {
            if (e.Actor != self) self.OnNeighborWentCrazy(e.Actor);
        }
    }
}