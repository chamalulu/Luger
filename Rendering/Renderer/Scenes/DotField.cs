using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Luger.Rendering.Renderer.Scenes
{
    public class DotField
    {
        private readonly struct PointValuePair
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
                    default:
                        var mi = dots.Length >> 1;
                        var as1 = AverageSum(dots[..mi]);
                        var as2 = AverageSum(dots[mi..]);
                        return AverageSum(in as1, in as2);
                }
            }
        }

        private interface INode
        {
            Vector3 this[in Vector3 point] { get; }
            PointValuePair AverageSum { get; }
        }

        private class LeafNode : INode
        {
            private readonly ImmutableArray<PointValuePair> _dots;

            public LeafNode(ImmutableArray<PointValuePair> dots)
            {
                _dots = dots;
                AverageSum = PointValuePair.AverageSum(dots.AsSpan());
            }

            public Vector3 this[in Vector3 point]
            {
                get
                {
                    var value = Vector3.Zero;

                    foreach (var dot in _dots)
                    {
                        value += dot.Value / Vector3.DistanceSquared(point, dot.Point);
                    }

                    return value;
                }
            }

            public PointValuePair AverageSum { get; }
        }

        private class Node : INode
        {
            private readonly Plane _partition;
            private readonly INode _negativeNode, _positiveNode;

            public Node(in Plane partition, INode negativeNode, INode positiveNode)
            {
                _partition = partition;
                _negativeNode = negativeNode;
                _positiveNode = positiveNode;

                AverageSum = PointValuePair.AverageSum(negativeNode.AverageSum, positiveNode.AverageSum);
            }

            public Vector3 this[in Vector3 point]
            {
                get
                {
                    var isPositive = Vector3.Dot(_partition.Normal.Value, point) + _partition.Distance >= 0f;
                    var (nearSide, farSide) = isPositive ? (_positiveNode, _negativeNode) : (_negativeNode, _positiveNode);

                    return nearSide[in point] + farSide.AverageSum.Value / Vector3.DistanceSquared(farSide.AverageSum.Point, point);
                }
            }

            public PointValuePair AverageSum { get; }
        }

        public class Builder : IDisposable
        {
            private interface IBuilderNode
            {
                IBuilderNode Add(in PointValuePair dot);
                INode Snapshot();
            }

            private class BuilderLeafNode : IBuilderNode
            {
                private const int MaxLeafSize = 128;

                private readonly ImmutableArray<PointValuePair>.Builder _dots

                    = ImmutableArray.CreateBuilder<PointValuePair>();

                private BuilderNode Split()
                {
                    // Calculate partition
                    var points = _dots.Select(dot => dot.Point);
                    var min = points.Aggregate(Vector3.Min);
                    var max = points.Aggregate(Vector3.Max);
                    var normal = new Direction(max - min);
                    var center = Vector3.Lerp(min, max, .5f);
                    var distance = -Vector3.Dot(center, normal.Value);
                    var partition = new Plane(normal, distance);

                    // Instanciate builder node
                    var builderNode = new BuilderNode(in partition);

                    // Add all dots to builder node
                    foreach (var dot in _dots)
                    {
                        builderNode.Add(in dot);
                    }

                    return builderNode;
                }

                public IBuilderNode Add(in PointValuePair dot)
                {
                    if (_dots.Count < MaxLeafSize)
                    {
                        _dots.Add(dot);
                        return this;
                    }
                    else
                    {
                        var builderNode = Split();
                        builderNode.Add(in dot);
                        return builderNode;
                    }
                }

                public INode Snapshot() => new LeafNode(_dots.ToImmutable());
            }

            private class BuilderNode : IBuilderNode
            {
                private readonly Plane _partition;
                private IBuilderNode _negative, _positive;

                public BuilderNode(in Plane partition)
                {
                    _partition = partition;
                    _negative = new BuilderLeafNode();
                    _positive = new BuilderLeafNode();
                }

                public void Add(in PointValuePair dot)
                {
                    if (Vector3.Dot(_partition.Normal.Value, dot.Point) + _partition.Distance >= 0f)
                    {
                        _positive = _positive.Add(in dot);
                    }
                    else
                    {
                        _negative = _negative.Add(in dot);
                    }
                }

                public INode Snapshot() => new Node(in _partition, _negative.Snapshot(), _positive.Snapshot());

                IBuilderNode IBuilderNode.Add(in PointValuePair dot)
                {
                    Add(in dot);
                    return this;
                }
            }

            private IBuilderNode _root;
            private readonly ConcurrentBag<PointValuePair> _inBag;
            private SemaphoreSlim _ingestSemaphore;
            private CancellationTokenSource _ingestCancellationTokenSource;
            private Task _ingestTask;
            private bool disposedValue;

            private async Task IngestTask(CancellationToken cancellationToken)
            {
                while(!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await _ingestSemaphore.WaitAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    // We could just take one since the semaphore will be signalled once per added dot...
                    // ...but eagerly emptying the bag feels more robust.
                    while (_inBag.TryTake(out var dot))
                    {
                        _root = _root.Add(in dot);
                    }
                }
            }

            public Builder()
            {
                _root = new BuilderLeafNode();
                _inBag = new();
                _ingestSemaphore = new(0);
                _ingestCancellationTokenSource = new();
                _ingestTask = IngestTask(_ingestCancellationTokenSource.Token);
            }

            public void Add(in Vector3 point, in Vector3 value)
            {
                _inBag.Add(new(in point, in value));
                _ingestSemaphore.Release();
            }

            public DotField ToDotField() => new(_root.Snapshot());

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _ingestCancellationTokenSource.Cancel();
                        _ingestTask.Wait(); // Not really necessary
                        _ingestSemaphore.Dispose();
                    }

                    _root = null!;
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private readonly INode _root;

        private DotField(INode root) => _root = root;

        public Vector3 this[in Vector3 point] => _root[in point];
    }
}
