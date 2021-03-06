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

            private static ulong MockNextUInt64() => MockUInt64;

            private static IEnumerable<byte> MockNextBytes(uint count) => EnumerableExt.Repeat(MockByte, count);

            public MockRNGState(Func<ulong> nextUInt64, Func<uint, IEnumerable<byte>> nextBytes)
            {
                _nextUInt64 = nextUInt64;
                _nextBytes = nextBytes;
            }

            public MockRNGState() : this(MockNextUInt64, MockNextBytes) { }

            public MockRNGState(ulong nextUInt64) : this(() => nextUInt64, MockNextBytes) { }

            public MockRNGState(IEnumerable<byte> nextBytes) : this(MockNextUInt64, _ => nextBytes) { }

            public MockRNGState(Func<ulong> nextUInt64) : this(nextUInt64, MockNextBytes) { }

            public MockRNGState(Func<uint, IEnumerable<byte>> nextBytes) : this(MockNextUInt64, nextBytes) { }

            public ulong NextUInt64() => _nextUInt64();

            public IEnumerable<byte> NextBytes(uint count) => _nextBytes(count);
        }

        [Fact]
        public void NextUInt64Test()
        {
            var target = RNG.NextUInt64();
            var state = new MockRNGState();
            var (actual, newState) = target(state);

            Assert.Equal(MockUInt64, actual);
            Assert.Equal(state, newState);
        }

        public static IEnumerable<object[]> ValidNextNBitsData
            => from v in Enumerable.Range(1, 64) select new object[] { v };

        [Theory]
        [MemberData(nameof(ValidNextNBitsData))]
        public void NextNBitsPositiveTest(int n)
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
        public void NextNBitsNegativeTest(int n)
            => Assert.Throws<ArgumentOutOfRangeException>(() => RNG.NextNBits(n));

        [Theory]
        [InlineData(100)]
        public void NextBytesPositiveTest(uint count)
        {
            var target = RNG.NextBytes(count);
            var state = new MockRNGState();
            var actual = target.Run(state);

            Assert.Equal(count, actual.UCount());
        }

        [Theory]
        [InlineData(ulong.MinValue, 100, 0)]
        [InlineData(ulong.MaxValue, 100, 99)]
        public void NextUInt64MaxValuePositiveTest(ulong nextUInt64, ulong maxValue, ulong expected)
        {
            var target = RNG.NextUInt64(maxValue);
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0)]
        public void NextUInt64MaxValueNegativeTest(ulong maxValue)
            => Assert.Throws<ArgumentOutOfRangeException>(() => RNG.NextUInt64(maxValue));

        [Theory]
        [InlineData(ulong.MinValue, 100, 200, 100)]
        [InlineData(ulong.MaxValue, 100, 200, 199)]
        public void NextUInt64MinValueMaxValuePositiveTest(ulong nextUInt64, ulong minValue, ulong maxValue, ulong expected)
        {
            var target = RNG.NextUInt64(minValue, maxValue);
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(200, 100)]
        public void NextUInt64MinValueMaxValueNegativeTest(ulong minValue, ulong maxValue)
            => Assert.Throws<ArgumentException>(() => RNG.NextUInt64(minValue, maxValue));

        [Theory]
        [InlineData(ulong.MinValue, 0L)]
        [InlineData(0x7FFF_FFFF_FFFF_FFFFUL, long.MaxValue)]
        [InlineData(0x8000_0000_0000_0000UL, long.MinValue)]
        [InlineData(ulong.MaxValue, -1L)]
        public void NextInt64Test(ulong nextUInt64, long expected)
        {
            var target = RNG.NextInt64();
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> NextDoubleTestData => new (ulong next, double expected)[]
        {
            (ulong.MinValue, 0d),
            (ulong.MaxValue, BitConverter.Int64BitsToDouble(0x3FEF_FFFF_FFFF_FFFF)) // Greatest value of Double less than 1
        }.Select(args => new object[] { args.next, args.expected });

        [Theory]
        [MemberData(nameof(NextDoubleTestData))]
        public void NextDoubleTest(ulong nextUInt64, double expected)
        {
            var target = RNG.NextDouble();
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(NextDoubleTestData))]
        public void NextDoubleBCTest(ulong nextUInt64, double expected)
        {
            var target = RNG.NextDoubleBitOp();
            var state = new MockRNGState(nextUInt64);
            var actual = target.Run(state);

            Assert.Equal(expected, actual);
        }

        // TODO: Write uniformity tests
    }

    public class RandomRNGStateTests
    {
        [Theory]
        [InlineData(0, 15628745651041733658ul)]
        public void NextUInt64Test(int seed, ulong expected)
        {
            var state = new RandomRNGState(new Random(seed), sizeof(ulong));

            Assert.Equal(expected, state.NextUInt64());
        }

        [Theory]
        [InlineData(32, 48)]
        public void NextBytesTest(uint bufferLength, uint count)
        {
            var state = new RandomRNGState(bufferLength: bufferLength);

            Assert.Equal(count, state.NextBytes(count).UCount());
        }
    }

    public class UInt64TransitionRNGStateTests
    {
        [Theory]
        [InlineData(0UL, 0UL)]
        public void NextUInt64Test(ulong seed, ulong expected)
        {
            var state = new UInt64TransitionRNGState(seed, s => (s, s));

            Assert.Equal(expected, state.NextUInt64());
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(8, 1)]
        [InlineData(9, 2)]
        public void NextBytesTest(uint count, uint calls)
        {
            var mockPrng = new FuncMock<ulong, (ulong, ulong)>(s => (s, s));
            
            var state = new UInt64TransitionRNGState(0, mockPrng.Invoke);

            var actual = state.NextBytes(count).ToList();

            Assert.Equal(count, actual.UCount());
            Assert.Equal(calls, mockPrng.Calls.UCount());
        }
    }

    public class XorShift64StarRNGStateTests
    {
        [Fact]
        public void CtorAOORExTest() =>
            Assert.Throws<ArgumentOutOfRangeException>("seed", () => new XorShift64StarRNGState(0));

        [Theory]
        [InlineData(1UL, 5180492295206395165UL)]
        [InlineData(ulong.MaxValue, 17954947803125907456UL)]
        public void NextUInt64Test(ulong seed, ulong expected)
        {
            var state = new XorShift64StarRNGState(seed);

            Assert.Equal(expected, state.NextUInt64());
        }
    }
}
