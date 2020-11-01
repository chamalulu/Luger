
using Xunit;

using static Luger.FrameBuffer.Tests.TestUtils;

namespace Luger.FrameBuffer.Tests
{
    public class FrameBufferTests
    {
        [Fact]
        public void TransposeTest()
        {
            // Arrange
            var (_, target) = TestBuffer(3, 2);

            // Act
            var actual = target.Transpose;

            // Assert
            Assert.Equal(2, actual.Width);
            Assert.Equal(3, actual.Height);

            var expected = new[,]
            { { 0, 3 },
              { 1, 4 },
              { 2, 5 } };

            Assert.True(ArrayEqualsFrameBuffer(expected, actual));
        }

        [Fact]
        public void GetElementTest()
        {
            // Arrange
            var (_, target) = TestBuffer(3, 2);

            // Act
            var actual = target[2, 1];

            // Assert
            Assert.Equal(5, actual);
        }

        [Fact]
        public void SetElementTest()
        {
            // Arrange
            var (buffer, target) = TestBuffer(3, 2);

            // Act
            target[2, 1] = 42;

            // Assert
            Assert.Equal(42, buffer[5]);
        }

        [Fact]
        public void IndexRangeWindowTest()
        {
            // Arrange
            var (_, target) = TestBuffer(3, 2);

            // Act
            var actual = target[0, ..];

            // Assert
            var expected = new[,]
            { {0},
              {3} };

            Assert.True(ArrayEqualsFrameBuffer(expected, actual));
        }

        [Fact]
        public void RangeIndexWindowTest()
        {
            // Arrange
            var (_, target) = TestBuffer(3, 2);

            // Act
            var actual = target[.., 1];

            // Assert
            var expected = new[,]
            { {3, 4, 5} };

            Assert.True(ArrayEqualsFrameBuffer(expected, actual));
        }

        [Fact]
        public void RangeRangeWindowTest()
        {
            // Arrange
            var (_, target) = TestBuffer(3, 2);

            // Act
            var actual = target[1.., 1..];

            // Assert
            var expected = new[,]
            { { 4, 5 } };

            Assert.True(ArrayEqualsFrameBuffer(expected, actual));
        }
    }
}
