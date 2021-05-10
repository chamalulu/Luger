using System;

namespace Luger.Configuration.CommandLine.Specifications
{
    public readonly struct ShortFlagName : IEquatable<ShortFlagName>
    {
        public static ShortFlagName Null => default;

        private readonly char _value;

        public ShortFlagName(char value)

            => _value = char.IsLetterOrDigit(value)
                ? value
                : throw new ArgumentException(null, nameof(value));

        public static implicit operator char(ShortFlagName shortFlagName) => shortFlagName._value;

        public bool Equals(ShortFlagName other) => _value.Equals(other._value);

        public override bool Equals(object? obj) => obj is ShortFlagName other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(ShortFlagName left, ShortFlagName right) => left.Equals(right);

        public static bool operator !=(ShortFlagName left, ShortFlagName right) => !left.Equals(right);

        public override string ToString() => new(new[] { '\'', _value, '\'' });
    }
}
