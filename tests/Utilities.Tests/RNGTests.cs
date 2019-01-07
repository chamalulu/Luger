using System;
using System.Collections.Generic;
using System.Linq;
using Luger.Functional;
using Xunit;

namespace Luger.Utilities.Tests
{

    public class RNGTests
    {
        private const ulong MockUInt64 = 0x123_4567_89AB_CDEF;
        private const byte MockByte = 42;

        private class MockRNGState : IRNGState
        {
            private readonly Func<ulong> _nextUInt64;
            private readonly Func<uint, IEnumerable<byte>> _nextBytes;

            public MockRNGState(Func<ulong> nextUInt64, Func<uint, IEnumerable<byte>> nextBytes)
            {
                _nextUInt64 = nextUInt64 ?? new Func<ulong>(() => MockUInt64);
                _nextBytes = nextBytes ?? new Func<uint, IEnumerable<byte>>(count => EnumerableExt.Repeat(MockByte, count));
            }

            public MockRNGState() : this(null, null) { }

            public MockRNGState(ulong nextUInt64) : this(() => nextUInt64, null) { }

            public MockRNGState(IEnumerable<byte> nextBytes) : this(null, _ => nextBytes) { }

            public MockRNGState(Func<ulong> nextUInt64) : this(nextUInt64, null) { }

            public MockRNGState(Func<uint, IEnumerable<byte>> nextBytes) : this(null, nextBytes) { }

            public ulong NextUInt64() => _nextUInt64();

            public IEnumerable<byte> NextBytes(uint count) => _nextBytes(count);
        }

        [Fact]
        public void NextUInt64_Test()
        {
            var target = RNG.NextUInt64();
            var state = new MockRNGState();
            var (actual, newState) = target(state);

            Assert.Equal(MockUInt64, actual);
            Assert.Equal(state, newState);
        }

        public static IEnumerable<object[]> ValidNextNBitsData
            => from v in System.Linq.Enumerable.Range(1, 64) select new object[] { v };

        [Theory]
        [MemberData(nameof(ValidNextNBitsData))]
        public void NextNBits_Positive_Test(int n)
        {
            var target = RNG.NextNBits(n);
            var state = new MockRNGState();
            var actual = target.Run(state);

            var mask = ~((1ul << n - 1) - 1) << 1;
            Assert.True((actual & mask) == 0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65)]
        public void NextNBits_Negative_Test(int n)
            => Assert.Throws<ArgumentOutOfRangeException>(() => RNG.NextNBits(n));

        [Theory]
        [InlineData(100)]
        public void NextBytes_Positive_Test(uint count)
        {
            var target = RNG.NextBytes(count);
            var state = new MockRNGState();
            var actual = target.Run(state);

            Assert.Equal(count, actual.UInt32Count());
        }

        [Theory]
        [InlineData(ulong.MinValue, 100, 0)]
        [InlineData(ulong.MaxValue, 100, 99)]
        public void NextUInt64_maxValue_Positive_Test(ulong nextUInt64, ulong maxValue, ulong expected)
        {
            var target = RNG.NextUInt64(maxValue);
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0)]
        public void NextUInt64_maxValue_Negative_Test(ulong maxValue)
            => Assert.Throws<ArgumentOutOfRangeException>(() => RNG.NextUInt64(maxValue));

        [Theory]
        [InlineData(ulong.MinValue, 100, 200, 100)]
        [InlineData(ulong.MaxValue, 100, 200, 199)]
        public void NextUInt64_minValue_maxValue_Positive_Test(ulong nextUInt64, ulong minValue, ulong maxValue, ulong expected)
        {
            var target = RNG.NextUInt64(minValue, maxValue);
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(200, 100)]
        public void NextUInt64_minValue_maxValue_Negative_Test(ulong minValue, ulong maxValue)
            => Assert.Throws<ArgumentException>(() => RNG.NextUInt64(minValue, maxValue));

        [Theory]
        [InlineData(ulong.MinValue, 0L)]
        [InlineData(0x7FFF_FFFF_FFFF_FFFFUL, long.MaxValue)]
        [InlineData(0x8000_0000_0000_0000UL, long.MinValue)]
        [InlineData(ulong.MaxValue, -1L)]
        public void NextInt64_Test(ulong nextUInt64, long expected)
        {
            var target = RNG.NextInt64();
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(ulong.MinValue, 0d)]
        [InlineData(ulong.MaxValue, 1d)]
        public void NextDouble_Test(ulong nextUInt64, double expected)
        {
            var target = RNG.NextDouble();
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(ulong.MinValue, 0d)]
        [InlineData(ulong.MaxValue, 0.999999999999999777955395074969d)]
        public void NextDoubleBC_Test(ulong nextUInt64, double expected)
        {
            var target = RNG.NextDoubleBC();
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }
    }

    public class RandomRNGStateTests
    {
        [Theory]
        [InlineData(0, 15628745651041733658ul)]
        public void NextUInt64_Test(int seed, ulong expected)
        {
            var state = new RandomRNGState(new Random(seed), sizeof(ulong));

            Assert.Equal(expected, state.NextUInt64());
        }

        [Theory]
        [InlineData(32, 48)]
        public void NextBytes_Test(uint bufferLength, uint count)
        {
            var state = new RandomRNGState(bufferLength: bufferLength);

            Assert.Equal(count, state.NextBytes(count).UInt32Count());
        }
    }

    public class UInt64TransitionRNGStateTests
    {
        [Fact]
        public void Ctor_ANEX_Test() =>
            Assert.Throws<ArgumentNullException>("prng", () => new UInt64TransitionRNGState(default, null));

        [Theory]
        [InlineData(0UL, 0UL)]
        public void NextUInt64_Test(ulong seed, ulong expected)
        {
            var state = new UInt64TransitionRNGState(seed, s => (s, s));

            Assert.Equal(expected, state.NextUInt64());
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(8, 1)]
        [InlineData(9, 2)]
        public void NextBytes_Test(uint count, uint calls)
        {
            var mockPrng = new FuncMock<ulong, (ulong, ulong)>(s => (s, s));
            
            var state = new UInt64TransitionRNGState(0, mockPrng.Invoke);

            var actual = state.NextBytes(count).ToList();

            Assert.Equal(count, actual.UInt32Count());
            Assert.Equal(calls, mockPrng.Calls.UInt32Count());
        }
    }

    public class XorShift64StarRNGStateTests
    {
        [Fact]
        public void Ctor_AOOREX_Test() =>
            Assert.Throws<ArgumentOutOfRangeException>("seed", () => new XorShift64StarRNGState(0));

        [Theory]
        [InlineData(1UL, 5180492295206395165UL)]
        [InlineData(ulong.MaxValue, 17954947803125907456UL)]
        public void NextUInt64_Test(ulong seed, ulong expected)
        {
            var state = new XorShift64StarRNGState(seed);

            Assert.Equal(expected, state.NextUInt64());
        }
    }
}
