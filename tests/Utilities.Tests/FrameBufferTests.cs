using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Luger.Utilities.Tests
{
    public class FrameBufferTests
    {
        private static int[][] TestValues(int width, int height, IEnumerable<int>? values = null)
            => (values ?? Enumerable.Range(0, width * height))
                .Select((v, i) => (value: v, row: i / width))
                .GroupBy(t => t.row, t => t.value)
                .Select(row => row.ToArray())
                .ToArray();

        private static bool Equals(int [][] array, IFrameBuffer<int> frameBuffer)
        {
            var equal = from i in Enumerable.Range(0, frameBuffer.Height)
                        from j in Enumerable.Range(0, frameBuffer.Width)
                        let line = array.Skip(i).FirstOrDefault()
                        let arrayElement = line?.Skip(j).FirstOrDefault() ?? 0
                        let x = j
                        let y = i
                        select (arrayElement, fbElement: frameBuffer[x, y]);

            return equal.All(ep => ep.arrayElement == ep.fbElement);
        }

        [Fact]
        public void FrameBufferTest()
        {
            // Arrange
            var values = TestValues(3, 3, Enumerable.Range(0, 5));

            // Act
            using var actual = new FrameBuffer<int>(3, 3, values);

            // Assert
            Assert.Equal(3, actual.Width);
            Assert.Equal(3, actual.Height);
            Assert.True(Equals(values, actual));
        }

        [Fact]
        public void WindowTestOff1x1()
        {
            // Arrange
            var values = TestValues(3, 3);

            using var target = new FrameBuffer<int>(3, 3, values);

            // Act
            var actual = target[1.., 1..];

            // Assert
            Assert.Equal(2, actual.Width);
            Assert.Equal(2, actual.Height);

            var expected = new[]
            {
                new[] { 4, 5 },
                new[] { 7, 8 }
            };

            Assert.True(Equals(expected, actual));
        }

        [Fact]
        public void TransposeTest()
        {
            // Arrange
            var values = TestValues(3, 2);

            using var target = new FrameBuffer<int>(3, 2, values);

            // Act
            var actual = target.Transpose;

            // Assert
            Assert.Equal(2, actual.Width);
            Assert.Equal(3, actual.Height);

            var expected = new[]
            {
                new[] { 0, 3 },
                new[] { 1, 4 },
                new[] { 2, 5 }
            };

            Assert.True(Equals(expected, actual));
        }
    }
}
