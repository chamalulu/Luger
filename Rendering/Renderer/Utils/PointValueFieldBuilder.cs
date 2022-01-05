using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;

using Luger.Rendering.Renderer.Scenes;

using Plane = Luger.Rendering.Renderer.Scenes.Plane;

namespace Luger.Rendering.Renderer.Utils
{
    public sealed class PointValueFieldBuilder : IDisposable
    {
        private interface IState { }

        private record LeafState(ImmutableArray<PointValuePair>.Builder PointValues) : IState;

        private record NodeState(
            Plane Partition,
            PointValueFieldBuilder NegativeBuilder,
            PointValueFieldBuilder PositiveBuilder) : IState, IDisposable
        {
            public void Dispose()
            {
                NegativeBuilder.Dispose();
                PositiveBuilder.Dispose();
            }
        }

        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
        private IState? _state = new LeafState(ImmutableArray.CreateBuilder<PointValuePair>());

        public PointValueFieldBuilder(int maxLeafSize = 128)

            => MaxLeafSize = maxLeafSize > 0
                ? maxLeafSize
                : throw new ArgumentOutOfRangeException(nameof(maxLeafSize));

        public int MaxLeafSize { get; }

        private static readonly ArrayPool<Vector3> Vector3ArrayPool = ArrayPool<Vector3>.Shared;

        private static Direction PrincipalComponent(IReadOnlyList<Vector3> points)
        {
            var count = points.Count;
            var X = Vector3ArrayPool.Rent(count);

            // Fill buffer with points and calculate empirical mean.
            var empMean = Vector3.Zero;

            for (var i = 0; i < count; i++)
            {
                empMean += X[i] = points[i];
            }

            empMean /= points.Count;

            // Translate points by -empirical mean
            for (var i = 0; i < count; i++)
            {
                X[i] -= empMean;
            }

            // Power iteration algorithm stolen from Wikipedia (https://en.wikipedia.org/wiki/Principal_component_analysis)
            /* r = a random vector of length p
             * r = r / norm(r)
             * do c times:
             *     s = 0 (a vector of length p)
             *     for each row x in X
             *         s = s + (x ⋅ r) x
             *     λ = rTs // λ is the eigenvalue
             *     error = |λ ⋅ r − s|
             *     r = s / norm(s)
             *     exit if error < tolerance
             * return λ, r
             */

            var r = new Vector3(
                0.26726124191242438468455348087975f,
                0.53452248382484876936910696175951f,
                0.80178372573727315405366044263926f);

            for (var c = 0; c < 3; c++)
            {
                var s = Vector3.Zero;
                for (var i = 0; i < count; i++)
                {
                    var x = X[i];
                    s += Vector3.Dot(x, r) * x;
                }

                var λ = Vector3.Dot(r, s);
                var error = Vector3.Distance(λ * r, s);
                r = Vector3.Normalize(s);
                if (error < 0.1)
                {
                    break;
                }
            }

            Vector3ArrayPool.Return(X);

            return new Direction(in r);
        }

        private class MappedReadOnlyList<TSource, TResult> : IReadOnlyList<TResult>
        {
            private readonly IReadOnlyList<TSource> _source;
            private readonly Func<TSource, TResult> _func;

            public MappedReadOnlyList(IReadOnlyList<TSource> source, Func<TSource, TResult> func)
            {
                _source = source;
                _func = func;
            }

            public TResult this[int index] => _func(_source[index]);

            public int Count => _source.Count;

            public IEnumerator<TResult> GetEnumerator() => _source.Select(_func).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static readonly ArrayPool<(float dotProduct, int index)> SortArrayPool = ArrayPool<(float, int)>.Shared;

