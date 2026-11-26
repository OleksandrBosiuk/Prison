using System;
using System.Collections.Generic;
using System.Linq;

namespace Prison.Core
{
    public class PrisonBlockAggregate
    {
        public Guid Id { get; private set; }
        public string BlockName { get; private set; }
        public int Capacity { get; private set; } = 5;

        private readonly List<Prisoner> _inmates = new();
        public IReadOnlyCollection<Prisoner> Inmates => _inmates.AsReadOnly();

        public PrisonBlockAggregate(string blockName, int capacity = 5)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("Block name is required.", nameof(blockName));

            Id = Guid.NewGuid();
            BlockName = blockName;
            Capacity = capacity;
        }

        public void AdmitPrisoner(Prisoner prisoner)
        {
            ArgumentNullException.ThrowIfNull(prisoner);

            if (_inmates.Any(p => p.Id == prisoner.Id))
                throw new InvalidOperationException($"Prisoner with ID {prisoner.Id} is already in {BlockName}.");

            if (_inmates.Count >= Capacity)
                throw new InvalidOperationException($"{BlockName} is full! Maximum capacity is {Capacity} inmates.");

            _inmates.Add(prisoner);
        }

        public void ReleasePrisoner(Guid id)
        {
            var found = _inmates.FirstOrDefault(p => p.Id == id);
            if (found != null)
                _inmates.Remove(found);
        }
    }
}
