using System;
using System.Numerics;

namespace Luger.Rendering.Renderer.Utils
{
    public readonly struct PointValuePair
    {
        public readonly Vector3 Point;
        public readonly Vector3 Value;

        public PointValuePair(in Vector3 point, in Vector3 value)
        {
            Point = point;
            Value = value;
        }

        public void Deconstruct(out Vector3 point, out Vector3 value)
        {
            point = Point;
            value = Value;
        }

        private static readonly Vector3 LuminanceFactor = new(.2126f, .7152f, .0722f);

        public static PointValuePair AverageSum(in PointValuePair dot1, in PointValuePair dot2)
        {
            var l1 = Vector3.Dot(dot1.Value, LuminanceFactor);
            var l2 = Vector3.Dot(dot2.Value, LuminanceFactor);
            // TODO: Is this linear interpolation based on luminance ok?
            var point = Vector3.Lerp(dot1.Point, dot2.Point, l2 / (l1 + l2));
            var value = dot1.Value + dot2.Value;
            return new(in point, in value);
        }

        public static PointValuePair AverageSum(ReadOnlySpan<PointValuePair> dots)
        {
            switch (dots.Length)
            {
                case 0:
                    return default;
                case 1:
                    return dots[0];
                case 2:
                    return AverageSum(dots[0], dots[1]);
                default:
                    var mi = dots.Length >> 1;
                    var as1 = AverageSum(dots[..mi]);
                    var as2 = AverageSum(dots[mi..]);
                    return AverageSum(in as1, in as2);
            }
        }
    }
}
