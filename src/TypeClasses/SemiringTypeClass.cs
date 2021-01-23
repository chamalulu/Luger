// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1005 // Single line comments should begin with single space
#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    //tex: Left distributivity of $\cdot$ over $+$ in $(S, +, \cdot)$ $$\forall a, b, c \in S, a \cdot (b + c) = (a \cdot b) + (a \cdot c)$$

    //tex: Right distributivity of $\cdot$ over $+$ in $(S, +, \cdot)$ $$\forall a, b, c \in S, (a + b) \cdot c = (a \cdot c) + (b \cdot c)$$

    /* Algebraic Type Classes defining semiring.
     * Much of the structure and remarks are inspired by or copied from Wikipedia (https://en.wikipedia.org/wiki/Algebraic_structure) and adjoining resources.
     * Mathematical properties such as closure, associativity and so on should be enforced by instances (implementations).
     * TODO: Introduce custom attribute and analyzer for mathematical properties. Any day now...
     */

    /// <summary>
    /// Type class of semiring.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <remarks>
    /// A semiring is a ring-like algebraic structure consisting of a set (<typeparamref name="TS"/>) together with
    ///  two binary operations, addition and multiplication, with multiplication distributing over addition.<br/>
    /// <typeparamref name="TS"/> with addition is a <see cref="ICommutativeMonoid{TS}">commutative monoid</see>.<br/>
    /// <typeparamref name="TS"/> with multiplication is a <see cref="IMonoid{TS}">monoid</see>.<br/>
    /// The additive identity is an absorbing element in multiplication.<br/>
    /// The set of natural numbers with addition and multiplication is an example of a semiring.
    /// </remarks>
    public interface ISemiring<TS>
    {
        /// <summary>
        /// Gets zero, the identity element of additive group.
        /// </summary>
        TS Zero { get; }

        /// <summary>
        /// Gets one, the identity element of multiplicative group.
        /// </summary>
        TS One { get; }

        /// <summary>
        /// Addition, the binary operation of additive group.
        /// </summary>
        /// <returns>Sum of operands.</returns>
        /// <inheritdoc cref="CommutativeBinaryOperation{TS}"/>
        TS Add(TS x, TS y);

        /// <summary>
        /// Multiplication, the binary operation of multiplicative group.
        /// </summary>
        /// <returns>Product of operands.</returns>
        /// <inheritdoc cref="NoncommutativeBinaryOperation{TS}"/>
        TS Multiply(TS left, TS right);
    }

    /// <summary>
    /// Generic semiring type class instance delegating to group type class instances.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <typeparam name="TAdditiveGroup">Additive group type class instance.</typeparam>
    /// <typeparam name="TMultiplicativeGroup">Multiplicative group type class instance.</typeparam>
    public struct Semiring<TS, TAdditiveGroup, TMultiplicativeGroup> : ISemiring<TS>
        where TAdditiveGroup : struct, ICommutativeMonoid<TS>
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
    }
}
