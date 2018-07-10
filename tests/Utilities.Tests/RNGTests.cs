using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Luger.Functional;
using Xunit;
using Xunit.Abstractions;

namespace Luger.Utilities.Tests
{
    public class IntExtTests
    {
        private ITestOutputHelper _output;

        public IntExtTests(ITestOutputHelper output) => _output = output;

        public static IEnumerable<object[]> as_testdata = new[]
        {
            new object[] { 0x0000_0000_0000_0000UL, 0 },
            new object[] { 0x7FFF_FFFF_FFFF_FFFFUL, long.MaxValue },
            new object[] { 0x8000_0000_0000_0000UL, long.MinValue },
            new object[] { 0xFFFF_FFFF_FFFF_FFFFUL, -1 }
        };

        [Theory]
        [MemberData(nameof(as_testdata))]
        public void AsInt64Test(ulong ul, long l) => Assert.Equal(ul.AsInt64(), l);

        [Theory]
        [MemberData(nameof(as_testdata))]
        public void AsUInt64Test(ulong ul, long l) => Assert.Equal(l.AsUInt64(), ul);

        private const int multestlimit = 100;

        public static IEnumerable<object[]> mul_testdata()
            => from x in Enumerable.Range(0, multestlimit)
               from y in Enumerable.Range(0, multestlimit)
               let bix = ((BigInteger)x << 64) / multestlimit
               let biy = ((BigInteger)y << 64) / multestlimit
               let bia = bix * biy >> 64
               select new object[] { (ulong)bix, (ulong)biy, (ulong)bia };

        [Theory]
        [MemberData(nameof(mul_testdata))]
        public void Mul64HiTest(ulong x, ulong y, ulong a) => Assert.Equal(IntExt.Mul64Hi(x, y), a);

        private const uint PT_Iterations = 10000000;

        // TODO: Put performance test in another test list
        [Fact]
        public void Mul64HiPerformanceTest()
        {
            var step = ulong.MaxValue / PT_Iterations;

            DateTime startTime = DateTime.Now;

            for (ulong i = 1; i <= PT_Iterations; i++)
            {
                var v = i * step;
                var p = v;
            }

            var noTime = DateTime.Now - startTime;
            _output.WriteLine("{0:N0} iterations over nothing took {1} time.", PT_Iterations, noTime);

            startTime = DateTime.Now;

            for (ulong i = 1; i <= PT_Iterations; i++)
            {
                var v = i * step;
                var p = IntExt.Mul64Hi(v, v);
            }

            var mul64hiTime = DateTime.Now - startTime - noTime;
            _output.WriteLine("{0:N0} iterations over Mul64Hi took {1} time.", PT_Iterations, mul64hiTime);

            startTime = DateTime.Now;

            for (ulong i = 0; i < PT_Iterations; i++)
            {
                var v = i * step;
                var biv1 = (BigInteger)v;
                var biv2 = (BigInteger)v;
                var p = (ulong)(biv1 * biv2 >> 64);
            }

            var biTime = DateTime.Now - startTime - noTime;
            _output.WriteLine("{0:N0} iterations over BigInteger took {1} time.", PT_Iterations, biTime);

            var mulsPerBIs = biTime / mul64hiTime;
            _output.WriteLine("Mul64Hi is {0:N2} times faster than BigInteger.", mulsPerBIs);
        }
    }

    public class RNGTests
    {
        private const ulong MockUInt64 = 0x123_4567_89AB_CDEF;
        private const byte MockByte = 42;

        private class MockRNGState : IRNGState
        {
            private readonly Func<ulong> _nextUInt64;
            private readonly Func<int, IEnumerable<byte>> _nextBytes;

            private MockRNGState(Func<ulong> nextUInt64, Func<int, IEnumerable<byte>> nextBytes)
            {
                _nextUInt64 = nextUInt64 ?? new Func<ulong>(() => MockUInt64);
                _nextBytes = nextBytes ?? new Func<int, IEnumerable<byte>>(count => Enumerable.Repeat(MockByte, count));
            }

            public MockRNGState() : this(null, null) { }

