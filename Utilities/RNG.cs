using System;
using System.Collections.Generic;
using System.Linq;
using Luger.Functional;

namespace Luger.Utilities
{
    public static class IntExt
    {
        private const ulong SignMask = 0x8000_0000_0000_0000;

        public static long AsInt64(this ulong value)
            => (long) (value & ~SignMask) | (value >= SignMask ? long.MinValue : 0);
        
        public static ulong AsUInt64(this long value)
            => (ulong) (value & long.MaxValue) | (value < 0 ? SignMask : 0);

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
    }

    public struct RNGState
    {
        public readonly ulong Seed;
        public readonly ulong Buffer;
        public readonly int FreshBits;

        public RNGState(ulong seed, ulong buffer = 0, int freshBits = 0)
        {
            Seed = seed;
            Buffer = buffer;
            FreshBits = freshBits;
        }

        public static RNGState FromClock() => new RNGState(DateTime.Now.Ticks.AsUInt64());
    }

    public static class RNG
    {
        // PRNG functions reimplemented from https://en.wikipedia.org/wiki/Xorshift
        public static Transition<ulong, ulong> xorshift64star
        => s =>
        {
            s ^= s >> 12;
            s ^= s << 25;
            s ^= s >> 27;

            return (s * 0x2545_F491_4F6C_DD1D, s);
        };

        // public static Transition<(int pos, ulong[] seed), ulong> xorshift1024star
        // => s =>
        // {
        //     var s0 = s.seed[s.pos++];
        //     var s1 = s.seed[s.pos &= 0xF];

        //     s1 ^= s1 << 31;
        //     s1 ^= s1 >> 11;
        //     s1 ^= s0 ^ (s0 >> 30);
        //     s.seed[s.pos] = s1;

        //     return (s1 * 0x1066_89d4_5497_fdb5, s);
        // };

        private static ulong CopyBits(ulong target, ulong source, int offset, int width)
        {
            var mask = ((1ul << width) - 1) << offset;
            
            return target & ~mask | (source & mask);
        }

        private static ulong CopyBits(ulong target, ulong source, int target_offset, int source_offset, int width)
        {
            if (target_offset < source_offset)
                return CopyBits(target, source >> (source_offset - target_offset), target_offset, width);
            else if (target_offset > source_offset)
                return CopyBits(target, source << (target_offset - source_offset), target_offset, width);
            else
                return CopyBits(target, source, target_offset, width);
        }

        public static Transition<RNGState, ulong> NextNBits(int n)
        {
            if (n < 1 || n > 64)
                throw new ArgumentOutOfRangeException(nameof(n));
            
            return state =>
            {
                var next = 0ul;

                if (n > state.FreshBits)
                {
                    next = CopyBits(
                        target: next,
                        source: state.Buffer,
                        target_offset: n - state.FreshBits,
                        source_offset: 0,
                        width: state.FreshBits
                    );
                    
                    n -= state.FreshBits;
                    
                    var (buffer, seed) = xorshift64star(state.Seed);
                    state = new RNGState(seed, buffer, 64);
                }

                return (
                    CopyBits(
                        target: next,
                        source: state.Buffer,
                        target_offset: 0,
                        source_offset: state.FreshBits - n,
                        width: n
                    ),
                    new RNGState(state.Seed, state.Buffer, state.FreshBits - n)
                );
            };
        }

        public static Transition<RNGState, ulong> Next64Bits()
            => state =>
            {
                var (value, seed) = xorshift64star(state.Seed);
                return (value, new RNGState(seed, state.Buffer, state.FreshBits));
            };

        public static Transition<RNGState, ulong> NextUInt64() => NextNBits(64);


        public static Transition<RNGState, ulong> NextUInt64(ulong maxValue)
        {
            if (maxValue == 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            
            return
                from value in NextUInt64()
                select IntExt.Mul64Hi(value, maxValue);
        }
        
        public static Transition<RNGState, ulong> NextUInt64(ulong minValue, ulong maxValue)
        {
            if (maxValue <= minValue)
                throw new ArgumentException($"{nameof(maxValue)} <= {nameof(minValue)}");
            
            return
                from value in NextUInt64(maxValue - minValue)
                select value + minValue;
        }

        public static Transition<RNGState, long> NextInt64()
            => from value in NextUInt64()
                select value.AsInt64();
        
        private const double MaxUInt64 = (double) ulong.MaxValue;

        public static Transition<RNGState, double> NextDouble()
            => from value in NextUInt64()
                select value / MaxUInt64;
        
        public static Transition<RNGState, byte> NextByte()
            => from value in NextNBits(8)
                select (byte) value;

        public static Transition<RNGState, IEnumerable<byte>> NextBytes(int count)
            => Enumerable.Range(0, count).TraverseM(_ => NextByte());
    }
}