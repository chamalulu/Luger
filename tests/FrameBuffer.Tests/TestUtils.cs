using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Luger.FrameBuffer.Tests
{
    internal static class TestUtils
    {
        public static (int[] buffer, FrameBuffer<int> frameBuffer) TestBuffer(int width, int height)
        {
            var buffer = Enumerable.Range(0, width * height).ToArray();
            var frameBuffer = FrameBuffer<int>.Create(buffer, width, height);

            return (buffer, frameBuffer);
        }

        public static bool ArrayEqualsFrameBuffer(int[,] array, FrameBuffer<int> frameBuffer)
        {
            var rows = array.GetLength(0);
            var columns = array.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                var y = i;

                for (int j = 0; j < columns; j++)
                {
                    var x = j;

                    if (array[i, j] != frameBuffer[x, y])
                        return false;
                }
            }

            return true;
        }
    }
}
