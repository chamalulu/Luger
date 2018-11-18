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

        private const uint multestcount = 3;

        public static IEnumerable<object[]> mul_testdata()
            => from x in EnumerableExt.RangeUInt32(multestcount)
               from y in EnumerableExt.RangeUInt32(multestcount)
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
            TimeSpan TimeAndReport(Func<ulong, ulong> f, string name)
            {
                var startTime = DateTime.Now;

                for (ulong i = 1; i <= PT_Iterations; i++)
                {
                    var v = i * (ulong.MaxValue / PT_Iterations);
                    var p = f(v);
                }

                var time = DateTime.Now - startTime;
                _output.WriteLine($"{PT_Iterations:N0} iterations over {name} took {time} time.");

                return time;
            }

            var noTime = TimeAndReport(v => v, "nothing");

            var mul64hiTime = TimeAndReport(v => IntExt.Mul64Hi(v, v), "Mul64Hi");

            var biTime = TimeAndReport(v => (ulong)((BigInteger)v * (BigInteger)v >> 64), "BigInteger");

            var mulsPerBIs = (biTime - noTime) / (mul64hiTime - noTime);
            _output.WriteLine($"Mul64Hi is {mulsPerBIs:N2} times faster than BigInteger.");
        }
    }
}
