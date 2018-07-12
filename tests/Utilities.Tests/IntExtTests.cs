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

        private const int multestcount = 3;

        public static IEnumerable<object[]> mul_testdata()
            => from x in Enumerable.Range(0, multestcount)
               from y in Enumerable.Range(0, multestcount)
               let bix = ((BigInteger)x << 64) / multestcount
               let biy = ((BigInteger)y << 64) / multestcount
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
}
