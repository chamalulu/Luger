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
        public void Mul64HiTest(ulong x, ulong y, ulong a) =>
            Assert.Equal(Mul64Hi(x, y), a);

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

            var mul64hiTime = TimeAndReport(v => Mul64Hi(v, v), "Mul64Hi");

            static ulong TimeAndReportFuncBI(ulong v)
            {
                var vbi = (BigInteger)v;
                return (ulong)(vbi * vbi >> 64);
            }

            var biTime = TimeAndReport(TimeAndReportFuncBI, "BigInteger");

            var mulsPerBIs = (biTime - noTime) / (mul64hiTime - noTime);
            _output.WriteLine($"Mul64Hi is {mulsPerBIs:N2} times faster than BigInteger.");
        }


        private const ulong
            CBT_Target = 0x5555_5555_5555_5555UL,
            CBT_Source = 0x0123_4567_89AB_CDEFUL;

        [Theory]
        [InlineData(0, 32, 0x5555_5555_89AB_CDEFUL)]
        [InlineData(0, 64, CBT_Source)]
        [InlineData(32, 32, 0x0123_4567_5555_5555UL)]
        [InlineData(48, 32, 0x0123_5555_5555_CDEFUL)]
        public void CopyBitsTest(int offset, int width, ulong expected)
        {
            ulong actual = CopyBits(
                target: CBT_Target,
                source: CBT_Source,
                offset: (UInt6)offset,
                width: (byte)width);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 32, 32, 0x5555_5555_0123_4567UL)]
        [InlineData(32, 0, 32, 0x89AB_CDEF_5555_5555UL)]
        [InlineData(16, 16, 32, 0x5555_4567_89AB_5555UL)]
        public void CopyBitsShiftTest(
            int target_offset, int source_offset, int width, ulong expected)
        {
            ulong actual = CopyBits(
                  target: CBT_Target,
                  source: CBT_Source,
                  target_offset: (UInt6)target_offset,
                  source_offset: (UInt6)source_offset,
                  width: (byte)width);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(42U, 0U, 42U)]
        [InlineData(0U, 42U, 42U)]
        [InlineData(63U, 42U, 21U)]
        [InlineData(42U, 63U, 21U)]
        [InlineData(~0U - 4, ~0U - 16, 1U)] // Greatest primes < 2^32
        public void GcdUInt32Test(uint a, uint b, uint expected) =>
            Assert.Equal(expected, Gcd(a, b));

        [Theory]
        [InlineData(42UL, 0UL, 42UL)]
        [InlineData(0UL, 42UL, 42UL)]
        [InlineData(63UL, 42UL, 21UL)]
        [InlineData(42UL, 63UL, 21UL)]
        [InlineData(~0UL - 58, ~0UL - 82, 1UL)] // Greatest primes < 2^64
        public void GcdUInt64Test(ulong a, ulong b, ulong expected) =>
            Assert.Equal(expected, Gcd(a, b));

        [Theory]
        [InlineData(0, 0U)]
        [InlineData(int.MinValue, 1U << 31)]
        [InlineData(int.MaxValue, (1U << 31) - 1)]
        public void AbsInt32Test(int n, uint expected) =>
            Assert.Equal(expected, Abs(n));

        [Theory]
        [InlineData(0, 0UL)]
        [InlineData(long.MinValue, 1UL << 63)]
        [InlineData(long.MaxValue, (1UL << 63) - 1)]
        public void AbsInt64Test(long n, ulong expected) =>
            Assert.Equal(expected, Abs(n));

    }
}
