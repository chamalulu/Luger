// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    /* Algebraic Type Classes defining rings.
     * Much of the structure and remarks are inspired or copied from Wikipedia (https://en.wikipedia.org/wiki/Algebraic_structure) and adjoining resources.
     * Mathematical properties such as closure, associativity and so on should be enforced by instances (implementations).
     * TODO: Introduce custom attribute and analyzer for mathematical properties. Any day now...
     */

    /// <summary>
    /// Type class of ring.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <remarks>
    /// A ring is an algebraic structure consisting of a set (<typeparamref name="TS"/>) together with two
    ///  binary operations, often called addition and multiplication, with multiplication distributing over addition.<br/>
    /// <typeparamref name="TS"/> with addition is an <see cref="IAbelianGroup{TS}">abelian group</see>.<br/>
    /// <typeparamref name="TS"/> with multiplication is a <see cref="IMonoid{TS}">monoid</see>.
    /// </remarks>
    public interface IRing<TS> : ISemiring<TS>
    {
        /// <summary>
        /// Negation, the inverse elements of additive group.
        /// </summary>
        /// <param name="x">Element to get additive inverse of.</param>
        /// <returns>Negation of <paramref name="x"/>.</returns>
        /// <remarks>N.b. this means negation as additive inverse which is not the same as logical negation.</remarks>
        TS Negate(TS x);

        /// <summary>
        /// Subtraction, the inverse binary operation of additive group.
        /// </summary>
        /// <param name="minuend">The element being subtracted from.</param>
        /// <param name="subtrahend">The element being subtracted.</param>
        /// <returns>Difference of <paramref name="subtrahend"/> subtracted from <paramref name="minuend"/>.</returns>
        TS Subtract(TS minuend, TS subtrahend);
    }

    /// <summary>
    /// Generic ring type class instance delegating to group type class instances.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <typeparam name="TAdditiveGroup">Additive group type class instance.</typeparam>
    /// <typeparam name="TMultiplicativeGroup">Multiplicative group type class instance.</typeparam>
    public struct Ring<TS, TAdditiveGroup, TMultiplicativeGroup> : IRing<TS>
        where TAdditiveGroup : struct, IAbelianGroup<TS>
        where TMultiplicativeGroup : struct, IMonoid<TS>
    {
        /// <inheritdoc cref="ISemiring{TS}.Zero"/>
        public TS Zero => default(TAdditiveGroup).Identity;

        /// <inheritdoc cref="ISemiring{TS}.One"/>
        public TS One => default(TMultiplicativeGroup).Identity;

        /// <inheritdoc cref="ISemiring{TS}.Add"/>
        public TS Add(TS x, TS y) => default(TAdditiveGroup).Operation(x, y);

        /// <inheritdoc cref="ISemiring{TS}.Multiply"/>
        public TS Multiply(TS left, TS right) => default(TMultiplicativeGroup).Operation(left, right);

        /// <inheritdoc cref="IRing{TS}.Negate"/>
        public TS Negate(TS x) => default(TAdditiveGroup).Inverse(x);

        /// <inheritdoc cref="IRing{TS}.Subtract"/>
        public TS Subtract(TS minuend, TS subtrahend) => this.Add(minuend, this.Negate(subtrahend));
    }
}
