using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Luger.Functional;

namespace Luger.Utilities
{
    public interface IRNGState
    {
        ulong NextUInt64();
        IEnumerable<byte> NextBytes(uint count);
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
        /// Number of significant bits in result. [1 .. 64]
        /// </param>
        public static Transition<IRNGState, ulong> NextNBits(int n)

            => (n - 1 & ~0x3F) == 0
                ? from value in NextUInt64()
                  select value >> 64 - n
                : throw new ArgumentOutOfRangeException(nameof(n));

        /// <summary>
        /// Return next count random bytes
        /// </summary>
        public static Transition<IRNGState, IEnumerable<byte>> NextBytes(uint count)

            => new(state => (state.NextBytes(count), state));

        /// <summary>
        /// Return next random ulong in range [0 .. maxValue)
        /// </summary>
        public static Transition<IRNGState, ulong> NextUInt64(ulong maxValue)

            => maxValue > 0
                ? from value in NextUInt64()
                  select IntExt.Mul64Hi(value, maxValue)
                : throw new ArgumentOutOfRangeException(nameof(maxValue));

        /// <summary>
        /// Return next random ulong in range [minValue .. maxValue)
        /// </summary>
        public static Transition<IRNGState, ulong> NextUInt64(ulong minValue, ulong maxValue)

            => maxValue > minValue
                ? from value in NextUInt64(maxValue - minValue)
                  select value + minValue
                : throw new ArgumentException($"{nameof(maxValue)} <= {nameof(minValue)}");

        /// <summary>
        /// Return next random long
        /// </summary>
        public static Transition<IRNGState, long> NextInt64()

            => from value in NextUInt64()
               select unchecked((long)value);

        // Set RangeUInt53 to IEEE 754 binary64 representation of exactly 2^53. (exponent + 1023) << 52 | (mantissa - 1) * 2^52
        private static readonly double RangeUInt53 = BitConverter.Int64BitsToDouble(0x4340_0000_0000_0000);

        /// <summary>
        /// Return next random double in range [0 .. 1)
        /// </summary>
        /// <remarks>
        /// This transition linearly scales the random integer value in range [0 .. 2^53) to an IEEE 754 binary64 value in range [0 .. 1)
        /// Linear scaling preserves uniform distribution but should otherwise be of no importance since these are random values.
        /// </remarks>
        public static Transition<IRNGState, double> NextDouble()

            => from value in NextUInt64(0x20_0000_0000_0000)
               select value / RangeUInt53;


        /// <summary>
        /// Return next random double in range [0 .. 1)
        /// </summary>
        /// <remarks>
        /// This transition translates the magnitude of the random integer value in range [0 .. 2^64) to an IEEE 754 binary64 exponent (biased 11-bit) and fills the mantissa with random bits.
        /// Exponent translation should preserve uniform distribution but the result will not be linear with the source since we don't bother shifting into position.
        /// </remarks>
        public static Transition<IRNGState, double> NextDoubleBitOp()

            => NextUInt64().Map(ul =>
            {
                long exponent = ul == 0 ? 0 : BitOperations.Log2(ul) + 959;
                var mantissa = (long)ul & 0xF_FFFF_FFFF_FFFF;

                return BitConverter.Int64BitsToDouble(exponent << 52 | mantissa);
            });
    }

    public class RandomRNGState : IRNGState
    {
        private readonly Random _random;
        private readonly byte[] _buffer;
        private int _freshBytes;

        private const uint DefaultBufferLength = 0x1000;

        public RandomRNGState(Random random, uint bufferLength)
        {
            _random = random;
            _buffer = new byte[bufferLength];
        }

        public RandomRNGState(Random random) : this(random, DefaultBufferLength) { }

        public RandomRNGState(uint bufferLength) : this(new Random(), bufferLength) { }

        public RandomRNGState() : this(new Random(), DefaultBufferLength) { }

        private void FillBuffer()
        {
            _random.NextBytes(_buffer);
            _freshBytes = _buffer.Length;
        }

        public ulong NextUInt64()
        {
            if (_freshBytes < sizeof(ulong))
            {
                FillBuffer();
            }

            var startIndex = _buffer.Length - _freshBytes;

            _freshBytes -= sizeof(ulong);

            return BitConverter.ToUInt64(_buffer, startIndex);
        }

        public IEnumerable<byte> NextBytes(uint count)
        {
            for (uint c = 0; c < count; c++)
            {
                if (_freshBytes == 0)
                {
                    FillBuffer();
                }

                var nextByte = _buffer[^_freshBytes];

                _freshBytes -= 1;

                yield return nextByte;
            }
        }
    }

    public class UInt64TransitionRNGState : IRNGState
    {
        private ulong _seed;
        private readonly Transition<ulong, ulong> _prng;

        public UInt64TransitionRNGState(ulong seed, Transition<ulong, ulong> prng)
        {
            _seed = seed;
            _prng = prng;
        }

        public ulong NextUInt64()
        {
            var (value, seed) = _prng(_seed);

            _seed = seed;

            return value;
        }

        public IEnumerable<byte> NextBytes(uint count)
        {
            if (count == 0)
            {
                return Enumerable.Empty<byte>();
            }

            var qwordCount = (count - 1) / sizeof(ulong) + 1;

            static IEnumerable<byte> GetBytes(IEnumerable<ulong> qwords) => qwords.Bind(BitConverter.GetBytes);

            var bytesGenerator = EnumerableExt.RangeUInt32(qwordCount)
                .TraverseM(_ => _prng)
                .Map(GetBytes);

            var (bytes, seed) = bytesGenerator(_seed);

            _seed = seed;

            return bytes.Take(count);
        }
    }

    public class XorShift64StarRNGState : UInt64TransitionRNGState
    {
        // PRNG function reimplemented from https://en.wikipedia.org/wiki/Xorshift
        private static Transition<ulong, ulong> XorShift64Star

            => s =>
            {
                s ^= s >> 12;
                s ^= s << 25;
                s ^= s >> 27;

                return (s * 0x2545_F491_4F6C_DD1D, s);
            };

        public XorShift64StarRNGState(ulong seed) : base(seed, XorShift64Star)
        {
            if (seed == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seed), $"{nameof(seed)} = 0");
            }
        }

        // Don't run this just around midnight, January 1, 0001 :)
        public static XorShift64StarRNGState FromClock() => new(unchecked((ulong)DateTime.Now.Ticks));
    }
}
