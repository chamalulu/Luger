using System;
using System.Collections.Generic;

namespace Luger.Utilities
{
    public interface IFieldStrategy<T> : IEqualityComparer<T>
    {
        T AddId { get; }
        T MulId { get; }
        T AddInv(T x);
        T MulInv(T x);
        T Mul(T x, T y);
        T Add(T x, T y);
        T Sub(T x, T y);
        T Div(T x, T y);
    }

    public struct DoubleFieldStrategy : IFieldStrategy<double>
    {
        public double AddId => 0;
        public double MulId => 1;
        public double AddInv(double x) => -x;
        public double MulInv(double x) => 1 / x;
        public double Add(double x, double y) => x + y;
        public double Mul(double x, double y) => x * y;
        public double Sub(double x, double y) => x - y;
        public double Div(double x, double y) => x / y;
        public bool Equals(double x, double y) => x.Equals(y);
        public int GetHashCode(double x) => x.GetHashCode();
    }

    public struct MatrixFieldStrategy<T, TFieldStrategy> : IFieldStrategy<Matrix<T, TFieldStrategy>> where TFieldStrategy : IFieldStrategy<T>
    {
        public Matrix<T, TFieldStrategy> AddId => ZeroMatrix<T, TFieldStrategy>.Default;
        public Matrix<T, TFieldStrategy> MulId => IdMatrix<T, TFieldStrategy>.Default;
        public Matrix<T, TFieldStrategy> AddInv(Matrix<T, TFieldStrategy> x) => -x;
        public Matrix<T, TFieldStrategy> MulInv(Matrix<T, TFieldStrategy> x) =>
            x is SquareMatrix<T, TFieldStrategy> sqx
                ? sqx.Pow(-1)
                : throw new InvalidOperationException();

        public Matrix<T, TFieldStrategy> Add(Matrix<T, TFieldStrategy> x, Matrix<T, TFieldStrategy> y) =>
            x + y;

        public Matrix<T, TFieldStrategy> Mul(Matrix<T, TFieldStrategy> x, Matrix<T, TFieldStrategy> y) =>
            x * y;

        public Matrix<T, TFieldStrategy> Sub(Matrix<T, TFieldStrategy> x, Matrix<T, TFieldStrategy> y) =>
            x - y;

        public Matrix<T, TFieldStrategy> Div(Matrix<T, TFieldStrategy> x, Matrix<T, TFieldStrategy> y) =>
            x * MulInv(y);

        public bool Equals(Matrix<T, TFieldStrategy> x, Matrix<T, TFieldStrategy> y) =>
            x.Equals(y);

        public int GetHashCode(Matrix<T, TFieldStrategy> obj) =>
            obj.GetHashCode();
    }
}
