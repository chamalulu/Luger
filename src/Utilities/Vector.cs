using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Luger.Utilities
{
    public interface IVector<out T> : IEquatable<IVector<T>>, IEnumerable<T>
    {
        int Dimensions { get; }
        T this[int dimension] { get; }
    }

    public abstract class Vector<T> : IVector<T>
    {
        public abstract int Dimensions { get; }

        public abstract T this[int dimension] { get; }

        public virtual bool Equals(IVector<T> other) => this.SequenceEqual(other);

        public virtual IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Dimensions; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static Vector<T> operator +(Vector<T> x) => x;
    }

    public abstract class Vector<T, TFieldStrategy> : Vector<T> where TFieldStrategy : IFieldStrategy<T>
    {
        protected static readonly IFieldStrategy<T> FS = default(TFieldStrategy);

        public override bool Equals(IVector<T> other) => this.SequenceEqual(other, FS);

        public static Vector<T, TFieldStrategy> operator -(Vector<T, TFieldStrategy> x) =>
            new MemoryVector<T, TFieldStrategy>(x.Select(FS.AddInv));

        private static IEnumerable<T> ZipSameLength(IVector<T> x, IVector<T> y, Func<T, T, T> f, string fdesc)
        {
            if (x.Dimensions != y.Dimensions)
                throw new ArithmeticException($"Dimension mismatch in vector {fdesc}.");

            return x.Zip(y, f);
        }

        public static Vector<T, TFieldStrategy> operator +(Vector<T, TFieldStrategy> x, Vector<T, TFieldStrategy> y) =>
            new MemoryVector<T, TFieldStrategy>(ZipSameLength(x, y, FS.Add, "addition"));

        public static Vector<T, TFieldStrategy> operator -(Vector<T, TFieldStrategy> x, Vector<T, TFieldStrategy> y) =>
            new MemoryVector<T, TFieldStrategy>(ZipSameLength(x, y, FS.Sub, "subtraction"));

        public static T operator *(Vector<T, TFieldStrategy> x, Vector<T, TFieldStrategy> y) =>
            ZipSameLength(x, y, FS.Mul, "multiplication").Aggregate(FS.AddId, FS.Add);
    }

    public class MemoryVector<T, TFieldStrategy> : Vector<T, TFieldStrategy>
        where TFieldStrategy : IFieldStrategy<T>
    {
        private readonly ImmutableArray<T> _values;

        public MemoryVector(IEnumerable<T> values) =>
            _values = values?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(values));

        public override int Dimensions => _values.Length;

        public override T this[int dimension] => _values[dimension];

        public override IEnumerator<T> GetEnumerator() => _values.AsEnumerable().GetEnumerator();
    }
}



