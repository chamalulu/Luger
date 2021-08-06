using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace RenderSandBox
{
    internal class MandelbrotScene : IScene
    {
        public Matrix3x2 Scene2Plane { get; init; } = Matrix3x2.CreateScale(3) * Matrix3x2.CreateTranslation(-.75f, 0);
        public IReadOnlyList<Vector4> Palette { get; init; } = new[] { Vector4.Zero, Vector4.One };
        public Vector4 Inside { get; init; } = Vector4.Zero;
        public int MaxIterations { get; init; } = 100;

        public float ViewRadius => 1f;

        private const double Bailout = 256d;    // Larger 

        public ValueTask<Vector4> GetColor(Vector2 point, Vector2 size, CancellationToken cancellationToken)
        {
            var pointInPlane = Vector2.Transform(point, Scene2Plane);

            var c = new Complex(pointInPlane.X, pointInPlane.Y);

            {
                var x = c.Real;
                var y = c.Imaginary;
                var y2 = y * y;

                // Cardioid check
                var q = x * (x - .5d) + 1d / 16 + y2;
                if (4 * q * (q + x - .25d) <= y2)
                {
                    return ValueTask.FromResult(Inside);
                }

                // P2-bulb check
                if (x * (x + 2) + y2 <= -15d / 16)
                {
                    return ValueTask.FromResult(Inside);
                }
            }

            var z = Complex.Zero;
            var n = 0;
            var period = 0;
            var z_old = z;

            while (z.Magnitude <= Bailout && n < MaxIterations)
            {
                z = z * z + c;
                n += 1;

                if (z == z_old)
                {
                    n = MaxIterations;
                }

                if (++period > 20)
                {
                    period = 0;
                    z_old = z;
                }
            }

            if (n < MaxIterations)
            {
                // Calibrate n and cutoff outside
                if (n >= 5) // number to calibrate is linear with log2(log2(Bailout)). Generalize another day.
                {
                    n -= 5;
                }
                else
                {
                    return ValueTask.FromResult(Palette[0]);
                }

                var color1 = Palette[n % Palette.Count];
                var color2 = Palette[(n + 1) % Palette.Count];

                // Bailout < |z| <= Bailout^2    (given Bailout is much larger than |c|)
                var log_bo_z = Math.Log(z.Magnitude, Bailout); // 1 < log_bo_z <= 2
                var amount = 2d - log_bo_z;  // 0 <= amount < 1

                var color = Vector4.Lerp(color1, color2, (float)amount);

                //color = c.Imaginary > 0 ? color : color1;

                return ValueTask.FromResult(color);
            }
            else
            {
                return ValueTask.FromResult(Inside);
            }
        }
    }
}
