using System;

namespace Prison.Core
{
    public record BadgeCode
    {
        public string Value { get; init; }

        public BadgeCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Badge code cannot be empty.", nameof(value));

            Value = value.Trim().ToUpperInvariant();
        }

        public static BadgeCode FromGuid(Guid id) =>
            new($"BLK-{id.ToString("N")[..6].ToUpperInvariant()}");

        public override string ToString() => Value;
    }
}
