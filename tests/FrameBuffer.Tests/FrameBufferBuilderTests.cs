
using Xunit;

using static Luger.FrameBuffer.Tests.TestUtils;

namespace Luger.FrameBuffer.Tests
{
    public class FrameBufferBuilderTests
    {
        [Fact]
        public void AddFFTest()
        {
            // Arrange
            using var manager = new FrameBufferManager();

            var target = manager.CreateFrameBufferBuilder<int>().Add((x, y) => x == y, (x, y) => x + 1);

            // Act
            var frameBuffer = target.ToFrameBuffer(3, 3);

            // Assert
            var expected = new int[,]
            { { 1, 0, 0 },
              { 0, 2, 0 },
              { 0, 0, 3 } };

            Assert.True(ArrayEqualsFrameBuffer(expected, frameBuffer));
        }

        [Fact]
        public void ToFrameBufferTest()
        {
            // Arrange
            using var manager = new FrameBufferManager();

            var target = manager.CreateFrameBufferBuilder<int>().Add((x, y) => x * y > 0, 1);

            // Act
            var frameBuffer = target.ToFrameBuffer(3, 3);

            // Assert
            var expected = new int[,]
            { { 0, 0, 0 },
              { 0, 1, 1 },
              { 0, 1, 1 } };

            Assert.True(ArrayEqualsFrameBuffer(expected, frameBuffer));
        }
    }
}
