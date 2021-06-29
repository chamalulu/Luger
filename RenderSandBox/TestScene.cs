using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace RenderSandBox
{
    internal class TestScene : IScene
    {
        private Matrix3x2 RedTransform, GreenTransform, BlueTransform;

        public float ViewRadius => 1f;

        private static Matrix3x2 CreateM32(Random rng)
        {
            var tx = (float)rng.NextDouble();
            var ty = (float)rng.NextDouble();
            var theta = (float)rng.NextDouble() * MathF.PI * .25f;
            var scale = (float)rng.NextDouble() * 15f + 1f;

            return Matrix3x2.Identity
                * Matrix3x2.CreateTranslation(tx, ty)
                * Matrix3x2.CreateRotation(theta)
                * Matrix3x2.CreateScale(scale);
        }

        public TestScene()
        {
            var rng = new Random();
            RedTransform = CreateM32(rng);
            GreenTransform = CreateM32(rng);
            BlueTransform = CreateM32(rng);
        }

        private static Vector2 Center = Vector2.One / 2f;

        private static float XY2I(Vector2 xy)
        {
            var xFloor = MathF.Floor(xy.X);
            var yFloor = MathF.Floor(xy.Y);
            var xyRemainder = xy - new Vector2(xFloor, yFloor);

            var distance = Vector2.Distance(xyRemainder, Center);

            return distance < .5f
                ? 1f/* - distance * 2f*/
                : 0f;
        }

        public ValueTask<Vector4> GetColor(Vector2 point, Vector2 size, CancellationToken cancellationToken)
        {
            var r = XY2I(Vector2.Transform(point, RedTransform));
            var g = XY2I(Vector2.Transform(point, GreenTransform));
            var b = XY2I(Vector2.Transform(point, BlueTransform));
            return ValueTask.FromResult(new Vector4(r, g, b, 1f));
        }
    }
}
