using System;
using System.Diagnostics.CodeAnalysis;

namespace Luger.Utilities
{
    public struct UInt6 : IEquatable<UInt6>, IComparable<UInt6>
    {
        readonly int _value;

        private UInt6(int value)
            => _value =
                ((value & ~0x3F) == 0)
                    ? value
                    : (byte)(value & 0x3F | 0x100);   // Preserve low bits and force OverflowException if checked context is enabled.

        public static implicit operator int(UInt6 value) => value._value;

        public int ToInt32() => this;

        public static explicit operator UInt6(int value) => new UInt6(value);

        public static UInt6 FromInt32(int value) => new UInt6(value);

        public static UInt6 operator +(UInt6 addend1, UInt6 addend2) =>
            new UInt6(addend1._value + addend2._value);

        public static UInt6 Add(UInt6 left, UInt6 right) => left + right;

        public static UInt6 operator -(UInt6 minuend, UInt6 subtrahend) =>
            new UInt6(minuend._value - subtrahend._value);

        public static UInt6 Subtract(UInt6 left, UInt6 right) => left - right;

        public bool Equals(UInt6 other) => _value.Equals(other._value);

        public override bool Equals(object? obj) => obj is UInt6 other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(UInt6 other) => _value.CompareTo(other._value);

        public static bool operator ==(UInt6 left, UInt6 right) => left._value == right._value;

        public static bool operator !=(UInt6 left, UInt6 right) => left._value != right._value;

        public static bool operator <(UInt6 left, UInt6 right) => left._value < right._value;

        public static bool operator <=(UInt6 left, UInt6 right) => left._value <= right._value;

        public static bool operator >(UInt6 left, UInt6 right) => left._value > right._value;

        public static bool operator >=(UInt6 left, UInt6 right) => left._value >= right._value;
    }
}
