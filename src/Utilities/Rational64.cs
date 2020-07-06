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
        public static readonly Rational64 Zero = new Rational64(0, 1);

        /// <summary>
        /// Represents the smallest positive Rational64 value that is greater than zero.
        /// </summary>
        public static readonly Rational64 Epsilon = new Rational64(1, uint.MaxValue);

        /// <summary>
        /// Represents the largest possible value of Rational64.
        /// </summary>
        public static readonly Rational64 MaxValue = new Rational64(int.MaxValue, 1);

        /// <summary>
        /// Represents the smallest possible value of Rational64.
        /// </summary>
        public static readonly Rational64 MinValue = new Rational64(int.MinValue, 1);

        /// <summary>
        /// Represents a value that is not a number (NaN).
        /// This is the default value of Rational64.
        /// </summary>
        public static readonly Rational64 NaN = new Rational64(0, 0);

        /// <summary>
        /// Represents positive infinity.
        /// </summary>
        public static readonly Rational64 PositiveInfinity = new Rational64(1, 0);

        /// <summary>
        /// Represents negative infinity.
        /// </summary>
        public static readonly Rational64 NegativeInfinity = new Rational64(-1, 0);

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
                throw new DivideByZeroException();

            // Check corner case where (int)gcd becomes int.MinValue
            if (numerator == int.MinValue && denominator == 0x8000_0000u)
                return new Rational64(-1, 1);

            uint gcd = IntExt.Gcd(IntExt.Abs(numerator), denominator);

            return new Rational64(numerator / (int)gcd, denominator / gcd);
        }

        // Delegate non-normal comparison and representation to System.Double

        public int CompareTo(Rational64 other) =>
            IsNormal(this) && IsNormal(other)
                ? (_numerator * other._denominator).CompareTo(other._numerator * _denominator)
                : ((double)this).CompareTo((double)other);

        public bool Equals(Rational64 other) =>
            IsNormal(this) && IsNormal(other)
                ? (_numerator * other._denominator).Equals(other._numerator * _denominator)
                : ((double)this).Equals((double)other);

        public override bool Equals(object obj) => obj is Rational64 r && Equals(r);

        public override int GetHashCode() =>
            HashCode.Combine(_numerator, _denominator);

        public string ToString(string format, IFormatProvider provider)
        {
            if (IsNormal(this))
            {
                string nRepr = _numerator.ToString(format, provider);
                string dRepr = _denominator.ToString(format, provider);
                return $"{nRepr}/{dRepr}";
            }
            else
                return ((double)this).ToString(format, provider);
        }

        public string ToString(IFormatProvider provider) => ToString(null, provider);

        public string ToString(string format) => ToString(format, null);

        public override string ToString() => ToString(null, null);

        public static bool IsInfinity(Rational64 value) =>
            value._denominator == 0 && value._numerator != 0;

        public static bool IsNaN(Rational64 value) =>
            value._denominator == 0 && value._numerator == 0;

        public static bool IsNegativeInfinity(Rational64 value) =>
            value._denominator == 0 && value._numerator < 0;

        public static bool IsNormal(Rational64 value) =>
            value._denominator != 0;

        public static bool IsPositiveInfinity(Rational64 value) =>
            value._denominator == 0 && value._numerator > 0;

        public static implicit operator Rational64(int value) =>
            new Rational64(value, 1);

        public static explicit operator int(Rational64 value) =>
            IsNormal(value)
                ? (int)(value._numerator / value._denominator)
                : throw new InvalidCastException();

        public static explicit operator Rational64(double value)
        {
            if (double.IsNaN(value))
                return NaN;

            if (double.IsNegativeInfinity(value))
                return NegativeInfinity;

            if (double.IsPositiveInfinity(value))
                return PositiveInfinity;

            if (value < int.MinValue / 2 || value > int.MaxValue / 2)
                // May throw overflow exception in checked environment
                return new Rational64((int)value, 1);

            /* Since a normal (IEEE 754) double is always a (dyadic) rational,
             *  I was initially tempted to just bit-massage it into a Rational64
             *  but that would nearly never be a good approximation.
             * By producing the fraction through evaluating the continued
             *  fraction of the "real" value I should get a much better
             *  rational approximation of the "real" value.
             */

            double t = Math.Floor(value);

            var (a, b, c, d) = ((int)t, 1, 1u, 0u);

            while (value != t) checked
            {
                value = 1d / (value - t);
                t = Math.Floor(value);

                try
                {
                    (a, b, c, d) =
                        (a * (int)t + b, a, c * (uint)t + d, c);
                }
                catch (OverflowException)
                {
                    break;
                }
            }

            return Create(a, c);
        }

        public static explicit operator double(Rational64 value)
        {
            if (IsNormal(value))
                return (double)value._numerator / (double)value._denominator;

            if (IsNegativeInfinity(value))
                return double.NegativeInfinity;

            if (IsPositiveInfinity(value))
                return double.PositiveInfinity;

            return double.NaN;
        }

        public static Rational64 operator +(Rational64 value) => value;

        public static Rational64 operator -(Rational64 value) => new Rational64(-value._numerator, value._denominator);

        public static Rational64 operator !(Rational64 value)
        {
            if (IsNormal(value))
            {
                var cmp = value.CompareTo(Zero);

                if (cmp < 0)
                    return new Rational64(-(int)value._denominator, IntExt.Abs(value._numerator));

                if (cmp > 0)
                    return new Rational64((int)value._denominator, IntExt.Abs(value._numerator));

                return PositiveInfinity;
            }

            if (IsInfinity(value))
                return Zero;

            return NaN;
        }

        public static Rational64 operator +(Rational64 x, Rational64 y)
        {
            if (IsNormal(x) && IsNormal(y))
            {
                uint gcd = IntExt.Gcd(x._denominator, y._denominator);

                long numerator = x._numerator * (y._denominator / gcd) + y._numerator * (x._denominator / gcd);
                uint denominator = x._denominator * (y._denominator / gcd);

                return new Rational64((int)numerator, denominator);
            }

            return (Rational64)((double)x + (double)y);
        }

        public static Rational64 operator -(Rational64 x, Rational64 y)
        {
            if (IsNormal(x) && IsNormal(y))
            {
                uint gcd = IntExt.Gcd(x._denominator, y._denominator);

                long numerator = x._numerator * (y._denominator / gcd) - y._numerator * (x._denominator / gcd);
                uint denominator = x._denominator * (y._denominator / gcd);

                return new Rational64((int)numerator, denominator);
            }

            return (Rational64)((double)x - (double)y);
        }

        public static Rational64 operator *(Rational64 x, Rational64 y)
        {
            if (IsNormal(x) && IsNormal(y))
            {
                long numerator = (long)x._numerator * (long)y._numerator;
                ulong denominator = (ulong)x._denominator * (ulong)y._denominator;

                ulong gcd = IntExt.Gcd(IntExt.Abs(numerator), denominator);

                return new Rational64((int)(numerator / (long)gcd), (uint)(denominator / gcd));
            }

            return (Rational64)((double)x * (double)y);
        }

        public static Rational64 operator /(Rational64 x, Rational64 y)
        {
            if (IsNormal(x) && IsNormal(y))
            {
                long numerator = x._numerator * y._denominator;
                long signed_denominator = x._denominator * y._numerator;
                ulong denominator;

                if (signed_denominator < 0)
                {
                    numerator = -numerator;
                    denominator = (ulong)(-signed_denominator);
                }
                else
                    denominator = (ulong)signed_denominator;

                ulong gcd = IntExt.Gcd(IntExt.Abs(numerator), denominator);

                return new Rational64((int)(numerator / (long)gcd), (uint)(denominator / gcd));
            }

            return (Rational64)((double)x / (double)y);
        }
    }
}
