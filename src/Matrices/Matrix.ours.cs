using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Luger.Matrices
{
    public interface IMatrix<out T, out TVector> where TVector : IVector<T>
    {
        IReadOnlyList<TVector> Rows { get; }
        IReadOnlyList<TVector> Columns { get; }
        T this[int i, int j] { get; }
    }

    public interface ISquareMatrix<out T, out TVector> : IMatrix<T, TVector> where TVector : IVector<T>
    {
        int Order { get; }
        T Determinant { get; }
        ISquareMatrix<T, TVector> Inverse { get; }
        ISquareMatrix<T, TVector> Pow(int n);
    }

    /*
    A: (17x23) (3x5)
    B: (23x19) (5x7)

    A*B (17x19) (3x7)

    A: 6x5 (2x3)
    B: 5x3 (3x5)

    A*B (6x3) (2x5)

    A*B (17x19)

     */

    // public class DoubleMatrix : IMatrix<double>
    // {
    //     protected readonly double[,] _values;

    //     protected DoubleMatrix(double[,] values) => _values = values ?? throw new ArgumentNullException(nameof(values));

    //     public double this[int i, int j] => _values[i, j];

    //     protected class RowVector : IVector<double>
    //     {
    //         private readonly DoubleMatrix _matrix;
    //         private readonly int _row;

    //         public RowVector(DoubleMatrix matrix, int row)
    //         {
    //             Debug.Assert(matrix != null);
    //             Debug.Assert(row >= 0 && row < matrix._values.GetLength(0));

    //             _matrix = matrix;
    //             _row = row;
    //         }

    //         public double this[int dimension] => _matrix[_row, dimension];

    //         public int Dimensions => _matrix._values.GetLength(1);

    //         public bool Equals(IVector<double> other) => this.SequenceEqual(other);

    //         public IEnumerator<double> GetEnumerator()
    //         {
    //             int columns = Dimensions;
    //             for (int j = 0; j < columns; j++)
    //                 yield return _matrix._values[_row, j];
    //         }

    //         IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    //     }

    //     public IReadOnlyList<IVector<double>> Rows => throw new NotImplementedException();

    //     public IReadOnlyList<IVector<double>> Columns => throw new NotImplementedException();
    // }

    public class Matrix<T> : IMatrix<T, Vector<T>>
    {
        protected readonly T[,] _values;

        public Matrix(T[,] values) => _values = values ?? throw new ArgumentNullException(nameof(values));

        protected class RowVector : Vector<T>
        {
            protected readonly Matrix<T> _matrix;
            protected readonly int _row;

            public RowVector(Matrix<T> matrix, int row)
            {
                _matrix = matrix;
                _row = row;
            }

            public override int Dimensions => _matrix._values.GetLength(1);

            public override T this[int index] => _matrix[_row, index];
        }

        protected class ColumnVector : Vector<T>
        {
            protected readonly Matrix<T> _matrix;
            protected readonly int _column;

            public ColumnVector(Matrix<T> matrix, int column)
            {
                _matrix = matrix;
                _column = column;
            }

            public override int Dimensions => _matrix._values.GetLength(0);

            public override T this[int index] => _matrix[index, _column];
        }

        protected class VectorList<TVector> : IReadOnlyList<TVector>
            where TVector : Vector<T>
        {
            private readonly Func<int, TVector> _vectorFactory;

            public int Count { get; }

            public VectorList(int count, Func<int, TVector> vectorFactory)
            {
                Count = count;
                _vectorFactory = vectorFactory ?? throw new ArgumentNullException(nameof(vectorFactory));
            }

            public TVector this[int index] => _vectorFactory(index);

            public virtual IEnumerator<TVector> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return this[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public virtual IReadOnlyList<Vector<T>> Rows =>
            new VectorList<Vector<T>>(_values.GetLength(0), row => new RowVector(this, row));

        public virtual IReadOnlyList<Vector<T>> Columns =>
            new VectorList<Vector<T>>(_values.GetLength(1), column => new ColumnVector(this, column));

        public virtual T this[int i, int j] => _values[i, j];
    }

    public class Matrix<T, TFieldStrategy> : Matrix<T>, IMatrix<T, Vector<T, TFieldStrategy>>
        where TFieldStrategy : IFieldStrategy<T>
    {
        private readonly static IFieldStrategy<T> FS = default(TFieldStrategy);

        public Matrix(T[,] values) : base(values) { }

        protected new class RowVector : Vector<T, TFieldStrategy>
        {
            protected readonly Matrix<T, TFieldStrategy> _matrix;
            protected readonly int _row;

            public RowVector(Matrix<T, TFieldStrategy> matrix, int row)
            {
                _matrix = matrix;
                _row = row;
            }

            public override int Dimensions => _matrix._values.GetLength(1);

            public override T this[int index] => _matrix[_row, index];
        }

        protected new class ColumnVector : Vector<T, TFieldStrategy>
        {
            protected readonly Matrix<T, TFieldStrategy> _matrix;
            protected readonly int _column;

            public ColumnVector(Matrix<T, TFieldStrategy> matrix, int column)
            {
                _matrix = matrix;
                _column = column;
            }

            public override int Dimensions => _matrix._values.GetLength(0);

            public override T this[int index] => _matrix[index, _column];
        }

        public new IReadOnlyList<Vector<T, TFieldStrategy>> Rows =>
            new VectorList<Vector<T, TFieldStrategy>>(_values.GetLength(0), row => new RowVector(this, row));

        public new IReadOnlyList<Vector<T, TFieldStrategy>> Columns =>
            new VectorList<Vector<T, TFieldStrategy>>(_values.GetLength(1), column => new ColumnVector(this, column));

        public override bool Equals(object obj) => obj is Matrix<T, TFieldStrategy> other && this == other;

        public override int GetHashCode()
        {
            int rows = this.Rows.Count;
            int columns = this.Columns.Count;
            uint hc = (uint)rows << 19 ^ (uint)columns;

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    hc = hc << 19 ^ hc >> 13 ^ (uint)FS.GetHashCode(this[i, j]);

            return (int)hc;
        }

        public static Matrix<T, TFieldStrategy> operator +(Matrix<T, TFieldStrategy> x) => x;

        public static Matrix<T, TFieldStrategy> operator -(Matrix<T, TFieldStrategy> x)
        {
            int rows = x.Rows.Count;
            int columns = x.Columns.Count;

            var values = new T[rows, columns];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    values[i, j] = FS.AddInv(x[i, j]);

            return new Matrix<T, TFieldStrategy>(values);
        }

        public static Matrix<T, TFieldStrategy> operator +(Matrix<T, TFieldStrategy> x, Matrix<T, TFieldStrategy> y)
        {
            int rows = x.Rows.Count;
            int columns = x.Columns.Count;

            if (rows != y.Rows.Count || columns != y.Columns.Count)
                throw new InvalidOperationException();

            var values = new T[rows, columns];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    values[i, j] = FS.Add(x[i, j], y[i, j]);

            return new Matrix<T, TFieldStrategy>(values);
        }

        public static Matrix<T, TFieldStrategy> operator -(Matrix<T, TFieldStrategy> x, Matrix<T, TFieldStrategy> y)
        {
            int rows = x.Rows.Count;
            int columns = x.Columns.Count;

            if (rows != y.Rows.Count || columns != y.Columns.Count)
                throw new InvalidOperationException();

            var values = new T[rows, columns];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    values[i, j] = FS.Sub(x[i, j], y[i, j]);

            return new Matrix<T, TFieldStrategy>(values);
        }

        public static Matrix<T, TFieldStrategy> operator *(Matrix<T, TFieldStrategy> x, Matrix<T, TFieldStrategy> y)
        {
            int rows = x.Rows.Count;
            int columns = y.Columns.Count;

            if (x.Columns.Count != y.Rows.Count)
                throw new ArithmeticException("Inner dimensions missmatch in matrix multiplication.");

            var values = new T[rows, columns];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    values[i, j] = x.Rows[i] * y.Columns[j];

            return new Matrix<T, TFieldStrategy>(values);
        }

        public static bool operator ==(Matrix<T, TFieldStrategy> left, Matrix<T, TFieldStrategy> right)
        {
            int rows = left.Rows.Count;
            int columns = left.Columns.Count;

            if (rows != right.Rows.Count || columns != right.Columns.Count)
                return false;

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    if (!FS.Equals(left[i, j], right[i, j]))
                        return false;

            return true;
        }

        public static bool operator !=(Matrix<T, TFieldStrategy> left, Matrix<T, TFieldStrategy> right) =>
            !(left == right);
    }

    public class SquareMatrix<T, TFieldStrategy> : Matrix<T, TFieldStrategy>, ISquareMatrix<T, Vector<T, TFieldStrategy>>
        where TFieldStrategy : IFieldStrategy<T>
    {
        private static IFieldStrategy<T> FS = default(TFieldStrategy);

        public SquareMatrix(T[,] values) : base(values)
        {
            if (values.GetLength(0) == values.GetLength(1))
                throw new ArgumentOutOfRangeException(nameof(values));
        }

        public int Order => _values.GetLength(0);

        private T Minor(int row, int column)
        {
            int order = Order;

            if (row < 0 || row >= order)
                throw new ArgumentOutOfRangeException(nameof(row));

            if (column < 0 || column >= order)
                throw new ArgumentOutOfRangeException(nameof(column));

            if (order == 1)
                return FS.MulId;

            var values = new T[order - 1, order - 1];

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < column; j++)
                    values[i, j] = this[i, j];

                for (int j = column + 1; j < order; j++)
                    values[i, j - 1] = this[i, j];
            }

            for (int i = row + 1; i < order; i++)
            {
                for (int j = 0; j < column; j++)
                    values[i - 1, j] = this[i, j];

                for (int j = column + 1; j < order; j++)
                    values[i - 1, j - 1] = this[i, j];
            }

        }

        public T Determinant
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SquareMatrix<T, TFieldStrategy> Inverse
        {
            get
            {
                if (FS.Equals(Determinant, FS.AddId))
                    throw new InvalidOperationException();

                throw new NotImplementedException();
            }
        }

        ISquareMatrix<T, Vector<T, TFieldStrategy>> ISquareMatrix<T, Vector<T, TFieldStrategy>>.Inverse => Inverse;

        public SquareMatrix<T, TFieldStrategy> Pow(uint n)
        {
            if (n == 0)
                return IdMatrix<T, TFieldStrategy>.Default; // a^0 = id

            if ((n & 1) == 1)
                return this * Pow(n - 1);   // a^(2n+1) = a * a^2n

            return (this * this).Pow(n >> 1);   // a^2n = (a^2)^n
        }

        public SquareMatrix<T, TFieldStrategy> Pow(int n) =>
            (n < 0 ? Inverse : this).Pow(IntExt.Abs(n));

        ISquareMatrix<T, Vector<T, TFieldStrategy>> ISquareMatrix<T, Vector<T, TFieldStrategy>>.Pow(int n) => Pow(n);

        public static SquareMatrix<T, TFieldStrategy> operator *(SquareMatrix<T, TFieldStrategy> x, SquareMatrix<T, TFieldStrategy> y)
        {
            int order = x.Order;

            if (order != y.Order)
                throw new ArithmeticException("Order missmatch in square matrix multiplication.");

            var values = new T[order, order];

            for (int i = 0; i < order; i++)
                for (int j = 0; j < order; j++)
                    values[i, j] = x.Rows[i] * y.Columns[j];

            return new SquareMatrix<T, TFieldStrategy>(values);
        }
    }

    public class DoubleMatrix : Matrix<double, DoubleFieldStrategy>
    {
        public DoubleMatrix(double[,] values) : base(values) { }
    }

    public class DoubleBlockMatrix : Matrix<DoubleMatrix, MatrixFieldStrategy<DoubleMatrix>>, IMatrix<double>
    {
        public DoubleBlockMatrix(DoubleMatrix[,] values) : base(values) { }


    }

    public class ZeroMatrix<T, TFieldStrategy> : Matrix<T, TFieldStrategy> where TFieldStrategy : IFieldStrategy<T>
    {
        public static ZeroMatrix<T, TFieldStrategy> Default = new ZeroMatrix<T, TFieldStrategy>();

        private ZeroMatrix() : base(new T[0, 0]) { }

        public new T this[int i, int j] => default(TFieldStrategy).AddId;

        public new IReadOnlyList<IVector<T>> Rows => throw new InvalidOperationException();

        public new IReadOnlyList<IVector<T>> Columns => throw new InvalidOperationException();

        public static Matrix<T, TFieldStrategy> operator +(ZeroMatrix<T, TFieldStrategy> zero) =>
            zero;

        public static Matrix<T, TFieldStrategy> operator -(ZeroMatrix<T, TFieldStrategy> zero) =>
            zero;

        public static Matrix<T, TFieldStrategy> operator +(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
            left;

        public static Matrix<T, TFieldStrategy> operator -(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
            left;

        public static Matrix<T, TFieldStrategy> operator *(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
            zero;

        public static Matrix<T, TFieldStrategy> operator /(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
            throw new DivideByZeroException();
    }

    public class IdMatrix<T, TFieldStrategy> : SquareMatrix<T, TFieldStrategy> where TFieldStrategy : IFieldStrategy<T>
    {
        public static IdMatrix<T, TFieldStrategy> Default = new IdMatrix<T, TFieldStrategy>();
        private static readonly T addId = default(TFieldStrategy).AddId;
        private static readonly T mulId = default(TFieldStrategy).MulId;

        private IdMatrix() : base(new T[0, 0]) { }

        public new T this[int i, int j] => i == j ? mulId : addId;

        public new IReadOnlyList<IVector<T>> Rows => throw new InvalidOperationException();

        public new IReadOnlyList<IVector<T>> Columns => throw new InvalidOperationException();

        public static SquareMatrix<T, TFieldStrategy> operator +(IdMatrix<T, TFieldStrategy> id) =>
            id;

        public static SquareMatrix<T, TFieldStrategy> operator -(IdMatrix<T, TFieldStrategy> id) =>
            NegIdMatrix<T, TFieldStrategy>.Default;

        public static SquareMatrix<T, TFieldStrategy> operator +(SquareMatrix<T, TFieldStrategy> left, IdMatrix<T, TFieldStrategy> id)
        {
            int rows = left.Rows.Count;
            int columns = left.Columns.Count;
            var values = new T[rows, columns];

            var fs = default(TFieldStrategy);

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++)
                    values[i, j] = fs.Add(left[i, j], id[i, j]);

            return new SquareMatrix<T, TFieldStrategy>(values);
        }

        public static Matrix<T, TFieldStrategy> operator -(Matrix<T, TFieldStrategy> left, IdMatrix<T, TFieldStrategy> id) =>
            left + -id;

        public static Matrix<T, TFieldStrategy> operator *(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
            zero;

        public static Matrix<T, TFieldStrategy> operator /(Matrix<T, TFieldStrategy> left, ZeroMatrix<T, TFieldStrategy> zero) =>
            throw new DivideByZeroException();
    }




    public class DoubleMatrixOld : Matrix<double>
    {
        protected readonly double[,] _values;

        private DoubleMatrixOld(double[,] values) : base(values.GetLength(0), values.GetLength(1)) => _values = values;

        public DoubleMatrixOld(int rows, int columns) : base(rows, columns) => _values = new double[rows, columns];

        private abstract class DoubleVector : Vector<double>
        {
            public DoubleVector(int dimensions) : base(dimensions)
            {
            }
        }

        private new class ColumnVector : DoubleVector
        {
            public ColumnVector(int dimensions) : base(dimensions) { }

            public override double this[int dimension] => throw new NotImplementedException();
        }

        private new class RowVectorList : Matrix<double>.RowVectorList
        {
            public RowVectorList(DoubleMatrix matrix) : base(matrix) { }

            public override Vector<double> this[int index] => new RowVector(base._matrix, index);
        }

        private new class ColumnVectorList : Matrix<double>.ColumnVectorList, IReadOnlyList<DoubleVector>
        {
            public ColumnVectorList(DoubleMatrix matrix) : base(matrix) { }

            public override DoubleVector this[int index] => new ColumnVector(base._matrix, index);
        }

        public override IReadOnlyList<DoubleVector> Rows => new RowVectorList(this);

        public override IReadOnlyList<DoubleVector> Columns => new ColumnVectorList(this);

        public override double this[int i, int j] => _values[i, j];

        public static DoubleMatrix operator *(DoubleMatrix left, DoubleMatrix right)
        {
            if (left._columns != right._rows)
                throw new ArithmeticException("Inner dimensions missmatch in matrix multiplication.");

            var product = new double[left._rows, right._columns];

            for (int i = 0; i < left._rows; i++)
                for (int j = 0; j < right._columns; j++)
                    product[i, j] = left.Rows[i] * right.Columns[j];

            return new DoubleMatrix(product);
        }
    }

    //     public class DoubleBlockMatrix : M

    //         private DoubleMatrix CreateBlock(uint bi, uint bj)
    //     {
    //         uint rows = (bi + 1) * BlockOrder >= _rows
    //             ? (_rows - 1) % BlockOrder + 1
    //             : BlockOrder;
    //         uint columns = (bj + 1) * BlockOrder >= _columns
    //             ? (_columns - 1) % BlockOrder + 1
    //             : BlockOrder;

    //         return new DoubleMatrix(rows, columns);
    //     }

    //     public DoubleMatrix(uint rows, uint columns)
    //     {
    //         _rows = rows;
    //         _columns = columns;

    //         if (rows > BlockOrder || columns > BlockOrder)
    //         {
    //             uint blockRows = (rows - 1) / BlockOrder + 1;
    //             uint blockColumns = (columns - 1) / BlockOrder + 1;

    //             _blocks = new DoubleMatrix[blockRows, blockColumns];
    //             for (uint bi = 0; bi < blockRows; bi++)
    //                 for (uint bj = 0; bj < blockColumns; bj++)
    //                     _blocks[bi, bj] = CreateBlock(bi, bj);
    //         }
    //         else
    //             _values = new double[rows, columns];
    //     }

    //     public double this[uint i, uint j] =>
    //         _values?[i, j] ?? _blocks[i / BlockOrder, j / BlockOrder][i % BlockOrder, j % BlockOrder];

    //     protected abstract class Vector : IEnumerable<double>
    //     {
    //         protected readonly DoubleMatrix _matrix;

    //         public Vector(DoubleMatrix matrix) => _matrix = matrix;

    //         public abstract uint Dimension { get; }

    //         public abstract double this[uint i] { get; }

    //         public static double operator *(Vector u, Vector v)
    //         {
    //             if (u.Dimension != v.Dimension)
    //                 throw new ArithmeticException("Dimensions missmatch in dot product vector multiplication.");

    //             // return u.Zip(v, (x, y) => x * y).Aggregate(0d, (a, p) => a + p);
    //             double product = 0;
    //             for (uint i = 0; i < u.Dimension; i++)
    //                 product += u[i] * v[i];

    //             return product;
    //         }

    //         public abstract IEnumerator<double> GetEnumerator();

    //         IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    //     }

    //     protected class RowVector : Vector
    //     {
    //         private readonly uint _row;

    //         public RowVector(DoubleMatrix matrix, uint row) : base(matrix) => _row = row;

    //         public override double this[uint j] => _matrix[_row, j];

    //         public override uint Dimension => _matrix._columns;

    //         public override IEnumerator<double> GetEnumerator()
    //         {
    //             for (uint j = 0; j < Dimension; j++)
    //                 yield return this[j];
    //         }
    //     }

    //     protected class ColumnVector : Vector
    //     {
    //         private readonly uint _column;

    //         public ColumnVector(DoubleMatrix matrix, uint column) : base(matrix) => _column = column;

    //         public override double this[uint i] => _matrix[i, _column];

    //         public override uint Dimension => _matrix._rows;

    //         public override IEnumerator<double> GetEnumerator()
    //         {
    //             for (uint i = 0; i < Dimension; i++)
    //                 yield return this[i];
    //         }
    //     }

    //     protected abstract class VectorCollection
    //     {
    //         protected readonly DoubleMatrix _matrix;

    //         protected VectorCollection(DoubleMatrix matrix) => _matrix = matrix;

    //         public abstract uint Count { get; }

    //         public abstract Vector this[uint i] { get; }
    //     }

    //     protected class RowVectorCollection : VectorCollection
    //     {
    //         public RowVectorCollection(DoubleMatrix matrix) : base(matrix) { }

    //         public override Vector this[uint i] => new RowVector(_matrix, i);

    //         public override uint Count => _matrix._rows;
    //     }

    //     protected class ColumnVectorCollection : VectorCollection
    //     {
    //         public ColumnVectorCollection(DoubleMatrix matrix) : base(matrix) { }

    //         public override Vector this[uint j] => new ColumnVector(_matrix, j);

    //         public override uint Count => _matrix._columns;
    //     }

    //     protected VectorCollection Rows => new RowVectorCollection(this);

    //     protected VectorCollection Columns => new ColumnVectorCollection(this);

    //     // Naive matrix multiplication implementation. You need seious implementations for better performance.
    //     public static DoubleMatrix operator *(DoubleMatrix left, DoubleMatrix right)
    //     {
    //         if (left._columns != right._rows)
    //             throw new ArithmeticException("Inner dimensions missmatch in matrix multiplication.");

    //         var product = new DoubleMatrix(left._rows, right._columns);

    //         for (uint i = 0; i < left._rows; i++)
    //             for (uint j = 0; j < right._columns; j++)
    //                 //product[i, j] = left.Rows[i] * right.Columns[j]
    //                 ;

    //         return product;
    //     }
    // }

    // public class SquareMatrix : DoubleMatrix
    // {
    //     public readonly uint Order;

    //     public SquareMatrix(uint order) : base(order, order) => Order = order;

    //     public static SquareMatrix Identity(uint order)
    //     {
    //         var I = new SquareMatrix(order);
    //         for (uint i = 0; i < order; i++)
    //             //I[i, i] = 1d
    //             ;

    //         return I;
    //     }

    //     public static SquareMatrix operator *(SquareMatrix left, SquareMatrix right)
    //     {
    //         if (left.Order != right.Order)
    //             throw new ArithmeticException("Order missmatch in square matrix multiplication.");

    //         uint order = left.Order;

    //         var product = new SquareMatrix(order);

    //         for (uint i = 0; i < order; i++)
    //             for (uint j = 0; j < order; j++)
    //                 //product[i, j] = left.Rows[i] * right.Columns[j]
    //                 ;

    //         return product;
    //     }

    //     public SquareMatrix Pow(uint n) => Enumerable.Range(0, (int)n).Aggregate(Identity(Order), (acc, _) => acc * this);
    // }
}



