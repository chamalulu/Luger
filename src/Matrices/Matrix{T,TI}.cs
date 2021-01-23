// <copyright file="Matrix{T,TI}.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using Luger.TypeClasses;
    using Luger.Utilities;

    public readonly struct Matrix<T, TI> : IMatrix<T, TI>, IEquatable<Matrix<T, TI>>
        where TI : struct, IRing<T>, IEq<T>, IHashable<T>
    {
        private readonly Matrix<T> matrix;

        internal Matrix(IFrameBuffer<T> buffer) => _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

        private readonly IFrameBuffer<T> _buffer;

        public int Rows => _buffer.Height;

        public int Columns => _buffer.Width;

        public T this[Index i, Index j]
        {
            get
            {
                _ = i.GetCheckedOffset(Rows, nameof(i));
                _ = j.GetCheckedOffset(Columns, nameof(j));

                return _buffer[j, i];
            }
        }

        public Vector<T, TI> this[Index i, Range js]
        {
            get
            {
                _ = i.GetCheckedOffset(Rows, nameof(i));
                _ = js.GetCheckedOffsetAndLength(Columns, nameof(js));

                return new Vector<T, TI>(_buffer[js, i]);
            }
        }

        public Vector<T, TI> this[Range @is, Index j]
        {
            get
            {
                _ = @is.GetCheckedOffsetAndLength(Rows, nameof(@is));
                _ = j.GetCheckedOffset(Columns, nameof(j));

                return new Vector<T, TI>(_buffer[j, @is]);
            }
        }

        public Matrix<T, TI> this[Range @is, Range js]
        {
            get
            {
                _ = @is.GetCheckedOffsetAndLength(Rows, nameof(@is));
                _ = js.GetCheckedOffsetAndLength(Columns, nameof(js));

                return new Matrix<T, TI>(_buffer[js, @is]);
            }
        }

        //
        public int Rank => throw new NotImplementedException();



        /// <summary>
        /// Initializer of <see cref="TEqualityComparer"/>
        /// </summary>
        private static IEqualityComparer InitTEqualityComparer()
        {
            static bool IsEnumOrEquatable(Type type) => type.IsEnum || typeof(IEquatable<>).MakeGenericType(type).IsAssignableFrom(type);

            static bool IsNullable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

            return IsEnumOrEquatable(typeof(T)) || IsNullable(typeof(T)) && IsEnumOrEquatable(typeof(T).GetGenericArguments()[0])
                ? EqualityComparer<T>.Default
                : StructuralComparisons.StructuralEqualityComparer;
        }

        /// <summary>
        /// The frameworks <see cref="IEqualityComparer"/> best fitted for use with comparing equality of <typeparamref name="T"/>
        /// </summary>
        private static readonly IEqualityComparer TEqualityComparer = InitTEqualityComparer();

        public bool Equals(Matrix<T> other) => _buffer.Equals(other._buffer, TEqualityComparer);
    }

    // public abstract class Matrix<T> : IMatrix<T, Vector<T>>, IEquatable<IMatrix<T, Vector<T>>> where T : IEquatable<T>
    // {
    //    public abstract int RowDimensions { get; }
    //    public abstract int ColumnDimensions { get; }

    // public abstract T this[int i, int j] { get; }

    // protected class RowVector : Vector<T>
    //    {
    //        protected Matrix<T> Matrix { get; }
    //        protected int Row { get; }

    // public RowVector(Matrix<T> matrix, int row)
    //        {
    //            Matrix = matrix;
    //            Row = row;
    //        }

    // public override int Dimensions => Matrix.ColumnDimensions;

    // public override T this[int index] => Matrix[Row, index];
    //    }

    // protected class ColumnVector : Vector<T>
    //    {
    //        protected Matrix<T> Matrix { get; }
    //        protected int Column { get; }

    // public ColumnVector(Matrix<T> matrix, int column)
    //        {
    //            Matrix = matrix;
    //            Column = column;
    //        }

    // public override int Dimensions => Matrix.RowDimensions;

    // public override T this[int index] => Matrix[index, Column];
    //    }

    // protected class VectorList<TVector> : IReadOnlyList<TVector>
    //        where TVector : Vector<T>
    //    {
    //        private readonly Func<int, TVector> _vectorFactory;

    // public int Count { get; }

    // public VectorList(int count, Func<int, TVector> vectorFactory)
    //        {
    //            Count = count;
    //            _vectorFactory = vectorFactory ?? throw new ArgumentNullException(nameof(vectorFactory));
    //        }

    // public TVector this[int index] => _vectorFactory(index);

    // public virtual IEnumerator<TVector> GetEnumerator()
    //        {
    //            for (int i = 0; i < Count; i++)
    //                yield return this[i];
    //        }

    // IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    //    }

    // public virtual IReadOnlyList<Vector<T>> Rows =>
    //        new VectorList<Vector<T>>(RowDimensions, row => new RowVector(this, row));

    // public virtual IReadOnlyList<Vector<T>> Columns =>
    //        new VectorList<Vector<T>>(ColumnDimensions, column => new ColumnVector(this, column));

    // public bool Equals(IMatrix<T, Vector<T>>? other)
    //        => ReferenceEquals(this, other) // same check
    //        || other is IMatrix<T, Vector<T>>   // null check
    //        && RowDimensions == other.RowDimensions // fast size check
    //        && ColumnDimensions == other.ColumnDimensions   // fast size check
    //        && Rows.SequenceEqual(other.Rows);  // slow content check

    // public override bool Equals(object? obj) => obj is IMatrix<T, Vector<T>> other && Equals(other);

    // public override int GetHashCode()
    //    {
    //        static HashCode add(HashCode hc, Vector<T> v)
    //        {
    //            hc.Add(v);
    //            return hc;
    //        }

    // return Rows.Aggregate(new HashCode(), add).ToHashCode();
    //    }

    // public static Matrix<T> operator +(Matrix<T> x) => x;
    // }

    // public abstract class Matrix<T, TRing> : Matrix<T>, IMatrix<T, Vector<T, TRing>>
    //    where T : IEquatable<T>
    //    where TRing : struct, IRing<T>
    // {
    //    private readonly static IRing<T> RingInstance = default(TRing);

    // protected new class RowVector : Vector<T, TRing>
    //    {
    //        protected Matrix<T, TRing> Matrix { get; }
    //        protected int Row { get; }

    // public RowVector(Matrix<T, TRing> matrix, int row)
    //        {
    //            Matrix = matrix;
    //            Row = row;
    //        }

    // public override int Dimensions => Matrix.ColumnDimensions;

    // public override T this[int index] => Matrix[Row, index];
    //    }

    // protected new class ColumnVector : Vector<T, TRing>
    //    {
    //        protected Matrix<T, TRing> Matrix { get; }
    //        protected int Column { get; }

    // public ColumnVector(Matrix<T, TRing> matrix, int column)
    //        {
    //            Matrix = matrix;
    //            Column = column;
    //        }

    // public override int Dimensions => Matrix.RowDimensions;

    // public override T this[int index] => Matrix[index, Column];
    //    }

    // public new IReadOnlyList<Vector<T, TRing>> Rows =>
    //        new VectorList<Vector<T, TRing>>(RowDimensions, row => new RowVector(this, row));

    // public new IReadOnlyList<Vector<T, TRing>> Columns =>
    //        new VectorList<Vector<T, TRing>>(ColumnDimensions, column => new ColumnVector(this, column));

    // public static Matrix<T, TRing> operator -(Matrix<T, TRing> x)
    //    {
    //        x = x ?? throw new ArgumentNullException(nameof(x));

    // var rows = x.RowDimensions;
    //        var columns = x.ColumnDimensions;

    // var values = from i in Enumerable.Range(0, rows)
    //                     from j in Enumerable.Range(0, columns)
    //                     select RingInstance.Negate(x[i, j]);

    // return new MemoryMatrix<T, TRing>(x.RowDimensions, x.ColumnDimensions, values);
    //    }

    // public static Matrix<T, TRing> operator +(Matrix<T, TRing> x, Matrix<T, TRing> y)
    //    {

    // int rows = x.Rows.Count;
    //        int columns = x.Columns.Count;

    // if (rows != y.Rows.Count || columns != y.Columns.Count)
    //            throw new InvalidOperationException();

    // var values = new T[rows, columns];

    // for (int i = 0; i < rows; i++)
    //            for (int j = 0; j < columns; j++)
    //                values[i, j] = RingInstance.Add(x[i, j], y[i, j]);

    // return new MemoryMatrix<T, TRing>(rows, columns, values);
    //    }

    // public static Matrix<T, TRing> operator -(Matrix<T, TRing> x, Matrix<T, TRing> y)
    //    {
    //        int rows = x.Rows.Count;
    //        int columns = x.Columns.Count;

    // if (rows != y.Rows.Count || columns != y.Columns.Count)
    //            throw new InvalidOperationException();

    // var values = new T[rows, columns];

    // for (int i = 0; i < rows; i++)
    //            for (int j = 0; j < columns; j++)
    //                values[i, j] = RingInstance.Sub(x[i, j], y[i, j]);

    // return new Matrix<T, TRing>(values);
    //    }

    // public static Matrix<T, TRing> operator *(Matrix<T, TRing> x, Matrix<T, TRing> y)
    //    {
    //        int rows = x.Rows.Count;
    //        int columns = y.Columns.Count;

    // if (x.Columns.Count != y.Rows.Count)
    //            throw new ArithmeticException("Inner dimensions missmatch in matrix multiplication.");

    // var values = new T[rows, columns];

    // for (int i = 0; i < rows; i++)
    //            for (int j = 0; j < columns; j++)
    //                values[i, j] = x.Rows[i] * y.Columns[j];

    // return new Matrix<T, TRing>(values);
    //    }

    // public static bool operator ==(Matrix<T, TRing> left, Matrix<T, TRing> right)
    //    {
    //        int rows = left.Rows.Count;
    //        int columns = left.Columns.Count;

    // if (rows != right.Rows.Count || columns != right.Columns.Count)
    //            return false;

    // for (int i = 0; i < rows; i++)
    //            for (int j = 0; j < columns; j++)
    //                if (!RingInstance.Equals(left[i, j], right[i, j]))
    //                    return false;

    // return true;
    //    }

    // public static bool operator !=(Matrix<T, TRing> left, Matrix<T, TRing> right) =>
    //        !(left == right);
    // }

    // public class SquareMatrix<T, TFieldStrategy> : Matrix<T, TFieldStrategy>, ISquareMatrix<T, Vector<T, TFieldStrategy>>
    //    where TFieldStrategy : IFieldStrategy<T>
    // {
    //    private static IFieldStrategy<T> FS = default(TFieldStrategy);

    // public SquareMatrix(T[,] values) : base(values)
    //    {
    //        if (values.GetLength(0) == values.GetLength(1))
    //            throw new ArgumentOutOfRangeException(nameof(values));
    //    }

    // public int Order => _values.GetLength(0);

    // private T Minor(int row, int column)
    //    {
    //        int order = Order;

    // if (row < 0 || row >= order)
    //            throw new ArgumentOutOfRangeException(nameof(row));

    // if (column < 0 || column >= order)
    //            throw new ArgumentOutOfRangeException(nameof(column));

    // if (order == 1)
    //            return FS.MulId;

    // var values = new T[order - 1, order - 1];

    // for (int i = 0; i < row; i++)
    //        {
    //            for (int j = 0; j < column; j++)
    //                values[i, j] = this[i, j];

    // for (int j = column + 1; j < order; j++)
    //                values[i, j - 1] = this[i, j];
    //        }

    // for (int i = row + 1; i < order; i++)
    //        {
    //            for (int j = 0; j < column; j++)
    //                values[i - 1, j] = this[i, j];

    // for (int j = column + 1; j < order; j++)
    //                values[i - 1, j - 1] = this[i, j];
    //        }

    // }

    // public T Determinant
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    // public SquareMatrix<T, TFieldStrategy> Inverse
    //    {
    //        get
    //        {
    //            if (FS.Equals(Determinant, FS.AddId))
    //                throw new InvalidOperationException();

    // throw new NotImplementedException();
    //        }
    //    }

    // ISquareMatrix<T, Vector<T, TFieldStrategy>> ISquareMatrix<T, Vector<T, TFieldStrategy>>.Inverse => Inverse;

    // public SquareMatrix<T, TFieldStrategy> Pow(uint n)
    //    {
    //        if (n == 0)
    //            return IdMatrix<T, TFieldStrategy>.Default; // a^0 = id

    // if ((n & 1) == 1)
    //            return this * Pow(n - 1);   // a^(2n+1) = a * a^2n

    // return (this * this).Pow(n >> 1);   // a^2n = (a^2)^n
    //    }

    // public SquareMatrix<T, TFieldStrategy> Pow(int n) =>
    //        (n < 0 ? Inverse : this).Pow(IntExt.Abs(n));

    // ISquareMatrix<T, Vector<T, TFieldStrategy>> ISquareMatrix<T, Vector<T, TFieldStrategy>>.Pow(int n) => Pow(n);

    // public static SquareMatrix<T, TFieldStrategy> operator *(SquareMatrix<T, TFieldStrategy> x, SquareMatrix<T, TFieldStrategy> y)
    //    {
    //        int order = x.Order;

    // if (order != y.Order)
    //            throw new ArithmeticException("Order missmatch in square matrix multiplication.");

    // var values = new T[order, order];

    // for (int i = 0; i < order; i++)
    //            for (int j = 0; j < order; j++)
    //                values[i, j] = x.Rows[i] * y.Columns[j];

    // return new SquareMatrix<T, TFieldStrategy>(values);
    //    }
    // }

    // public class MemoryMatrix<T, TRing> : Matrix<T, TRing>
    //    where T : IEquatable<T>
    //    where TRing : struct, IRing<T>
    // {
    //    private readonly int _rows, _columns;
    //    // Buffer layout is row first like a framebuffer.
    //    private readonly ImmutableArray<T> _values;

    // public MemoryMatrix(int rows, int columns, IEnumerable<T> values)
    //    {
    //        _rows = rows;
    //        _columns = columns;

    // // Use builder for up front memory allocation
    //        var builder = ImmutableArray.CreateBuilder<T>(rows * columns);
    //        builder.AddRange(values.Take(rows * columns));
    //        _values = builder.ToImmutable();
    //    }

    // public override T this[int i, int j] => _values[i * _columns + j];

    // public override int RowDimensions => _rows;

    // public override int ColumnDimensions => _columns;
    // }

    // public class DoubleMatrix : Matrix<double, DoubleFieldStrategy>
    // {
    //    public DoubleMatrix(double[,] values) : base(values) { }
    // }

    // public class DoubleBlockMatrix : Matrix<DoubleMatrix, MatrixFieldStrategy<DoubleMatrix>>, IMatrix<double>
    // {
    //    public DoubleBlockMatrix(DoubleMatrix[,] values) : base(values) { }

    // }

    // public class ZeroMatrix<T, TFieldStrategy> : Matrix<T, TFieldStrategy> where TFieldStrategy : IFieldStrategy<T>
    // {
    //    public static ZeroMatrix<T, TFieldStrategy> Default = new ZeroMatrix<T, TFieldStrategy>();

    // private ZeroMatrix() : base(new T[0, 0]) { }

    // public new T this[int i, int j] => default(TFieldStrategy).AddId;

    // public new IReadOnlyList<IVector<T>> Rows => throw new InvalidOperationException();

    // public new IReadOnlyList<IVector<T>> Columns => throw new InvalidOperationException();

    // public static Matrix<T, TFieldStrategy> operator +(ZeroMatrix<T, TFieldStrategy> zero) =>
    //        zero;

    // public static Matrix<T, TFieldStrategy> operator -(ZeroMatrix<T, TFieldStrategy> zero) =>
    //        zero;

    // public static Matrix<T, TFieldStrategy> operator +(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
    //        left;

    // public static Matrix<T, TFieldStrategy> operator -(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
    //        left;

    // public static Matrix<T, TFieldStrategy> operator *(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
    //        zero;

    // public static Matrix<T, TFieldStrategy> operator /(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
    //        throw new DivideByZeroException();
    // }

    // public class IdMatrix<T, TFieldStrategy> : SquareMatrix<T, TFieldStrategy> where TFieldStrategy : IFieldStrategy<T>
    // {
    //    public static IdMatrix<T, TFieldStrategy> Default = new IdMatrix<T, TFieldStrategy>();
    //    private static readonly T addId = default(TFieldStrategy).AddId;
    //    private static readonly T mulId = default(TFieldStrategy).MulId;

    // private IdMatrix() : base(new T[0, 0]) { }

    // public new T this[int i, int j] => i == j ? mulId : addId;

    // public new IReadOnlyList<IVector<T>> Rows => throw new InvalidOperationException();

    // public new IReadOnlyList<IVector<T>> Columns => throw new InvalidOperationException();

    // public static SquareMatrix<T, TFieldStrategy> operator +(IdMatrix<T, TFieldStrategy> id) =>
    //        id;

    // public static SquareMatrix<T, TFieldStrategy> operator -(IdMatrix<T, TFieldStrategy> id) =>
    //        NegIdMatrix<T, TFieldStrategy>.Default;

    // public static SquareMatrix<T, TFieldStrategy> operator +(SquareMatrix<T, TFieldStrategy> left, IdMatrix<T, TFieldStrategy> id)
    //    {
    //        int rows = left.Rows.Count;
    //        int columns = left.Columns.Count;
    //        var values = new T[rows, columns];

    // var fs = default(TFieldStrategy);

    // for (int i = 0; i < rows; i++)
    //            for (int j = 0; j < columns; j++)
    //                values[i, j] = fs.Add(left[i, j], id[i, j]);

    // return new SquareMatrix<T, TFieldStrategy>(values);
    //    }

    // public static Matrix<T, TFieldStrategy> operator -(Matrix<T, TFieldStrategy> left, IdMatrix<T, TFieldStrategy> id) =>
    //        left + -id;

    // public static Matrix<T, TFieldStrategy> operator *(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
    //        zero;

    // public static Matrix<T, TFieldStrategy> operator /(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
    //        throw new DivideByZeroException();
    // }

    // public class DoubleMatrixOld : Matrix<double>
    // {
    //    protected readonly double[,] _values;

    // private DoubleMatrixOld(double[,] values) : base(values.GetLength(0), values.GetLength(1)) => _values = values;

    // public DoubleMatrixOld(int rows, int columns) : base(rows, columns) => _values = new double[rows, columns];

    // private abstract class DoubleVector : Vector<double>
    //    {
    //        public DoubleVector(int dimensions) : base(dimensions)
    //        {
    //        }
    //    }

    // private new class ColumnVector : DoubleVector
    //    {
    //        public ColumnVector(int dimensions) : base(dimensions) { }

    // public override double this[int dimension] => throw new NotImplementedException();
    //    }

    // private new class RowVectorList : Matrix<double>.RowVectorList
    //    {
    //        public RowVectorList(DoubleMatrix matrix) : base(matrix) { }

    // public override Vector<double> this[int index] => new RowVector(base._matrix, index);
    //    }

    // private new class ColumnVectorList : Matrix<double>.ColumnVectorList, IReadOnlyList<DoubleVector>
    //    {
    //        public ColumnVectorList(DoubleMatrix matrix) : base(matrix) { }

    // public override DoubleVector this[int index] => new ColumnVector(base._matrix, index);
    //    }

    // public override IReadOnlyList<DoubleVector> Rows => new RowVectorList(this);

    // public override IReadOnlyList<DoubleVector> Columns => new ColumnVectorList(this);

    // public override double this[int i, int j] => _values[i, j];

    // public static DoubleMatrix operator *(DoubleMatrix left, DoubleMatrix right)
    //    {
    //        if (left._columns != right._rows)
    //            throw new ArithmeticException("Inner dimensions missmatch in matrix multiplication.");

    // var product = new double[left._rows, right._columns];

    // for (int i = 0; i < left._rows; i++)
    //            for (int j = 0; j < right._columns; j++)
    //                product[i, j] = left.Rows[i] * right.Columns[j];

    // return new DoubleMatrix(product);
    //    }
    // }

    // public class DoubleBlockMatrix : M

    // private DoubleMatrix CreateBlock(uint bi, uint bj)
    //     {
    //         uint rows = (bi + 1) * BlockOrder >= _rows
    //             ? (_rows - 1) % BlockOrder + 1
    //             : BlockOrder;
    //         uint columns = (bj + 1) * BlockOrder >= _columns
    //             ? (_columns - 1) % BlockOrder + 1
    //             : BlockOrder;

    // return new DoubleMatrix(rows, columns);
    //     }

    // public DoubleMatrix(uint rows, uint columns)
    //     {
    //         _rows = rows;
    //         _columns = columns;

    // if (rows > BlockOrder || columns > BlockOrder)
    //         {
    //             uint blockRows = (rows - 1) / BlockOrder + 1;
    //             uint blockColumns = (columns - 1) / BlockOrder + 1;

    // _blocks = new DoubleMatrix[blockRows, blockColumns];
    //             for (uint bi = 0; bi < blockRows; bi++)
    //                 for (uint bj = 0; bj < blockColumns; bj++)
    //                     _blocks[bi, bj] = CreateBlock(bi, bj);
    //         }
    //         else
    //             _values = new double[rows, columns];
    //     }

    // public double this[uint i, uint j] =>
    //         _values?[i, j] ?? _blocks[i / BlockOrder, j / BlockOrder][i % BlockOrder, j % BlockOrder];

    // protected abstract class Vector : IEnumerable<double>
    //     {
    //         protected readonly DoubleMatrix _matrix;

    // public Vector(DoubleMatrix matrix) => _matrix = matrix;

    // public abstract uint Dimension { get; }

    // public abstract double this[uint i] { get; }

    // public static double operator *(Vector u, Vector v)
    //         {
    //             if (u.Dimension != v.Dimension)
    //                 throw new ArithmeticException("Dimensions missmatch in dot product vector multiplication.");

    // // return u.Zip(v, (x, y) => x * y).Aggregate(0d, (a, p) => a + p);
    //             double product = 0;
    //             for (uint i = 0; i < u.Dimension; i++)
    //                 product += u[i] * v[i];

    // return product;
    //         }

    // public abstract IEnumerator<double> GetEnumerator();

    // IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    //     }

    // protected class RowVector : Vector
    //     {
    //         private readonly uint _row;

    // public RowVector(DoubleMatrix matrix, uint row) : base(matrix) => _row = row;

    // public override double this[uint j] => _matrix[_row, j];

    // public override uint Dimension => _matrix._columns;

    // public override IEnumerator<double> GetEnumerator()
    //         {
    //             for (uint j = 0; j < Dimension; j++)
    //                 yield return this[j];
    //         }
    //     }

    // protected class ColumnVector : Vector
    //     {
    //         private readonly uint _column;

    // public ColumnVector(DoubleMatrix matrix, uint column) : base(matrix) => _column = column;

    // public override double this[uint i] => _matrix[i, _column];

    // public override uint Dimension => _matrix._rows;

    // public override IEnumerator<double> GetEnumerator()
    //         {
    //             for (uint i = 0; i < Dimension; i++)
    //                 yield return this[i];
    //         }
    //     }

    // protected abstract class VectorCollection
    //     {
    //         protected readonly DoubleMatrix _matrix;

    // protected VectorCollection(DoubleMatrix matrix) => _matrix = matrix;

    // public abstract uint Count { get; }

    // public abstract Vector this[uint i] { get; }
    //     }

    // protected class RowVectorCollection : VectorCollection
    //     {
    //         public RowVectorCollection(DoubleMatrix matrix) : base(matrix) { }

    // public override Vector this[uint i] => new RowVector(_matrix, i);

    // public override uint Count => _matrix._rows;
    //     }

    // protected class ColumnVectorCollection : VectorCollection
    //     {
    //         public ColumnVectorCollection(DoubleMatrix matrix) : base(matrix) { }

    // public override Vector this[uint j] => new ColumnVector(_matrix, j);

    // public override uint Count => _matrix._columns;
    //     }

    // protected VectorCollection Rows => new RowVectorCollection(this);

    // protected VectorCollection Columns => new ColumnVectorCollection(this);

    // // Naive matrix multiplication implementation. You need seious implementations for better performance.
    //     public static DoubleMatrix operator *(DoubleMatrix left, DoubleMatrix right)
    //     {
    //         if (left._columns != right._rows)
    //             throw new ArithmeticException("Inner dimensions missmatch in matrix multiplication.");

    // var product = new DoubleMatrix(left._rows, right._columns);

    // for (uint i = 0; i < left._rows; i++)
    //             for (uint j = 0; j < right._columns; j++)
    //                 //product[i, j] = left.Rows[i] * right.Columns[j]
    //                 ;

    // return product;
    //     }
    // }

    // public class SquareMatrix : DoubleMatrix
    // {
    //     public readonly uint Order;

    // public SquareMatrix(uint order) : base(order, order) => Order = order;

    // public static SquareMatrix Identity(uint order)
    //     {
    //         var I = new SquareMatrix(order);
    //         for (uint i = 0; i < order; i++)
    //             //I[i, i] = 1d
    //             ;

    // return I;
    //     }

    // public static SquareMatrix operator *(SquareMatrix left, SquareMatrix right)
    //     {
    //         if (left.Order != right.Order)
    //             throw new ArithmeticException("Order missmatch in square matrix multiplication.");

    // uint order = left.Order;

    // var product = new SquareMatrix(order);

    // for (uint i = 0; i < order; i++)
    //             for (uint j = 0; j < order; j++)
    //                 //product[i, j] = left.Rows[i] * right.Columns[j]
    //                 ;

    // return product;
    //     }

    // public SquareMatrix Pow(uint n) => Enumerable.Range(0, (int)n).Aggregate(Identity(Order), (acc, _) => acc * this);
    // }
}
