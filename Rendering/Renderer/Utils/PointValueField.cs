using System;
using System.Collections.Immutable;
using System.Numerics;

using Plane = Luger.Rendering.Renderer.Scenes.Plane;

namespace Luger.Rendering.Renderer.Utils
{
    /// <summary>
    /// Base class for immutable field of point value pairs.
    /// </summary>
    public abstract class PointValueFieldBase
    {
        private readonly PointValuePair _averageSum;

        /// <summary>
        /// Read-only reference to single point value pair representing average point and sum value of field.
        /// </summary>
        public ref readonly PointValuePair AverageSum => ref _averageSum;

        /// <summary>
        /// Number of point value pairs in field.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Depth of spatial tree holding point value pairs.
        /// </summary>
        public int Depth { get; }

        private protected PointValueFieldBase(PointValuePair averageSum, int count, int depth)
        {
            _averageSum = averageSum;
            Count = count;
            Depth = depth;
        }

        /// <summary>
        /// Produce value of field at given point.
        /// </summary>
        /// <remarks>
        /// Point does not have to coincide with an actual stored point value pair.
        /// Actually, if it does, value will be infinite or NaN.
        /// </remarks>
        public abstract Vector3 this[in Vector3 point] { get; }
    }

    /// <summary>
    /// Leaf of immutable field of point value pairs. 
    /// </summary>
    /// <remarks>
    /// Stores an immutable array of point value pairs.
    /// </remarks>
    public class PointValueFieldLeaf : PointValueFieldBase
    {
        private readonly ImmutableArray<PointValuePair> _pointValues;

        public PointValueFieldLeaf(ImmutableArray<PointValuePair> pointValues)
            : base(PointValuePair.AverageSum(pointValues.AsSpan()), pointValues.Length, 1)

            => _pointValues = pointValues;

        public override Vector3 this[in Vector3 point]
        {
            get
            {
                var value = Vector3.Zero;

                foreach (var pv in _pointValues)
                {
                    value += pv.Value / Vector3.DistanceSquared(point, pv.Point);
                }

                return value;
            }
        }
    }

    /// <summary>
    /// Node of immutable field of point value pairs.
    /// </summary>
    /// <remarks>
    /// Stores two partitions of field of point value pairs. One negative and one positive w.r.t. a partitioning plane.
    /// </remarks>
    public class PointValueFieldNode : PointValueFieldBase
    {
        private readonly Plane _partition;
        private readonly PointValueFieldBase _negative, _positive;

        public PointValueFieldNode(in Plane partition, PointValueFieldBase negative, PointValueFieldBase positive) : base(
            averageSum: PointValuePair.AverageSum(negative.AverageSum, positive.AverageSum),
            count: negative.Count + positive.Count,
            depth: Math.Max(negative.Depth, positive.Depth) + 1)
        {
            _partition = partition;
            _negative = negative;
            _positive = positive;
        }

        public override Vector3 this[in Vector3 point]
        {
            get
            {
                var isPositive = Vector3.Dot(_partition.Normal.Value, point) + _partition.Distance >= 0f;

                if (isPositive)
                {
                    ref readonly var farAverageSum = ref _negative.AverageSum;
                    return _positive[in point] + farAverageSum.Value / Vector3.DistanceSquared(farAverageSum.Point, point);
                }
                else
                {
                    ref readonly var farAverageSum = ref _positive.AverageSum;
                    return _negative[in point] + farAverageSum.Value / Vector3.DistanceSquared(farAverageSum.Point, point);
                }
            }
        }
    }
}
