using System;
using System.Numerics;

namespace Luger.Rendering.Renderer.Scenes
{
    public class BasePlanesScene : Scene3DBase
    {
        private static readonly Vector3 RotateToZAngles = new(-2f * MathF.PI / 3f, 2f * MathF.PI / 3f, 0f);
        private static readonly Vector3 UnitXYZ = Vector3.Normalize(Vector3.One);

        public BasePlanesScene(Camera camera) : base(camera) { }

        private static float CalculateBrightness(in HalfLine ray, in Plane plane)
        {
            if (ray.Intersect(plane, out var distance))
            {
                if (float.IsNaN(distance))
                {
                    // We're looking straight down the plane. Very bright indeed.
                    return 1f;
                }
                else
                {
                    // Get point of intersection
                    var intersection = ray.Start + ray.Direction * distance;

                    // Rotate around (1,1,1) to get intersection in XY plane
                    var rotateToZAngle = Vector3.Dot(plane.Normal.Value, RotateToZAngles);
                    var rotation = Quaternion.CreateFromAxisAngle(UnitXYZ, rotateToZAngle);
                    var inPlaneXY = Vector3.Transform(intersection.Value, rotation);

                    // Calculate intersection distance to integer grid per dimension (and set Z distance to +Inf)
                    var inPlaneXYRounded = new Vector3(MathF.Round(inPlaneXY.X), MathF.Round(inPlaneXY.Y), float.PositiveInfinity);
                    var gridDistance = Vector3.Abs(inPlaneXYRounded - inPlaneXY);

                    // Calculate brightness contribution per dimension as inverse of 1 + 100 * distance^2
                    var gridBrightness = Vector3.One / (Vector3.One + 100f * gridDistance * gridDistance);

                    // Calculate compound brightness as negation of product of negation of brightness per dimension
                    var gridDarkness = Vector3.One - gridBrightness;
                    var brightness = 1f - gridDarkness.X * gridDarkness.Y;

                    // Calculate projected brightness as inverse of 1 + distance^2 / 100.
                    // About these factors, I'm not a physicist.
                    var projectedBrightness = brightness / (1f + .01f * distance * distance);

                    return projectedBrightness;
                }
            }
            else
            {
                // We're missing the plane altogether. Very dark indeed.
                return 0f;
            }
        }

        protected override Vector4 GetColor(in HalfLine ray)
        {
            var r = CalculateBrightness(in ray, Plane.XYPlane);
            var g = CalculateBrightness(in ray, Plane.XZPlane);
            var b = CalculateBrightness(in ray, Plane.YZPlane);

            return new Vector4(r, g, b, 1f);
        }
    }
}
