using Xunit;

namespace Luger.Utilities.Tests
{
    public class IntExtTests
    {
        private static BijectiveDictionary<ulong, long> testdata = BijectiveDictionary.Create<ulong, long>(
            (0x0000_0000_0000_0000UL, 0),
            (0x7FFF_FFFF_FFFF_FFFFUL, long.MaxValue),
            (0x8000_0000_0000_0000UL, long.MinValue),
            (0xFFFF_FFFF_FFFF_FFFFUL, -1)
        );

        [Fact]
        public void AsInt64Test()
        {
            foreach (var (ul, l) in testdata)
                Assert.True(ul.AsInt64() == l);
        }

        [Fact]
        public void AsUInt64Test()
        {
            foreach (var (ul, l) in testdata)
                Assert.True(l.AsUInt64() == ul);
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
