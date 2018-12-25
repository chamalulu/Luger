using System;
using System.Collections.Generic;
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
            => new Transition<IRNGState, IEnumerable<byte>>(state => (state.NextBytes(count), state));

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
               select value.AsInt64();

        // Set RangeUInt64 to IEEE 754 binary64 representation of exactly 2^64. (exponent + 1023) << 52 | (mantissa - 1) * 2^52
        private static readonly double RangeUInt64 = BitConverter.Int64BitsToDouble(0x43F0_0000_0000_0000);

        /// <summary>
        /// Return next random double in range [0..1]
        /// </summary>
        /// <remarks>
        /// Because IEEE 754 cast of 2^64-1 is rounding to 2^64 the greatest value is equal to 1
        /// </remarks>
        public static Transition<IRNGState, double> NextDouble()
            => from value in NextUInt64()
               select value / RangeUInt64;

        /// <summary>
        /// Return next random double in range [0..1)
        /// </summary>
        /// <remarks>
        /// Because IEEE 754 subtraction of greatest IEEE 754 number &lt; 2 by 1 is shifting a 
        /// clear bit into the mantissa the greatest value is 0.999999999999999777955395074969
        /// which is not the greatest IEEE 754 number &lt; 1 (0.999999999999999888977697537484)
        /// </remarks>
        public static Transition<IRNGState, double> NextDoubleBC()
            => from value in NextUInt64()
               let bits = (long)(value >> 12) | 0x3FF0_0000_0000_0000
               select BitConverter.Int64BitsToDouble(bits) - 1;
    }

    public class RandomRNGState : IRNGState
    {
        private readonly Random _random;
        private readonly byte[] _buffer;
        private int _freshBytes;

        public RandomRNGState(Random random = null, uint bufferLength = 0x1000)
        {
            _random = random ?? new Random();

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

        public IEnumerable<byte> NextBytes(uint count)
        {
            for (uint c = 0; c < count; c++)
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

        public IEnumerable<byte> NextBytes(uint count)
        {
            IEnumerable<byte> GetBytes(IEnumerable<ulong> qwords) => qwords.Bind(BitConverter.GetBytes);

            var ulongCount = (count - 1) / sizeof(ulong) + 1;

            var bytesGenerator = EnumerableExt.RangeUInt32(ulongCount)
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

        // Don't run this just around midnight, January 1, 0001 :)
        public static XorShift64StarRNGState FromClock()
            => new XorShift64StarRNGState(DateTime.Now.Ticks.AsUInt64());
    }
}
