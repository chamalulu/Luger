using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RenderSandBox
{
    internal class TestScene : SceneBase<RgbaVector>
    {
        public TestScene(Size size, int jitter = 0) : base(new(Point.Empty, size), jitter: jitter) { }

        protected override ValueTask RenderRow(Rectangle rect, int row, RgbaVector[] buffer)
        {
            var rwh = (float)(rect.Width * rect.Height);

            for (var i = 0; i < buffer.Length; i++)
            {
                var k = i * row / rwh;
                var r = rect.X * k / View.Width;
                var b = rect.Y * k / View.Height;

                buffer[i] = new RgbaVector(r, r * b, b);
            }

            return ValueTask.CompletedTask;
        }
    }
}