            public MockRNGState(ulong nextUInt64) : this(() => nextUInt64, null) { }

            public MockRNGState(IEnumerable<byte> nextBytes) : this(null, _ => nextBytes) { }

            public MockRNGState(Func<ulong> nextUInt64) : this(nextUInt64, null) { }

            public MockRNGState(Func<int, IEnumerable<byte>> nextBytes) : this(null, nextBytes) { }

            public ulong NextUInt64() => _nextUInt64();

            public IEnumerable<byte> NextBytes(int count) => _nextBytes(count);
        }

        [Fact]
        public void NextUInt64_Test()
        {
            var state = new MockRNGState();
            var (next, newState) = RNG.NextUInt64()(state);

            Assert.Equal(MockUInt64, next);
            Assert.Equal(state, newState);
        }

        public static IEnumerable<object[]> ValidNextNBitsData => from v in Enumerable.Range(1, 64) select new object[] { v };

        [Theory]
        [MemberData(nameof(ValidNextNBitsData))]
        public void NextNBits_Positive_Test(int n)
        {
            var state = new MockRNGState();
            var (next, _) = RNG.NextNBits(n)(state);

            Assert.Equal(MockUInt64 & (1ul << n) - 1, next);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65)]
        public void NextNBits_Negative_Test(int n) => Assert.Throws<ArgumentOutOfRangeException>(() => RNG.NextNBits(n));

        [Theory]
        [InlineData(100)]
        public void NextBytes_Positive_Test(int count)
        {
            var state = new MockRNGState();
            var (next, _) = RNG.NextBytes(count)(state);

            Assert.Equal(count, next.Count());
        }

        [Theory]
        [InlineData(-1)]
        public void NextBytes_Negative_Test(int count) => Assert.Throws<ArgumentOutOfRangeException>(() => RNG.NextBytes(count));

        [Theory]
        [InlineData(ulong.MinValue, 100, 0)]
        [InlineData(ulong.MaxValue, 100, 99)]
        public void NextUInt64_maxValue_Positive_Test(ulong nextUInt64, ulong maxValue, ulong expected)
        {
            var state = new MockRNGState(nextUInt64);
            var (next, _) = RNG.NextUInt64(maxValue)(state);

            Assert.Equal(expected, next);
        }

        [Theory]
        [InlineData(0)]
        public void NextUInt64_maxValue_Negative_Test(ulong maxValue) => Assert.Throws<ArgumentOutOfRangeException>(() => RNG.NextUInt64(maxValue));

        [Theory]
        [InlineData(ulong.MinValue, 100, 200, 100)]
        [InlineData(ulong.MaxValue, 100, 200, 199)]
        public void NextUInt64_minValue_maxValue_Positive_Test(ulong nextUInt64, ulong minValue, ulong maxValue, ulong expected)
        {
            var state = new MockRNGState(nextUInt64);
            var (next, _) = RNG.NextUInt64(minValue, maxValue)(state);

            Assert.Equal(expected, next);
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
        public void NextInt64(ulong nextUInt64, long expected)
        {
            var state = new MockRNGState(nextUInt64);
            var (next, _) = RNG.NextInt64()(state);

            Assert.Equal(expected, next);
        }

        [Theory]
        [InlineData(ulong.MinValue, 0d)]
        [InlineData(ulong.MaxValue, 1d)]
        public void NextDouble_Test(ulong nextUInt64, double expected)
        {
            var state = new MockRNGState(nextUInt64);
            var (next, _) = RNG.NextDouble()(state);

            Assert.Equal(expected, next);
        }
    }

    public class RandomRNGStateTests
    {
        [Theory]
        [InlineData(0, 0ul)]
        public void NextUInt64_Test(int seed, ulong expected)
        {
            var state = new RandomRNGState(new Random(seed), sizeof(ulong));

            Assert.Equal(expected, state.NextUInt64());
        }

        [Fact]
        public void NextBytes_Test()
        {
            var state = new RandomRNGState(bufferLength: 32);

            Assert.Equal(48, state.NextBytes(48).Count());
        }
    }
}
