using System.Diagnostics;
using System.Numerics;

namespace Luger.Rendering.Renderer.Scenes
{
    [DebuggerDisplay("{Value}")]
    public readonly struct Point
    {
        public static readonly Point Origin = default;

        public readonly Vector3 Value;

        public Point(in Vector3 value) => Value = value;

        public Point(float x, float y, float z) : this(new Vector3(x, y, z)) { }

        public static Point operator +(in Point point, in Vector3 offset) => new(point.Value + offset);

        public static Point operator -(in Point point, in Vector3 offset) => new(point.Value - offset);

        public static Vector3 operator -(in Point left, in Point right) => left.Value - right.Value;

        public static Point operator *(in Point point, in Matrix4x4 transformation)

            => new(Vector3.Transform(point.Value, transformation));

    }

    [DebuggerDisplay("{Value}")]
    public readonly struct Direction
    {
        public static readonly Direction PositiveX = new(Vector3.UnitX);
        public static readonly Direction PositiveY = new(Vector3.UnitY);
        public static readonly Direction PositiveZ = new(Vector3.UnitZ);
        public static readonly Direction NegativeX = new(-Vector3.UnitX);
        public static readonly Direction NegativeY = new(-Vector3.UnitY);
        public static readonly Direction NegativeZ = new(-Vector3.UnitZ);

        public readonly Vector3 Value;

        public Direction(in Vector3 value) => Value = Vector3.Normalize(value);

        public static Vector3 operator *(in Direction direction, float distance) => direction.Value * distance;

        public static Direction operator *(in Direction direction, in Matrix4x4 transformation)

            => new(Vector3.TransformNormal(direction.Value, transformation));
    }

    [DebuggerDisplay("Fulcrum: {Fulcrum}, Direction: {Direction}")]
    public readonly struct Line
    {
        public static readonly Line XAxis = new(Point.Origin, Direction.PositiveX);
        public static readonly Line YAxis = new(Point.Origin, Direction.PositiveY);
        public static readonly Line ZAxis = new(Point.Origin, Direction.PositiveZ);

        public readonly Point Fulcrum;

        public readonly Direction Direction;

        public Line(in Point fulcrum, in Direction direction)
        {
            Fulcrum = fulcrum;
            Direction = direction;
        }
    }

    [DebuggerDisplay("Start: {Start}, Direction: {Direction}")]
    public readonly struct HalfLine
    {
        public static readonly Line PositiveXAxis = new(Point.Origin, Direction.PositiveX);
        public static readonly Line PositiveYAxis = new(Point.Origin, Direction.PositiveY);
        public static readonly Line PositiveZAxis = new(Point.Origin, Direction.PositiveZ);
        public static readonly Line NegativeXAxis = new(Point.Origin, Direction.NegativeX);
        public static readonly Line NegativeYAxis = new(Point.Origin, Direction.NegativeY);
        public static readonly Line NegativeZAxis = new(Point.Origin, Direction.NegativeZ);

        public readonly Point Start;

        public readonly Direction Direction;

        public HalfLine(in Point start, in Direction direction)
        {
            Start = start;
            Direction = direction;
        }

        public static HalfLine operator *(in HalfLine halfLine, Matrix4x4 transformation)

            => new(halfLine.Start * transformation, halfLine.Direction * transformation);

        public static HalfLine operator +(in HalfLine halfLine, float distance)

            => new(halfLine.Start + halfLine.Direction * distance, halfLine.Direction);
    }

    [DebuggerDisplay("Normal: {Normal}, Distance: {Distance}")]
    public readonly struct Plane
    {
        public static readonly Plane XYPlane = new(Direction.PositiveZ, 0f);
        public static readonly Plane XZPlane = new(Direction.PositiveY, 0f);
        public static readonly Plane YZPlane = new(Direction.PositiveX, 0f);

        public readonly Direction Normal;

        public readonly float Distance;

        public Plane(in Direction normal, float distance)
        {
            Normal = normal;
            Distance = distance;
        }
    }

    public static class Geometry
    {
        /// <summary>
        /// Calculate intersection of a line and a plane.
        /// </summary>
        /// <param name="line">Line</param>
        /// <param name="plane">Plane</param>
        /// <param name="distance">
        /// Scalar s.t. line.Fulcrum + line.Direction * distance is point of intersection.<br/>
        /// NaN if line is a subset of plane.<br/>
        /// (+/-)Inf if line does not intersect plane.
        /// </param>
        /// <returns>
        /// False if line is parallel with, but not a subset of, plane; otherwise, True.
        /// </returns>
        public static bool Intersect(this in Line line, in Plane plane, out float distance)
        {
            distance =
                -(Vector3.Dot(plane.Normal.Value, line.Fulcrum.Value) + plane.Distance)
                / Vector3.Dot(plane.Normal.Value, line.Direction.Value);

            return !float.IsInfinity(distance);
        }

        /// <summary>
        /// Calculate intersection of a halfline and a plane.
        /// </summary>
        /// <param name="halfLline">Halfline</param>
        /// <param name="plane">Plane</param>
        /// <param name="distance">
        /// Scalar s.t. halfLine.Start + halfLine.Direction * distance is point of intersection (even if in negative line direction).<br/>
        /// NaN if halfline is a subset of plane.<br/>
        /// (+/-)Inf if halfline does not intersect plane.
        /// </param>
        /// <returns>
        /// False if halfline is parallel with, but not a subset of, plane or intersection is in negative line direction; otherwise, True.
        /// </returns>
        public static bool Intersect(this in HalfLine halfLine, in Plane plane, out float distance)
        {
            distance =
                -(Vector3.Dot(plane.Normal.Value, halfLine.Start.Value) + plane.Distance)
                / Vector3.Dot(plane.Normal.Value, halfLine.Direction.Value);

            return float.IsFinite(distance) && distance >= 0f || float.IsNaN(distance);
        }
    }
}
