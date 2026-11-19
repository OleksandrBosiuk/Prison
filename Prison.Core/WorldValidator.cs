using System;
using System.Collections.Generic;
using System.IO;

namespace Prison.Core
{
    public static class WorldValidator
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Rookie", "Philosopher", "Artist", "Lawyer", "Athlete", "Mechanic"
        };

        public static void Validate(WorldDto? world)
        {
            if (world == null || world.Blocks == null)
                throw new InvalidDataException("File is corrupted or missing world structure.");

            var seenIds = new HashSet<Guid>();

            foreach (var block in world.Blocks)
            {
                if (string.IsNullOrWhiteSpace(block.BlockName))
                    throw new InvalidDataException("Found a block without a group name.");

                if (block.Inmates == null) continue;

                foreach (var p in block.Inmates)
                {
                    if (p.Id == Guid.Empty)
                        throw new InvalidDataException($"Prisoner '{p.Name}' is missing a valid ID.");

                    if (!seenIds.Add(p.Id))
                        throw new InvalidDataException($"Duplicate prisoner ID: {p.Id}.");

                    if (string.IsNullOrWhiteSpace(p.Name))
                        throw new InvalidDataException("Found a prisoner with an empty name.");

                    if (!AllowedTypes.Contains(p.TypeName))
                        throw new InvalidDataException($"Unknown prisoner type: '{p.TypeName}'.");

                    if (p.Energy < 0 || p.Energy > 100)
                        throw new InvalidDataException($"Invalid energy level ({p.Energy}%) for '{p.Name}'.");
                }
            }
        }
    }
}