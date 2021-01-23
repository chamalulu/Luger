// <copyright file="Vector.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers

namespace Luger.Matrices
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Luger.TypeClasses;

    public readonly struct SingletonVector<T, TI> : IVector<T, TI>, ISquareMatrix<T, TI>, IEquatable<SingletonVector<T, TI>>
        where TI : struct, IField<T>, IEq<T>, IHashable<T>
    {
        #region Constructors

        private SingletonVector(SingletonMatrix<T, TI> matrix) => _matrix = matrix;

        #endregion

        #region Fields

        private readonly SingletonMatrix<T, TI> _matrix;

        #endregion

        #region Properties

        public T this[Index i]
            => i.GetOffset(1) == 0
                ? _matrix
                : throw new ArgumentOutOfRangeException(nameof(i));

        public int Dimensions => 1;

        public VectorOrientationEnum Orientation => VectorOrientationEnum.None;

        #endregion

        #region Methods

        public IEnumerator<T> GetEnumerator()
        {
            yield return _matrix;
        }

        public bool Equals(SingletonVector<T, TI> other) => _matrix.Equals(other._matrix);

        public override bool Equals(object? obj) => _matrix.Equals(obj);

        public override int GetHashCode() => _matrix.GetHashCode();

        #endregion

        #region Operators

        public static bool operator ==(SingletonVector<T,TI> left, SingletonVector<T, TI> right) => left.Equals(right);

        public static bool operator !=(SingletonVector<T, TI> left, SingletonVector<T, TI> right) => !left.Equals(right);

        public static implicit operator T(SingletonVector<T, TI> vector) => vector._matrix;

        public static implicit operator SingletonVector<T, TI>(T value) => new SingletonVector<T, TI>(value);

        public static implicit operator SingletonMatrix<T, TI>(SingletonVector<T, TI> vector) => vector._matrix;

        public static implicit operator SingletonVector<T, TI>(SingletonMatrix<T, TI> matrix) => new SingletonVector<T, TI>(matrix);

        #endregion

        #region Explicit Interface Implementations

        T IMatrix<T>.this[Index i, Index j] => _matrix[i, j];

        IVector<T> IMatrix<T>.this[Index i, Range js] => _matrix[i, js];

        IVector<T> IMatrix<T>.this[Range @is, Index j] => _matrix[@is, j];

        IMatrix<T> IMatrix<T>.this[Range @is, Range js] => _matrix[@is, js];

        IVector<T, TI> IMatrix<T, TI>.this[Index i, Range js] => _matrix[i, js];

        IVector<T, TI> IMatrix<T, TI>.this[Range @is, Index j] => _matrix[@is, j];

        IMatrix<T, TI> IMatrix<T, TI>.this[Range @is, Range js] => _matrix[@is, js];

        int ISquareMatrix.Order => 1;

        int IMatrix.Rows => 1;

        int IMatrix.Columns => 1;

        int IMatrix<T, TI>.Rank => _matrix.Rank;

        T ISquareMatrix<T, TI>.Determinant => _matrix.Determinant;

        ISquareMatrix<T, TI> ISquareMatrix<T, TI>.Inverse => _matrix.Inverse;

        ISquareMatrix<T, TI> ISquareMatrix<T, TI>.Pow(int n) => _matrix.Pow(n);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer) => ((IStructuralEquatable)_matrix).Equals(other, comparer);

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) => ((IStructuralEquatable)_matrix).GetHashCode(comparer);

        #endregion
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
