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

        private static (ulong, long)[] as_testdata = new[]
        {
            (0x0000_0000_0000_0000UL, 0),
            (0x7FFF_FFFF_FFFF_FFFFUL, long.MaxValue),
            (0x8000_0000_0000_0000UL, long.MinValue),
            (0xFFFF_FFFF_FFFF_FFFFUL, -1)
        };

        [Fact]
        public void AsInt64Test()
        {
            foreach (var (ul, l) in as_testdata)
                Assert.True(ul.AsInt64() == l);
        }

        [Fact]
        public void AsUInt64Test()
        {
            foreach (var (ul, l) in as_testdata)
                Assert.True(l.AsUInt64() == ul);
        }

        private const int multestlimit = 100;

        private static IEnumerable<(ulong, ulong, ulong)> mul_testdata()
            => from x in Enumerable.Range(0, multestlimit)
               from y in Enumerable.Range(0, multestlimit)
               let bix = ((BigInteger)x << 64) / multestlimit
               let biy = ((BigInteger)y << 64) / multestlimit
               let bia = bix * biy >> 64
               select ((ulong)bix, (ulong)biy, (ulong)bia);

        [Fact]
        public void Mul64HiTest()
        {
            foreach (var (x, y, p) in mul_testdata())
            {
                var a = IntExt.Mul64Hi(x, y);
                Assert.True(a == p);
            }
        }

        private const uint PT_Iterations = 10000000;

        [Fact(Skip = "")]
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
        [Fact]
        public void RNGStateCtorNegative()
            => Assert.Throws<ArgumentOutOfRangeException>("seed", () => new RNGState(0));
        
        [Fact]
        public void NextNBits_FreshBitsUsed_Test()
        {
            Transition<ulong, ulong> prng = s => (0, s + 1);

            var state = new RNGState(1, 0x123_4567_89AB_CDEF, 32);
            var (next, newState) = RNG.NextNBits(16, prng)(state);

            Assert.True(next == 0x89AB);
            Assert.True(newState.Seed == 1);
            Assert.True(newState.Buffer == 0x123_4567_89AB_CDEF);
            Assert.True(newState.FreshBits == 16);
        }
        
        [Fact]
        public void NextNBits_FreshBitsInsufficient_Test()
        {
            Transition<ulong, ulong> prng = s => (0, s + 1);

            var state = new RNGState(1, 0x123_4567_89AB_CDEF, 16);
            var (next, newState) = RNG.NextNBits(32, prng)(state);

            Assert.True(next == 0xCDEF_0000);
            Assert.True(newState.Seed == 2);
            Assert.True(newState.Buffer == 0);
            Assert.True(newState.FreshBits == 48);
        }
    }
}
