using System.Linq;

using Xunit;

namespace Luger.FrameBuffer.Tests
{
    public class FrameBufferMemoryOwnerTests
    {
        [Fact]
        public void CtorTest()
        {
            // Arrange
            var values = Enumerable.Range(0, 5).GroupBy(e => e / 3);    // { { 0, 1, 2 }, { 3, 4 } }

            // Act
            using var actual = new FrameBufferMemoryOwner<int>(3, 3, values);

            // Assert
            var expected = new int[,]
            { { 0, 1, 2 },
              { 3, 4, 0 },
              { 0, 0, 0 } };

            Assert.True(TestUtils.ArrayEqualsFrameBuffer(expected, actual.FrameBuffer));
        }
    }
}
