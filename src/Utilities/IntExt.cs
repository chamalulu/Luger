using System;

namespace Luger.Utilities
{
    public static class IntExt
    {
        private const ulong SignMask = 0x8000_0000_0000_0000;

        /// <summary>
        /// Bitwise copy from UInt64 to Int64
        /// </summary>
        public static long AsInt64(this ulong value)
            => (long)(value & ~SignMask) | (value >= SignMask ? long.MinValue : 0);

        /// <summary>
        /// Bitwise copy from Int64 to UInt64
        /// </summary>
        public static ulong AsUInt64(this long value)
            => (ulong)(value & long.MaxValue) | (value < 0 ? SignMask : 0);

        private const ulong LoMask = 0x0000_0000_FFFF_FFFF;

        /// <summary>
        /// Calculates high qword of product of two qwords.
        /// </summary>
        /// <remarks>
        /// This method was interesting to implement and
        ///  it is >10 times faster than using BigInteger (on my hardware) but
        ///  readability vs. performance should be taken into account in usages.
        /// </remarks>
        public static ulong Mul64Hi(ulong x, ulong y)
        {
            /*
            x = xl + (xh << 32)
            y = yl + (yh << 32)
            */
            ulong xl = x & LoMask, yl = y & LoMask, xh = x >> 32, yh = y >> 32;

            /*
            x * y >> 64 = (xl + (xh << 32)) * (yl + (yh << 32)) >> 64
                        = xl * yl + xl * (yh << 32) + (xh << 32) * yl + (xh << 32) * (yh << 32) >> 64
            */
            var acc1 = (xl * yl >> 32) + xl * yh;   // Can not overflow
            var xhyl = xh * yl;
            var acc2 = acc1 + xhyl; // Can overflow
            var carry = (acc1 ^ ((acc1 ^ xhyl) & (xhyl ^ acc2))) >> 31 & ~LoMask;
            return (acc2 >> 32) + carry + xh * yh;  // Can not overflow
        }

        public struct UInt6
        {
            readonly int _value;

            private UInt6(int value)
                => _value =
                    ((value & ~0x3F) == 0)
                        ? value
                        : (byte)(value & 0x3F | 0x100);   // Preserve low bits and force OverflowException if checking enabled.

            public static implicit operator int(UInt6 value) => value._value;
            public static explicit operator UInt6(int value) => new UInt6(value);

            public static UInt6 operator +(UInt6 addend1, UInt6 addend2) =>
                new UInt6(addend1._value + addend2._value);

            public static UInt6 operator -(UInt6 minuend, UInt6 subtrahend) =>
                new UInt6(minuend._value - subtrahend._value);
        }

        public static ulong RotateLeft(ulong value, UInt6 n) => value << n | value >> 64 - n;

        public static ulong RotateRight(ulong value, UInt6 n) => value >> n | value << 64 - n;

        public static ulong CopyBits(ulong target, ulong source, UInt6 offset, byte width)
        {
            if (width > 64)
                throw new ArgumentOutOfRangeException(nameof(width));

            switch (width)
            {
                case 0:
                    return target;
                case 64:
                    return source;
                default:
                    var mask = RotateLeft((1ul << width) - 1, offset);
                    return target & ~mask | source & mask;
            }
        }

        public static ulong CopyBits(ulong target, ulong source, UInt6 target_offset, UInt6 source_offset, byte width)
        {
            if (target_offset < source_offset)
                return CopyBits(target, RotateRight(source, source_offset - target_offset), target_offset, width);
            else if (target_offset > source_offset)
                return CopyBits(target, RotateLeft(source, target_offset - source_offset), target_offset, width);
            else
                return CopyBits(target, source, target_offset, width);
        }
    }
}