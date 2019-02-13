using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Luger.Functional;
using Xunit;
using Xunit.Abstractions;
using static Luger.Utilities.IntExt;

namespace Luger.Utilities.Tests
{
    public class IntExtTests
    {
        private ITestOutputHelper _output;

        public IntExtTests(ITestOutputHelper output) => _output = output;

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


        private const ulong CBT_Target = 0x5555_5555_5555_5555UL, CBT_Source = 0x0123_4567_89AB_CDEFUL;

        [Theory]
        [InlineData(0, 32, 0x5555_5555_89AB_CDEFUL)]
        [InlineData(0, 64, CBT_Source)]
        [InlineData(32, 32, 0x0123_4567_5555_5555UL)]
        [InlineData(48, 32, 0x0123_5555_5555_CDEFUL)]
        public void CopyBitsTest(int offset, int width, ulong expected)
            => Assert.Equal(expected, IntExt.CopyBits(CBT_Target, CBT_Source, (UInt6) offset, (byte) width));

        [Theory]
        [InlineData(0, 32, 32, 0x5555_5555_0123_4567UL)]
        [InlineData(32, 0, 32, 0x89AB_CDEF_5555_5555UL)]
        [InlineData(16, 16, 32, 0x5555_4567_89AB_5555UL)]
        public void CopyBitsShiftTest(int target_offset, int source_offset, int width, ulong expected)
            => Assert.Equal(expected, IntExt.CopyBits(CBT_Target, CBT_Source, (UInt6) target_offset, (UInt6) source_offset, (byte) width));
    }
}
