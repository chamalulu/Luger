using System;
using System.Numerics;

using Luger.Functional;

namespace Luger.Utilities
{
    public static class IntExt
    {
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
            var acc2 = unchecked(acc1 + xhyl); // Can overflow
            var carry = (acc1 ^ ((acc1 ^ xhyl) & (xhyl ^ acc2))) >> 31 & ~LoMask;
            return (acc2 >> 32) + carry + xh * yh;  // Can not overflow
        }

        [Obsolete("Any sane person would use System.Numerics.BitOperations.RotateLeft instead.")]
        public static ulong RotateLeft(ulong value, UInt6 n) => value << n | value >> 64 - n;

        [Obsolete("Any sane person would use System.Numerics.BitOperations.RotateRight instead.")]
        public static ulong RotateRight(ulong value, UInt6 n) => value >> n | value << 64 - n;

        public static ulong CopyBits(ulong target, ulong source, int offset, int width)
        {
            var mask = BitOperations.RotateLeft((1ul << width) - 1, offset);

            return width switch
            {
                0 => target,
                64 => source,
                > 64 => throw new ArgumentOutOfRangeException(nameof(width)),
                _ => target & ~mask | source & mask,
            };
        }

        public static ulong CopyBits(ulong target, ulong source, int targetOffset, int sourceOffset, int width)

            => targetOffset.CompareTo(sourceOffset) switch
            {
                < 0 => CopyBits(target, BitOperations.RotateRight(source, sourceOffset - targetOffset), targetOffset, width),
                > 0 => CopyBits(target, BitOperations.RotateLeft(source, targetOffset - sourceOffset), targetOffset, width),
                _ => CopyBits(target, source, targetOffset, width)
            };

        public static uint Gcd(uint a, uint b) => b == 0 ? a : Gcd(b, a % b);

        public static ulong Gcd(ulong a, ulong b) => b == 0 ? a : Gcd(b, a % b);

        public static uint Abs(int n)
        {
            var mask = n >> ((sizeof(int) << 3) - 1);
            return unchecked((uint)((n + mask) ^ mask));
        }

        public static ulong Abs(long n)
        {
            var mask = n >> ((sizeof(long) << 3) - 1);
            return unchecked((ulong)((n + mask) ^ mask));
        }

        public static Maybe<int> ParseMaybe(string s)

            => int.TryParse(s, out var result)
                ? Maybe.Some(result)
                : Maybe.None<int>();
    }
}
