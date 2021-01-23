// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    /// <summary>
    /// (double, +) is, nearly but not strictly, a(n abelian) group. It's invertible with -x as the inverse for every x. See remarks about inf and NaN.
    /// </summary>
    /// <remarks>
    /// IEEE 754 floating point has a dedicated sign bit so even though +0 and -0 are considered equal they are distinct elements and mutual inverses.
    /// IEEE 754 infinities and NaN are exceptions. They don't have inverses since they don't add to 0 with any other element.
    /// </remarks>
    public struct DoubleAddGroupInstance : IAbelianGroup<double>
    {
        public double Identity => 0;

        public double Inverse(double x) => -x;

        public double Operation(double x, double y) => x + y;
    }

    /// <summary>
    /// (double, *) is, with some imagination, a(n abelian) group. For some nicely behaving elements 1/x is exactly the inverse of x.
    /// </summary>
    /// <remarks>
    /// For many elements x * 1/x is not exactly 1. The forgiven element 0 is not the only one, but we are
    ///  being pragmatic and believe that IEEE 754 division is probably doing its best with limited resources.
    /// </remarks>
    public struct DoubleMulGroupInstance : IAbelianGroup<double>
    {
        public double Identity => 1;

        public double Inverse(double x) => 1 / x;

        public double Operation(double x, double y) => x * y;
    }

    /// <summary>
    /// (double, +, *) is (not really) a field. Exceptions to invertibility exist in both groups.
    /// See <see cref="DoubleAddGroupInstance"/> and <see cref="DoubleMulGroupInstance"/> for details.
    /// </summary>
    public struct DoubleAddMulFieldInstance : IField<double>
    {
        public double Zero => 0;

        public double One => 1;

        public double Add(double x, double y) => x + y;

        public double Multiply(double x, double y) => x * y;

        public double Negate(double x) => -x;

        public double Subtract(double x, double y) => x - y;

        public double Reciprocate(double x) => 1 / x;

        public double Divide(double x, double y) => x / y;
    }

    /// <summary>
    /// Instance of double implementing several type classes.
    /// </summary>
    public struct DoubleInstance : IEq<double>, IHashable<double>, IOrd<double>, IField<double>
    {
        public double Zero => 0;

        public double One => 1;

        public double Add(double x, double y) => x + y;

        public OrderEnum Compare(double x, double y) => (OrderEnum)x.CompareTo(y);

        public double Divide(double x, double y) => x / y;

        public bool Equals(double x, double y) => x.Equals(y);

        public int GetHashCode(double x) => x.GetHashCode();

        public int GetHashCode(int salt, double x) => salt ^ x.GetHashCode();

        public double Multiply(double x, double y) => x * y;

        public double Negate(double x) => -x;

        public double Reciprocate(double x) => 1d / x;

        public double Subtract(double x, double y) => x - y;
    }
}
