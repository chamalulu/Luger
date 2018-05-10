using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace Luger.Utilities.Tests
{
    public class IntExtTests
    {
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

        // private static IEnumerable<(ulong, ulong, ulong)> mul_testdata()
        //     => new [] {(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue-1)};

        [Fact]
        public void Mul64HiTest()
        {
            foreach (var (x, y, p) in mul_testdata())
            {
                var a = IntExt.Mul64Hi(x, y);
                Assert.True(a == p);
            }
        }
    }

    public class RNGTests
    {
        [Fact]
        public void Test1()
        {

        }
    }
}
