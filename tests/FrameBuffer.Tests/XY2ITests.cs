using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace Luger.FrameBuffer.Tests
{
    public class XY2ITests
    {
        private static XY2I CreateXY2I(int k_x, int k_y, int m) => new XY2I(k_y) * XY2XY.Translation(m, 0) * XY2XY.Scale(k_x, 1);

        [Theory]
        [InlineData(1, 3, 0, 0, 0, 0)]
        [InlineData(1, 3, 0, 0, 1, 3)]
        [InlineData(1, 3, 0, 1, 0, 1)]
        [InlineData(1, 3, 0, 1, 1, 4)]
        [InlineData(1, 3, 4, 0, 0, 4)]
        [InlineData(1, 3, 4, 0, 1, 7)]
        [InlineData(1, 3, 4, 1, 0, 5)]
        [InlineData(1, 3, 4, 1, 1, 8)]
        public void MulOpCoordinatesTests(int k_x, int k_y, int m, int x, int y, int expected)
        {
            // Arrange
            var xy2i = CreateXY2I(k_x, k_y, m);
            var xy = (x, y);

            // Act
            var actual = xy2i * xy;

            // Assert
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> MulOpXY2XYTestData()
        {
            var xy2i = new XY2I(3);

            yield return new object[] { xy2i, XY2XY.Identity, xy2i };
            yield return new object[] { xy2i, XY2XY.Scale(2, 2), CreateXY2I(2, 6, 0) };
            yield return new object[] { xy2i, XY2XY.Scale(-1, -1), CreateXY2I(-1, -3, 0) };
            yield return new object[] { xy2i, XY2XY.Translation(1, 1), CreateXY2I(1, 3, 4) };
            yield return new object[] { xy2i, XY2XY.Transpose, CreateXY2I(3, 1, 0) };
        }

        [Theory]
        [MemberData(nameof(MulOpXY2XYTestData))]
        public void MulOpXY2XYTest(XY2I xy2i, XY2XY xy2xy, XY2I expected)
        {
            // Arrange

            // Act
            var actual = xy2i * xy2xy;

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
