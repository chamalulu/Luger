using System;
using System.Collections.Generic;
using System.Numerics;

namespace Luger.Rendering.Renderer.Scenes
{
    public class MandelbrotScene : IScene
    {
        public IReadOnlyList<Vector4> Palette { get; init; } = new[] { Vector4.Zero, Vector4.One };
        public Vector4 Inside { get; init; } = Vector4.Zero;
        public int MaxIterations { get; init; } = 100;

        public RectF ViewArea { get; init; } = new(-2.5f, -1.5f, 3.5f, 3f);

        private const double Bailout = 256d;    // Need to be large enough to dwarf c in interpolation calculation

        public Vector4 GetColor(Vector2 point, Vector2 size)
        {
            var c = new Complex(point.X, point.Y);

            // P1-bulb check. Bounded by cardioid from fixed circle of radius 1/4 centered at 0.
            /*  (1)        c = u/2 * (1 - u/2)
             *          => c = u/2 - u^2/4
             *    (*-4) => -4c = u^2 - 2u
             *     (kk) => -4c = (u - 1)^2 - 1
             *     (+1) => 1 - 4c = (u - 1)^2
             *   (sqrt) => (+/-)sqrt(1 - 4c) = u - 1
             *     (+1) => 1 (+/-) sqrt(1 - 4c) = u
             *     
             *  (2)        |u| < 1
             *  
             *  (3) (1,2) => |1 (+/-) sqrt(1 - 4c)| < 1
             *  
             *  We're only interested in the non-principal square root as the principal square root, having non-negative real part,
             *   cannot fulfill the inequality
             */
            if ((1 - Complex.Sqrt(1 - c * 4)).Magnitude < 1)
            {
                return Inside;
            }

            // P2-bulb check. Bounded by circle of radius 1/4 centered at -1.
            if ((c + 1).Magnitude < .25d)
            {
                return Inside;
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
                    return Palette[0];
                }

                var color1 = Palette[n % Palette.Count];
                var color2 = Palette[(n + 1) % Palette.Count];

                // Bailout < |z| <= Bailout^2    (given Bailout is much larger than |c|)
                var log_bo_z = Math.Log(z.Magnitude, Bailout); // 1 < log_bo_z <= 2
                var amount = 2d - log_bo_z;  // 0 <= amount < 1

                return Vector4.Lerp(color1, color2, (float)amount);
            }
            else
            {
                return Inside;
            }
        }
    }
}
