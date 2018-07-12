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

        public static ulong CopyBits(ulong target, ulong source, int offset, int width)
        {
            var mask = ((1ul << width) - 1) << offset;

            return target & ~mask | source & mask;
        }

        public static ulong CopyBits(ulong target, ulong source, int target_offset, int source_offset, int width)
        {
            if (target_offset < source_offset)
                return CopyBits(target, source >> (source_offset - target_offset), target_offset, width);
            else if (target_offset > source_offset)
                return CopyBits(target, source << (target_offset - source_offset), target_offset, width);
            else
                return CopyBits(target, source, target_offset, width);
        }
    }
}