using System;
using System.Collections.Generic;
using System.Linq;
using Luger.Functional;
using PRNG = Luger.Functional.Transition<ulong, ulong>;

namespace Luger.Utilities
{
    public static class IntExt
    {
        private const ulong SignMask = 0x8000_0000_0000_0000;

        public static long AsInt64(this ulong value)
            => (long)(value & ~SignMask) | (value >= SignMask ? long.MinValue : 0);

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
    }

    public static class OldRNG
    {
        private static ulong CopyBits(ulong target, ulong source, int offset, int width)
        {
            var mask = ((1ul << width) - 1) << offset;

            return target & ~mask | source & mask;
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
    }

    public interface IRNGState
    {
        ulong NextUInt64();
        IEnumerable<byte> NextBytes(int count);
    }

    public static class RNG
    {
        /// <summary>
        /// Return next random ulong
        /// </summary>
        public static Transition<IRNGState, ulong> NextUInt64() => state => (state.NextUInt64(), state);

        /// <summary>
        /// Return next random UInt64 in range [0 .. 2^n)
        /// </summary>
        /// <param name="n">
        /// Number of significant bits in result.
        /// </param>
        public static Transition<IRNGState, ulong> NextNBits(int n)
            => (n - 1 & ~0x3F) == 0
                ? from value in NextUInt64() select value & (1ul << n) - 1
                : throw new ArgumentOutOfRangeException(nameof(n));

        /// <summary>
        /// Return next count random bytes
        /// </summary>
        public static Transition<IRNGState, IEnumerable<byte>> NextBytes(int count)
            => count >= 0
                ? new Transition<IRNGState, IEnumerable<byte>>(state => (state.NextBytes(count), state))
                : throw new ArgumentOutOfRangeException(nameof(count));

        /// <summary>
        /// Return next random ulong in range [0 .. maxValue)
        /// </summary>
        public static Transition<IRNGState, ulong> NextUInt64(ulong maxValue)
            => maxValue > 0
                ? from value in NextUInt64() select IntExt.Mul64Hi(value, maxValue)
                : throw new ArgumentOutOfRangeException(nameof(maxValue));

        /// <summary>
        /// Return next random ulong in range [minValue .. maxValue)
        /// </summary>
        public static Transition<IRNGState, ulong> NextUInt64(ulong minValue, ulong maxValue)
            => maxValue > minValue
                ? from value in NextUInt64(maxValue - minValue) select value + minValue
                : throw new ArgumentException($"{nameof(maxValue)} <= {nameof(minValue)}");

        /// <summary>
        /// Return next random long
        /// </summary>
        public static Transition<IRNGState, long> NextInt64()
            => from value in NextUInt64() select value.AsInt64();

        private const double MaxUInt64 = (double)ulong.MaxValue;

        /// <summary>
        /// Return next random double in range [0.0 .. 1.0)
        /// </summary>
        public static Transition<IRNGState, double> NextDouble()
            => from value in NextUInt64() select value / MaxUInt64;
    }

    public class RandomRNGState : IRNGState
    {
        private readonly Random _random;
        private readonly byte[] _buffer;
        private int _freshBytes;

        public RandomRNGState(Random random = null, int bufferLength = 0x1000)
        {
            _random = random ?? new Random();

            if (bufferLength < sizeof(ulong))
                throw new ArgumentOutOfRangeException(nameof(bufferLength));

            _buffer = new byte[bufferLength];
        }

        private void FillBuffer()
        {
            _random.NextBytes(_buffer);
            _freshBytes = _buffer.Length;
        }

        public ulong NextUInt64()
        {
            if (_freshBytes < sizeof(ulong))
                FillBuffer();

            var startIndex = _buffer.Length - _freshBytes;

            _freshBytes -= sizeof(ulong);

            return BitConverter.ToUInt64(_buffer, startIndex);
        }

        public IEnumerable<byte> NextBytes(int count)
        {
            for (int c = 0; c < count; c++)
            {
                if (_freshBytes == 0)
                    FillBuffer();

                yield return _buffer[_buffer.Length - _freshBytes--];
            }
        }

        public static implicit operator RandomRNGState(Random random) => new RandomRNGState(random);
    }

    public class UInt64TransitionRNGState : IRNGState
    {
        private readonly Transition<ulong, ulong> _prng;
        private ulong _seed;

        public UInt64TransitionRNGState(ulong seed, Transition<ulong, ulong> prng)
        {
            _prng = prng ?? throw new ArgumentNullException(nameof(prng));
            _seed = seed;
        }

        public ulong NextUInt64()
        {
            var (value, seed) = _prng(_seed);

            _seed = seed;
            
            return value;
        }

        public IEnumerable<byte> NextBytes(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            IEnumerable<byte> GetBytes(IEnumerable<ulong> qwords) => qwords.Bind(BitConverter.GetBytes);

            var (bytes, seed) = Enumerable.Range(0, (count - 1) / sizeof(ulong) + 1).TraverseM(_ => _prng).Map(GetBytes)(_seed);

            _seed = seed;

            return bytes.Take(count);
        }
    }

    public class XorShift64StarRNGState : UInt64TransitionRNGState
    {
        // PRNG function reimplemented from https://en.wikipedia.org/wiki/Xorshift
        private static Transition<ulong, ulong> xorshift64star
        => s =>
        {
            s ^= s >> 12;
            s ^= s << 25;
            s ^= s >> 27;

            return (s * 0x2545_F491_4F6C_DD1D, s);
        };

        public XorShift64StarRNGState(ulong seed) : base(seed, xorshift64star)
        {
            if (seed == 0)
                throw new ArgumentOutOfRangeException(nameof(seed), "Seed must not be 0.");
        }

        // Don't run this on exactly midnight, January 1, 0001 :)
        public static XorShift64StarRNGState FromClock() => new XorShift64StarRNGState(DateTime.Now.Ticks.AsUInt64());
    }
}