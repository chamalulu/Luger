// <copyright file="Vector{T,TI}.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Luger.TypeClasses;

    /// <summary>
    /// A generic vector with element type class instance.
    /// </summary>
    /// <inheritdoc/>
    public readonly struct Vector<T, TI> : IVector<T, TI>, IEquatable<Vector<T, TI>>
        where TI : struct, IRing<T>, IEq<T>, IHashable<T>
    {
        private readonly IVector<T> vector;

        private Vector(IVector<T> vector) => this.vector = vector;

        /// <inheritdoc/>
        public int Dimensions => this.vector.Dimensions;

        /// <inheritdoc/>
        public VectorOrientationEnum Orientation => this.vector.Orientation;

        /// <inheritdoc/>
        int IMatrix.Rows => this.vector.Rows;

        /// <inheritdoc/>
        int IMatrix.Columns => this.vector.Columns;

        /// <inheritdoc/>
        T IMatrix<T>.this[Index i, Index j] => this.vector[i, j];

        /// <inheritdoc/>
        IVector<T> IMatrix<T>.this[Index i, Range js] => this.vector[i, js];

        /// <inheritdoc/>
        IVector<T> IMatrix<T>.this[Range @is, Index j] => this.vector[@is, j];

        /// <inheritdoc/>
        IMatrix<T> IMatrix<T>.this[Range @is, Range js] => this.vector[@is, js];

        public static implicit operator Matrix<T, TI>(Vector<T, TI> vector) => new Matrix<T, TI>(vector);

        public static explicit operator Vector<T, TI>(Matrix<T, TI> matrix) => new Vector<T, TI>(matrix);

        public static bool operator ==(Vector<T, TI> left, Vector<T, TI> right) => left.Equals(right);

        public static bool operator !=(Vector<T, TI> left, Vector<T, TI> right) => !left.Equals(right);

        /// <summary>
        /// Returns an enumerator that enumerates the elements of the vector.
        /// </summary>
        /// <returns>Enumerator that enumerates the elements of the vector.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var k in Enumerable.Range(0, this.Dimensions))
            {
                yield return this.Orientation == VectorOrientationEnum.Row
                    ? this.matrix[0, k]
                    : this.matrix[k, 0];
            }
        }

        /// <summary>
        /// Indicates wether the current vector is equal to another vector of the same type.
        /// </summary>
        /// <returns>true if the current vector is equal to the <paramref name="other"/> vector; otherwise, false.</returns>
        /// <inheritdoc/>
        public bool Equals(Vector<T, TI> other) => this.matrix.Equals(other.matrix);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => this.matrix.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => this.matrix.GetHashCode();

        /// <inheritdoc/>
        IVector<T, TI> IMatrix<T, TI>.this[Index i, Range js] => this.matrix[i, js];

        /// <inheritdoc/>
        IVector<T, TI> IMatrix<T, TI>.this[Range @is, Index j] => this.matrix[@is, j];

        /// <inheritdoc/>
        IMatrix<T, TI> IMatrix<T, TI>.this[Range @is, Range js] => this.matrix[@is, js];

        /// <inheritdoc/>
        int IMatrix<T, TI>.Rank => this.matrix.Rank;

        /// <inheritdoc/>
        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) => ((IStructuralEquatable)this.matrix).Equals(other, comparer);

        /// <inheritdoc/>
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) => ((IStructuralEquatable)this.matrix).GetHashCode(comparer);

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    //public abstract class Vector<T> : IVector<T>, IEquatable<IVector<T>> where T : IEquatable<T>
    //{
    //    public abstract int Dimensions { get; }

    //    public int Count => Dimensions;

    //    public abstract T this[int dimension] { get; }

    //    public virtual bool Equals(IVector<T>? other)
    //        => ReferenceEquals(this, other) // same check
    //        || other is IVector<T>  // null check
    //        && Dimensions == other.Dimensions   // fast size check
    //        && this.SequenceEqual(other);   // slow content check

    //    public override bool Equals(object? obj) => obj is IVector<T> other && Equals(other);

    //    public override int GetHashCode()
    //    {
    //        static HashCode add(HashCode hc, T t)
    //        {
    //            hc.Add(t);
    //            return hc;
    //        }

    //        return this.Aggregate(new HashCode(), add).ToHashCode();
    //    }

    //    public virtual IEnumerator<T> GetEnumerator()
    //    {
    //        for (int i = 0; i < Dimensions; i++)
    //            yield return this[i];
    //    }

    //    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    //    public static Vector<T> operator +(Vector<T> u) => u;
    //}

    //public abstract class Vector<T, TRing> : Vector<T>
    //    where T : IEquatable<T>
    //    where TRing : struct, IRing<T>
    //{
    //    protected static readonly TRing RingInstance = default;

    //    public static Vector<T, TRing> operator -(Vector<T, TRing> u)
    //        => new MemoryVector<T, TRing>(u.Select(RingInstance.Negate));

    //    private static IEnumerable<T> ZipSameLength(IVector<T> u, IVector<T> v, Func<T, T, T> f, string fdesc)
    //        => u.Dimensions == v.Dimensions
    //            ? u.Zip(v, f)
    //            : throw new ArithmeticException($"Dimension mismatch in vector {fdesc}.");

    //    public static Vector<T, TRing> operator +(Vector<T, TRing> u, Vector<T, TRing> v)
    //        => new MemoryVector<T, TRing>(ZipSameLength(u, v, RingInstance.Add, "addition"));

    //    public static Vector<T, TRing> operator -(Vector<T, TRing> u, Vector<T, TRing> v)
    //        => new MemoryVector<T, TRing>(ZipSameLength(u, v, RingInstance.Subtract, "subtraction"));

    //    public static T operator *(Vector<T, TRing> u, Vector<T, TRing> v)
    //        => ZipSameLength(u, v, RingInstance.Multiply, "multiplication").Aggregate(RingInstance.One, RingInstance.Add);

    //    public static Vector<T, TRing> operator *(Vector<T, TRing> u, T t)
    //        => new MemoryVector<T, TRing>(u.Select(s => RingInstance.Multiply(s, t)));

    //    public static Vector<T, TRing> operator *(T s, Vector<T, TRing> v)
    //        => new MemoryVector<T, TRing>(v.Select(t => RingInstance.Multiply(s, t)));

    //}

    //public class MemoryVector<T, TRing> : Vector<T, TRing>
    //    where T : IEquatable<T>
    //    where TRing : struct, IRing<T>
    //{
    //    private readonly ImmutableArray<T> _values;

    //    public MemoryVector(IEnumerable<T> values) =>
    //        _values = values?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(values));

    //    public override int Dimensions => _values.Length;

    //    public override T this[int dimension] => _values[dimension];

    //    public override IEnumerator<T> GetEnumerator() => _values.AsEnumerable().GetEnumerator();
    //}

    //public static class Vector
    //{
    //    public static Vector<T> Create<T>(int dimensions, IEnumerable<T> values) where T : IEquatable<T>
    //    {
    //        var builder = ImmutableArray.CreateBuilder<T>(dimensions);
    //        builder.AddRange(values.Take(dimensions));
    //        return new Vector<T>(builder.ToImmutable());
    //    }

    //    public static Vector<T> Create<T>(params T[] values) where T : IEquatable<T>
    //        => Create(values.Length, values);

    //    public static Vector<T> Create<T>(int dimensions) where T : IEquatable<T>
    //        => Create(dimensions, Enumerable.Repeat<T>(default!, dimensions));

    //    private static TResult Zip<T, TResult>(IVector<T> u, IVector<T> v, Func<T, T, T> selector, Func<int, IEnumerable<T>, TResult> resultSelector)
    //    {
    //        u = u ?? throw new ArgumentNullException(nameof(u));
    //        v = v ?? throw new ArgumentNullException(nameof(v));

    //        if (u.Dimensions != v.Dimensions)
    //            throw new ArgumentException("Dimensions mismatch.");

    //        return resultSelector(u.Dimensions, Enumerable.Zip(u, v, selector));
    //    }

    //    public static T Zip<T>(this IVector<T> u, IVector<T> v, Func<T, T, T> binaryFunc, T seed, Func<T, T, T> aggregateFunc)
    //    {
    //        u = u ?? throw new ArgumentNullException(nameof(u));
    //        v = v ?? throw new ArgumentNullException(nameof(v));

    //        if (u.Dimensions != v.Dimensions)
    //            throw new ArgumentException("Dimensions mismatch.");

    //        return Enumerable.Zip(u, v, binaryFunc).Aggregate(seed, aggregateFunc);
    //    }

    //    public static Vector<T> Zip<T>(this IVector<T> u, IVector<T> v, Func<T, T, T> binaryFunc)
    //        where T : IEquatable<T>
    //    {
    //        u = u ?? throw new ArgumentNullException(nameof(u));
    //        v = v ?? throw new ArgumentNullException(nameof(v));

    //        if (u.Dimensions != v.Dimensions)
    //            throw new ArgumentException("Dimension mismatch.");

    //        return Create(u.Dimensions, Enumerable.Zip(u, v, binaryFunc));
    //    }
    //}

    //public readonly struct Vector<T> : IVector<T>, IEquatable<Vector<T>> where T : IEquatable<T>
    //{
    //    public static readonly Vector<T> Empty = default;

    //    private readonly ImmutableArray<T> _values;

    //    internal Vector(ImmutableArray<T> values) => _values = values;

    //    public T this[int dimension] => _values[dimension];

    //    public int Dimensions => _values.Length;

    //    public bool Equals(Vector<T> other)
    //        => _values.Length == other.Dimensions   // fast size check
    //        && _values.SequenceEqual(other);    // slow values check

    //    public override bool Equals(object? obj) => obj is Vector<T> other && Equals(other);

    //    public override int GetHashCode() => _values.GetHashCode();

    //    public ImmutableArray<T>.Enumerator GetEnumerator() => _values.GetEnumerator();

    //    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _values.AsEnumerable().GetEnumerator();

    //    IEnumerator IEnumerable.GetEnumerator() => _values.AsEnumerable().GetEnumerator();

    //    int IReadOnlyCollection<T>.Count => _values.Length;

    //    public static bool operator ==(Vector<T> left, Vector<T> right) => left.Equals(right);

    //    public static bool operator !=(Vector<T> left, Vector<T> right) => !left.Equals(right);

    //    public static Vector<T> operator +(Vector<T> u) => u;
    //}

    //public readonly struct Vector<T, TSemiring> : IVector<T>, IEquatable<Vector<T, TSemiring>>
    //    where T : IEquatable<T>
    //    where TSemiring : struct, ISemiring<T>
    //{
    //    public static readonly Vector<T, TSemiring> Empty = default;

    //    private static readonly ISemiring<T> RingInstance = default(TSemiring);

    //    private readonly Vector<T> _values;

    //    private Vector(Vector<T> values) => _values = values;

    //    public T this[int dimension] => _values[dimension];

    //    public int Dimensions => _values.Dimensions;

    //    public static implicit operator Vector<T>(Vector<T, TSemiring> v) => v._values;

    //    public static Vector<T, TSemiring> operator +(Vector<T, TSemiring> u) => u;

    //    public static Vector<T, TSemiring> operator +(Vector<T, TSemiring> u, Vector<T, TSemiring> v)
    //        => new Vector<T, TSemiring>(Vector.Zip<T>(u, v, RingInstance.Add));

    //    public static T operator *(Vector<T, TSemiring> u, Vector<T, TSemiring> v)
    //        => Vector

    //    public bool Equals(Vector<T, TSemiring> other) => _values.Equals(other);

    //    public override bool Equals(object? obj) => _values.Equals(obj);

    //    public override int GetHashCode() => _values.GetHashCode();

    //    public ImmutableArray<T>.Enumerator GetEnumerator() => _values.GetEnumerator();

    //    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _values.AsEnumerable().GetEnumerator();

    //    IEnumerator IEnumerable.GetEnumerator() => _values.AsEnumerable().GetEnumerator();

    //    int IReadOnlyCollection<T>.Count => _values.Dimensions;

    //    public static bool operator ==(Vector<T, TSemiring> left, Vector<T, TSemiring> right) => left.Equals(right);

    //    public static bool operator !=(Vector<T, TSemiring> left, Vector<T, TSemiring> right) => !left.Equals(right);
    //}

}
