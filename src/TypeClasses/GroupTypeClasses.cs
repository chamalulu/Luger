// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1005 // Single line comments should begin with single space
#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    /* Algebraic Type Classes defining Groups.
     * Much of the structure and remarks are inspired or copied from Wikipedia (https://en.wikipedia.org/wiki/Algebraic_structure) and adjoining resources.
     * Mathematical properties such as closure, associativity and so on should be enforced by instances (implementations).
     * TODO: Introduce custom attribute and analyzer for mathematical properties. Any day now...
     */

    //tex: Closure of $(S, \cdot)$ $$\forall a, b \in S, a \cdot b \in S$$

    //tex: Associativity of $(S, \cdot)$ $$\forall a, b, c \in S, (a \cdot b) \cdot c = a \cdot (b \cdot c)$$

    //tex: Identity element of $(S, \cdot)$ $$\exists e \in S, \forall a \in S, e \cdot a = a \cdot e = a$$

    //tex: Invertibility of $(S, \cdot)$ (where $e$ is the identity element) $$\forall a \in S, \exists b \in S, a \cdot b = b \cdot a = e$$

    //tex: Commutativity of $(S, \cdot)$ $$\forall a, b \in S, a \cdot b = b \cdot a$$

    /// <summary>
    /// Type class of magma.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <remarks>
    /// A magma is a basic group-like algebraic structure consisting of a set (<typeparamref name="TS"/>)
    ///  together with a single closed binary <see cref="Operation">operation</see>.
    /// </remarks>
    public interface IMagma<TS>
    {
        /// <summary>
        /// Binary operation of group.
        /// </summary>
        /// <returns>Result of operation.</returns>
        /// <inheritdoc cref="NoncommutativeBinaryOperation{TS}"/>
        TS Operation(TS left, TS right);
    }

    /// <summary>
    /// Type class of semigroup.
    /// </summary>
    /// <remarks>
    /// A semigroup is a <see cref="IMagma{TS}">magma</see> where the binary <see cref="IMagma{T}.Operation">operation</see> is associative.
    /// </remarks>
    /// <inheritdoc cref="IMagma{T}"/>
    public interface ISemigroup<TS> : IMagma<TS>
    {
    }

    /// <summary>
    /// Type class of monoid.
    /// </summary>
    /// <remarks>
    /// A monoid is a <see cref="ISemigroup{TS}">semigroup</see> together with an <see cref="Identity">identity</see> element.
    /// </remarks>
    /// <inheritdoc cref="ISemigroup{TS}"/>
    public interface IMonoid<TS> : ISemigroup<TS>
    {
        /// <summary>
        /// Gets identity element of group.
        /// </summary>
        TS Identity { get; }
    }

    /// <summary>
    /// Type class of commutative monoid.
    /// </summary>
    /// <remarks>
    /// A commutative monoid is a <see cref="IMonoid{TS}">monoid</see> where the binary <see cref="IMagma{TS}.Operation">operation </see> is commutative.
    /// </remarks>
    /// <inheritdoc cref="IMonoid{TS}"/>
    public interface ICommutativeMonoid<TS> : IMonoid<TS>
    {
        /// <inheritdoc cref="IMagma{TS}.Operation"/>
        /// <inheritdoc cref="CommutativeBinaryOperation{TS}"/>
        new TS Operation(TS x, TS y);
    }

    /// <summary>
    /// Type class of group.
    /// </summary>
    /// <remarks>
    /// A group is a <see cref="IMonoid{TS}">monoid</see> together with a unary <see cref="Inverse"/>.
    /// </remarks>
    /// <inheritdoc cref="IMonoid{TS}"/>
    public interface IGroup<TS> : IMonoid<TS>
    {
        /// <summary>
        /// Produce inverse element of <paramref name="x"/> in <typeparamref name="TS"/>.
        /// </summary>
        /// <param name="x">Element to get inverse of.</param>
        /// <returns>Inverse of <paramref name="x"/>.</returns>
        TS Inverse(TS x);
    }

    /// <summary>
    /// Type class of abelian group.
    /// </summary>
    /// <remarks>
    /// An abelian group is a <see cref="IGroup{TS}">group</see> where the binary <see cref="IMagma{TS}.Operation">operation</see> is commutative.
    /// </remarks>
    /// <inheritdoc cref="IGroup{TS}"/>
    public interface IAbelianGroup<TS> : IGroup<TS>, ICommutativeMonoid<TS>
    {
    }
}
