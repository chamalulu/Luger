using System;

namespace Luger.Utilities
{
    /// <summary>
    /// Represent rational number with signed 32bit numerator and
    ///  unsigned 32bit denominator.
    /// </summary>
    /// <remarks>
    /// Represent rational number numerator (n) / denominator (d).
    ///  d = 0 and n = 0 => NaN
    ///  d = 0 and n > 0 => +Inf
    ///  d = 0 and n < 0 => -Inf
    ///
    ///  Arithmetics is implemented to produce exact results for operations
    ///   within value space and always keep numerator and denominator co-
    ///   prime.
    ///  Overflows in checked environment should throw OverflowException.
    ///  Overflows in unchecked environment should give undefined results.
    /// </remarks>
    public readonly struct Rational64 : IEquatable<Rational64>, IComparable<Rational64>, IFormattable
    {
        /// <summary>
        /// Represents canonical 0 of Rational64. Internally (0/1u).
        /// </summary>
        public static readonly Rational64 Zero = new(0, 1);

        /// <summary>
        /// Represents the smallest positive Rational64 value that is greater than zero.
        /// </summary>
        public static readonly Rational64 Epsilon = new(1, uint.MaxValue);

        /// <summary>
        /// Represents the largest possible value of Rational64.
        /// </summary>
        public static readonly Rational64 MaxValue = new(int.MaxValue, 1);

        /// <summary>
        /// Represents the smallest possible value of Rational64.
        /// </summary>
        public static readonly Rational64 MinValue = new(int.MinValue, 1);

        /// <summary>
        /// Represents a value that is not a number (NaN).
        /// This is the default value of Rational64.
        /// </summary>
        public static readonly Rational64 NaN = new(0, 0);

        /// <summary>
        /// Represents positive infinity.
        /// </summary>
        public static readonly Rational64 PositiveInfinity = new(1, 0);

        /// <summary>
        /// Represents negative infinity.
        /// </summary>
        public static readonly Rational64 NegativeInfinity = new(-1, 0);

        public readonly int _numerator;
        public readonly uint _denominator;

        private Rational64(int numerator, uint denominator)
        {
            _numerator = numerator;
            _denominator = denominator;
        }

        /// <summary>
        /// Static factory for normal Rational64.
        /// </summary>
        public static Rational64 Create(int numerator, uint denominator)
        {
            if (denominator == 0)
            {
                throw new DivideByZeroException();
            }

            // Check corner case where (int)gcd becomes int.MinValue
            if (numerator == int.MinValue && denominator == 0x8000_0000u)
            {
                return new Rational64(-1, 1);
            }

            var gcd = IntExt.Gcd(IntExt.Abs(numerator), denominator);

            return new Rational64(numerator / (int)gcd, denominator / gcd);
        }

        // Delegate non-normal comparison and representation to System.Double

        public int CompareTo(Rational64 other)

            => IsNormal(this) && IsNormal(other)
                ? (_numerator * other._denominator).CompareTo(other._numerator * _denominator)
                : ((double)this).CompareTo((double)other);

        public bool Equals(Rational64 other)

            => IsNormal(this) && IsNormal(other)
                ? (_numerator * other._denominator).Equals(other._numerator * _denominator)
                : ((double)this).Equals((double)other);

        public override bool Equals(object? obj) => obj is Rational64 r && Equals(r);

        public override int GetHashCode() => HashCode.Combine(_numerator, _denominator);

        /// <remarks>Falls back to <see cref="double.ToString(string?, IFormatProvider?)"/> for non-normal numbers.</remarks>
        /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)"/>
        public string ToString(string? format, IFormatProvider? provider)
        {
            if (IsNormal(this))
            {
                var nRepr = _numerator.ToString(format, provider);
                var dRepr = _denominator.ToString(format, provider);
                return $"{nRepr}/{dRepr}";
            }
            else
            {
                return ((double)this).ToString(format, provider);
            }
        }

        public string ToString(IFormatProvider provider) => ToString(null, provider);

        public string ToString(string format) => ToString(format, null);

        public override string ToString() => ToString(null, null);

        public static bool IsInfinity(Rational64 value) => value._denominator == 0 && value._numerator != 0;

        public static bool IsNaN(Rational64 value) => value._denominator == 0 && value._numerator == 0;

        public static bool IsNegativeInfinity(Rational64 value) => value._denominator == 0 && value._numerator < 0;

        public static bool IsNormal(Rational64 value) => value._denominator != 0;

        public static bool IsPositiveInfinity(Rational64 value) => value._denominator == 0 && value._numerator > 0;

        public static implicit operator Rational64(int value) => new(value, 1);

        public static explicit operator int(Rational64 value)

            => IsNormal(value)
                ? (int)(value._numerator / value._denominator)
                : throw new InvalidCastException();

