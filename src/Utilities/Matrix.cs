using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Luger.Utilities
{
    public class Matrix
    {
        protected readonly double[,] _values;

        public Matrix(uint rows, uint columns) => _values = new double[rows, columns];

        public double this[uint i, uint j]
        {
            get => _values[i, j];
            set => _values[i, j] = value;
        }

        protected abstract class Vector : IEnumerable<double>
        {
            protected readonly Matrix _matrix;

            public Vector(Matrix matrix) => _matrix = matrix;

            public abstract uint Dimension { get; }

            public abstract double this[uint i] { get; set; }

            public static double operator *(Vector u, Vector v)
            {
                if (u.Dimension != v.Dimension)
                    throw new ArithmeticException("Dimensions missmatch in dot product vector multiplication.");

                return u.Zip(v, (x, y) => x * y).Aggregate(0d, (a, p) => a + p);
            }

            public abstract IEnumerator<double> GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        protected class RowVector : Vector
        {
            private readonly uint _row;

            public RowVector(Matrix matrix, uint row) : base(matrix) => _row = row;

            public override double this[uint j]
            {
                get => _matrix[_row, j];
                set => _matrix[_row, j] = value;
            }

            public override uint Dimension => (uint)_matrix._values.GetLength(1);

            public override IEnumerator<double> GetEnumerator()
            {
                for (uint j = 0; j < Dimension; j++)
                    yield return this[j];
            }
        }

        protected class ColumnVector : Vector
        {
            private readonly uint _column;

            public ColumnVector(Matrix matrix, uint column) : base(matrix) => _column = column;

            public override double this[uint i]
            {
                get => _matrix[i, _column];
                set => _matrix[i, _column] = value;
            }

            public override uint Dimension => (uint)_matrix._values.GetLength(0);

            public override IEnumerator<double> GetEnumerator()
            {
                for (uint i = 0; i < Dimension; i++)
                    yield return this[i];
            }
        }

        protected abstract class VectorCollection
        {
            protected readonly Matrix _matrix;

            protected VectorCollection(Matrix matrix) => _matrix = matrix;

            public abstract uint Count { get; }

            public abstract Vector this[uint i] { get; }
        }

        protected class RowVectorCollection : VectorCollection
        {
            public RowVectorCollection(Matrix matrix) : base(matrix) { }

            public override Vector this[uint i] => new RowVector(_matrix, i);

            public override uint Count => (uint)_matrix._values.GetLength(0);
        }

        protected class ColumnVectorCollection : VectorCollection
        {
            public ColumnVectorCollection(Matrix matrix) : base(matrix) { }

            public override Vector this[uint j] => new ColumnVector(_matrix, j);

            public override uint Count => (uint)_matrix._values.GetLength(1);
        }

        protected VectorCollection Rows => new RowVectorCollection(this);

        protected VectorCollection Columns => new ColumnVectorCollection(this);

        // Naive matrix multiplication implementation. You need seious implementations for better performance.
        public static Matrix operator *(Matrix left, Matrix right)
        {
            if (left.Columns.Count != right.Rows.Count)
                throw new ArithmeticException("Inner dimensions missmatch in matrix multiplication.");

            var product = new Matrix(left.Rows.Count, right.Columns.Count);

            for (uint i = 0; i < left.Rows.Count; i++)
                for (uint j = 0; j < right.Columns.Count; j++)
                    product[i, j] = left.Rows[i] * right.Columns[j];

            return product;
        }
    }

    public class SquareMatrix : Matrix
    {
        public readonly uint Order;

        public SquareMatrix(uint order) : base(order, order) => Order = order;

        public static SquareMatrix Identity(uint order)
        {
            var I = new SquareMatrix(order);
            for (uint i = 0; i < order; i++)
                I[i, i] = 1d;

            return I;
        }

        public static SquareMatrix operator *(SquareMatrix left, SquareMatrix right)
        {
            if (left.Order != right.Order)
                throw new ArithmeticException("Order missmatch in square matrix multiplication.");

            uint order = left.Order;

            var product = new SquareMatrix(order);

            for (uint i = 0; i < order; i++)
                for (uint j = 0; j < order; j++)
                    product[i, j] = left.Rows[i] * right.Columns[j];

            return product;
        }

        public SquareMatrix Pow(uint n) => Enumerable.Range(0, (int)n).Aggregate(Identity(Order), (acc, _) => acc * this);
    }
}



