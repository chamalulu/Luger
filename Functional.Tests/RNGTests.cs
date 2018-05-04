using System;
using System.Collections.Generic;
using Xunit;

namespace Luger.Functional.Tests
{
    public class IntExtTests
    {
        private (ulong, long)[] testdata = new []
        {
            (0UL, 0L)
        };

        [Fact]
        public void AsInt64Test()
        {
            foreach (var (ul, l) in testdata)
                Assert.True(ul.AsInt64() == l);
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
