using System;
using System.Collections.Generic;
using System.Numerics;

namespace Luger.TypeClasses
{
    #region Int32 group and ring instances

    /// <summary>
    /// (int, +) is an abelian group. It's invertible with -x as the inverse for every x.
    /// </summary>
    /// <remarks>
    /// 0 and int.MinValue are their own inverses.
    /// </remarks>
    public struct Int32AddGroupInstance : IAbelianGroup<int>
    {
        public int Identity => 0;

        public int Inverse(int x) => unchecked(-x);

        public int Operation(int x, int y) => unchecked(x + y);
    }

    /// <summary>
    /// (int, *) is a commutative monoid. It's not invertible.
    /// </summary>
    public struct Int32MulMonoidInstance : ICommutativeMonoid<int>
    {
        public int Identity => 1;

        public int Operation(int x, int y) => unchecked(x * y);
    }

    /// <summary>
    /// (int, +, *) is a ring. It's only invertible in its additive group.
    /// </summary>
    public struct Int32AddMulRingInstance : ICommutativeRing<int>
    {
        public int Zero => 0;

        public int One => 1;

        public int Add(int x, int y) => x + y;

        public int Multiply(int x, int y) => x * y;

        public int Negate(int x) => -x;

        public int Subtract(int x, int y) => x - y;
    }

    #endregion

}
