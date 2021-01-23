// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    /// <summary>
    /// Type class of commutative ring.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <remarks>
    /// A commutative ring is a <see cref="IRing{TS, TAdditiveGroup, TMultiplicativeGroup}">ring</see> where multiplication is commutative.<br/>
    /// <typeparamref name="TS"/> with addition is an <see cref="IAbelianGroup{TS}">abelian group</see>.<br/>
    /// <typeparamref name="TS"/> with multiplication is a <see cref="ICommutativeMonoid{TS}">commutative monoid</see>.<br/>
    /// The set of integers with addition and multiplication is an example of a commutative ring.
    /// </remarks>
    public interface ICommutativeRing<TS> : IRing<TS>
    {
        /// <inheritdoc cref="ISemiring{TS}.Multiply"/>
        /// <inheritdoc cref="CommutativeBinaryOperation{TS}"/>
        new TS Multiply(TS x, TS y);
    }

    /// <summary>
    /// Generic commutative ring type class instance delegating to group type class instances.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <typeparam name="TAdditiveGroup">Additive group type class instance.</typeparam>
    /// <typeparam name="TMultiplicativeGroup">Multiplicative group type class instance.</typeparam>
    public struct CommutativeRing<TS, TAdditiveGroup, TMultiplicativeGroup> : ICommutativeRing<TS>
        where TAdditiveGroup : struct, IAbelianGroup<TS>
        where TMultiplicativeGroup : struct, ICommutativeMonoid<TS>
    {
        /// <inheritdoc cref="ISemiring{TS}.Zero"/>
        public TS Zero => default(TAdditiveGroup).Identity;

        /// <inheritdoc cref="ISemiring{TS}.One"/>
        public TS One => default(TMultiplicativeGroup).Identity;

        /// <inheritdoc cref="ISemiring{TS}.Add"/>
        public TS Add(TS x, TS y) => default(TAdditiveGroup).Operation(x, y);

        /// <inheritdoc cref="ICommutativeRing{TS}.Multiply"/>
        public TS Multiply(TS x, TS y) => default(TMultiplicativeGroup).Operation(x, y);

        /// <inheritdoc cref="IRing{TS}.Negate"/>
        public TS Negate(TS x) => default(TAdditiveGroup).Inverse(x);

        /// <inheritdoc cref="IRing{TS}.Subtract"/>
        public TS Subtract(TS minuend, TS subtrahend) => this.Add(minuend, this.Negate(subtrahend));
    }
}