        private static NodeState Partition(LeafState state, int maxLeafSize)
        {
            var existingPointValues = state.PointValues;
            var count = existingPointValues.Count; // Should be equal to MaxLeafSize

            // Calculate normal of partitioning plane
            var normal = PrincipalComponent(
                new MappedReadOnlyList<PointValuePair, Vector3>(existingPointValues, pv => pv.Point));

            // Calculate median point sorted by normal direction
            var sortArray = SortArrayPool.Rent(count);

            for (var i = 0; i < count; i++)
            {
                sortArray[i] = (Vector3.Dot(normal.Value, existingPointValues.ItemRef(i).Point), i);
            }

            Array.Sort(sortArray, (dpi1, dpi2) => dpi1.dotProduct.CompareTo(dpi2.dotProduct));

            var median = existingPointValues.ItemRef(sortArray[count >> 1].index).Point;

            SortArrayPool.Return(sortArray);

            // Calculate partitioning plane
            var distance = -Vector3.Dot(median, normal.Value);
            var partition = new Plane(normal, distance);

            // Ready subnodes
            var negative = new PointValueFieldBuilder(maxLeafSize);
            var positive = new PointValueFieldBuilder(maxLeafSize);

            // Partitioning is done, sort of.
            // Adding existing values will be done concurrently with other threads.
            return new NodeState(partition, negative, positive);
        }

        private static void AddToNode(NodeState nodeState, in PointValuePair pointValue)
        {
            var (partition, negativeBuilder, positiveBuilder) = nodeState;

            if (Vector3.Dot(partition.Normal.Value, pointValue.Point) + partition.Distance >= 0f)
            {
                positiveBuilder.Add(in pointValue);
            }
            else
            {
                negativeBuilder.Add(in pointValue);
            }
        }

        private void AddToLeaf(in PointValuePair pointValue)
        {
            Debug.Assert(_lock.IsWriteLockHeld);
            var leafState = _state as LeafState ?? throw new InvalidOperationException();
            var pointValues = leafState.PointValues;

            if (pointValues.Count < MaxLeafSize)
            {
                // Add point value pair to leaf
                pointValues.Add(pointValue);
                _lock.ExitWriteLock();
            }
            else
            {
                NodeState nodeState;

                // Create partitioning plane and subnodes
                _state = nodeState = Partition(leafState, MaxLeafSize);

                _lock.ExitWriteLock();

                // Add existing point value pairs to subnodes
                foreach (var pvp in pointValues)
                {
                    AddToNode(nodeState, in pvp);
                }

                // Add new point value pair to subnodes
                AddToNode(nodeState, in pointValue);
            }
        }

        /// <summary>
        /// Add point value pair to builder.
        /// </summary>
        /// <param name="pointValue">Point value pair to add.</param>
        /// <remarks>
        /// This builder holds up to <see cref="MaxLeafSize"/> number of point value pairs.
        /// Adding more will cause it to partition into sub-builders ready to hold more point value pairs.
        /// </remarks>
        public void Add(in PointValuePair pointValue)
        {
            switch (_state)
            {
                case LeafState:
                    _lock.EnterWriteLock();
                    switch (_state)
                    {
                        case LeafState:
                            AddToLeaf(in pointValue);
                            Debug.Assert(!_lock.IsWriteLockHeld);
                            return;
                        case NodeState nodeState:   // Handle race condition
                            _lock.ExitWriteLock();
                            AddToNode(nodeState, in pointValue);
                            return;
                        case null:
                            throw new ObjectDisposedException(typeof(PointValueFieldBuilder).Name);
                        default:
                            throw new InvalidOperationException();
                    }
                case NodeState nodeState:
                    AddToNode(nodeState, in pointValue);
                    return;
                case null:
                    throw new ObjectDisposedException(typeof(PointValueFieldBuilder).Name);
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Return an immutable field of point value pairs that contains the current contents of this
        /// <see cref="PointValueFieldBuilder"/>.
        /// </summary>
        public PointValueFieldBase ToImmutable()
        {
            _lock.EnterReadLock();

            PointValueFieldBase result = _state switch
            {
                LeafState leafState => new PointValueFieldLeaf(leafState.PointValues.ToImmutable()),
                NodeState nodeState => new PointValueFieldNode(
                    nodeState.Partition,
                    nodeState.NegativeBuilder.ToImmutable(),
                    nodeState.PositiveBuilder.ToImmutable()),
                null => throw new ObjectDisposedException(typeof(PointValueFieldBuilder).Name),
                _ => throw new InvalidOperationException()
            };

            _lock.ExitReadLock();

            return result;
        }

        public void Dispose()
        {
            if (_state is null)
            {
                throw new ObjectDisposedException(typeof(PointValueFieldBuilder).Name);
            }

            _lock.EnterWriteLock();

            (_state as NodeState)?.Dispose();

            _state = null;

            _lock.ExitWriteLock();

            _lock.Dispose();
        }
    }
}