        public static explicit operator Rational64(double value)
        {
            if (double.IsNaN(value))
            {
                return NaN;
            }

            if (double.IsNegativeInfinity(value))
            {
                return NegativeInfinity;
            }

            if (double.IsPositiveInfinity(value))
            {
                return PositiveInfinity;
            }

            if (value < int.MinValue / 2 || value > int.MaxValue / 2)
            {
                // May throw overflow exception in checked environment
                return new((int)value, 1);
            }

            /* Since a normal (IEEE 754) double is always a (dyadic) rational,
             *  I was initially tempted to just bit-massage it into a Rational64
             *  but that would nearly never be a good approximation.
             * By producing the fraction through evaluating the continued
             *  fraction of the "real" value I should get a much better
             *  rational approximation of the "real" value.
             */

            var t = Math.Floor(value);

            var (a, b, c, d) = ((int)t, 1, 1u, 0u);

            while (value != t)
            {
                checked
                {
                    value = 1d / (value - t);
                    t = Math.Floor(value);

                    try
                    {
                        (a, b, c, d) = (a * (int)t + b, a, c * (uint)t + d, c);
                    }
                    catch (OverflowException)
                    {
                        break;
                    }
                }
            }

            return Create(a, c);
        }

        public static explicit operator double(Rational64 value)
        {
            if (IsNormal(value))
            {
                return (double)value._numerator / value._denominator;
            }

            if (IsNegativeInfinity(value))
            {
                return double.NegativeInfinity;
            }

            if (IsPositiveInfinity(value))
            {
                return double.PositiveInfinity;
            }

            return double.NaN;
        }

        public static Rational64 operator +(Rational64 value) => value;

        public static Rational64 operator -(Rational64 value) => new(-value._numerator, value._denominator);

        public static Rational64 operator !(Rational64 value) => (value._numerator, value._denominator) switch
        {
            // NaN
            (0, 0) => NaN,
            // Infinity
            (_, 0) => Zero,
            // Negative
            ( < 0, _) => new(-(int)value._denominator, IntExt.Abs(value._numerator)),
            // Positive
            ( > 0, _) => new((int)value._denominator, (uint)value._numerator),
            // Zero
            (0, _) => PositiveInfinity
        };

        public static Rational64 operator +(Rational64 x, Rational64 y)
        {
            if (IsNormal(x) && IsNormal(y))
            {
                var gcd = IntExt.Gcd(x._denominator, y._denominator);

                var numerator = x._numerator * (y._denominator / gcd) + y._numerator * (x._denominator / gcd);
                var denominator = x._denominator * (y._denominator / gcd);

                return new((int)numerator, denominator);
            }

            return (Rational64)((double)x + (double)y);
        }

        public static Rational64 operator -(Rational64 x, Rational64 y)
        {
            if (IsNormal(x) && IsNormal(y))
            {
                var gcd = IntExt.Gcd(x._denominator, y._denominator);

                var numerator = x._numerator * (y._denominator / gcd) - y._numerator * (x._denominator / gcd);
                var denominator = x._denominator * (y._denominator / gcd);

                return new((int)numerator, denominator);
            }

            return (Rational64)((double)x - (double)y);
        }

        public static Rational64 operator *(Rational64 x, Rational64 y)
        {
            if (IsNormal(x) && IsNormal(y))
            {
                var numerator = (long)x._numerator * y._numerator;
                var denominator = (ulong)x._denominator * y._denominator;

                var gcd = IntExt.Gcd(IntExt.Abs(numerator), denominator);

                return new((int)(numerator / (long)gcd), (uint)(denominator / gcd));
            }

            return (Rational64)((double)x * (double)y);
        }

        public static Rational64 operator /(Rational64 x, Rational64 y)
        {
            if (IsNormal(x) && IsNormal(y))
            {
                var numerator = x._numerator * y._denominator;
                var signed_denominator = x._denominator * y._numerator;
                ulong denominator;

                if (signed_denominator < 0)
                {
                    numerator = -numerator;
                    denominator = (ulong)-signed_denominator;
                }
                else
                {
                    denominator = (ulong)signed_denominator;
                }

                var gcd = IntExt.Gcd(IntExt.Abs(numerator), denominator);

                return new((int)(numerator / (long)gcd), (uint)(denominator / gcd));
            }

            return (Rational64)((double)x / (double)y);
        }

        public static bool operator ==(Rational64 left, Rational64 right) => left.Equals(right);

        public static bool operator !=(Rational64 left, Rational64 right) => !(left == right);

        public static bool operator <(Rational64 left, Rational64 right) => left.CompareTo(right) < 0;

        public static bool operator <=(Rational64 left, Rational64 right) => left.CompareTo(right) <= 0;

        public static bool operator >(Rational64 left, Rational64 right) => left.CompareTo(right) > 0;

        public static bool operator >=(Rational64 left, Rational64 right) => left.CompareTo(right) >= 0;
    }
}
