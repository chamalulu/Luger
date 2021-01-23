// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1623 // Property summary documentation should match accessors

namespace Luger.TypeClasses
{
    /// <summary>
    /// (bool, ∨) is a commutative monoid. It's not invertible.
    /// </summary>
    public struct BooleanOrMonoidInstance : ICommutativeMonoid<bool>
    {
        /// <summary>
        /// Gets identity element of (bool, ∨). I.e false.
        /// </summary>
        public bool Identity => false;

        /// <summary>
        /// Binary operation of (bool, ∨). I.e. Logical inclusive disjunction.
        /// </summary>
        /// <returns>Logical inclusive disjunction of operands <paramref name="x"/> and <paramref name="y"/>.</returns>
        /// <inheritdoc cref="CommutativeBinaryOperation{TS}"/>
        public bool Operation(bool x, bool y) => x | y;
    }

    /// <summary>
    /// (bool, ∧) is a commutative monoid. It's not invertible.
    /// </summary>
    public struct BooleanAndMonoidInstance : ICommutativeMonoid<bool>
    {
        /// <summary>
        /// Gets identity element of (bool, ∧). I.e. true.
        /// </summary>
        public bool Identity => true;

        /// <summary>
        /// Binary operation of (bool, ∧). I.e. Logical conjunction.
        /// </summary>
        /// <returns>Logical conjunction of operands <paramref name="x"/> and <paramref name="y"/>.</returns>
        /// <inheritdoc cref="CommutativeBinaryOperation{TS}"/>
        public bool Operation(bool x, bool y) => x & y;
    }

    /// <summary>
    /// (bool, ∨, ∧) is a (commutative) semiring. Neither operation is invertible.
    /// </summary>
    public struct BooleanOrAndSemiringInstance : ISemiring<bool>
    {
        /// <summary>
        /// Gets additive identity element of (bool, ∨, ∧). I.e. false.
        /// </summary>
        public bool Zero => false;

        /// <summary>
        /// Gets multiplicative identity element of (bool, ∨, ∧). I.e. true.
        /// </summary>
        public bool One => true;

        /// <summary>
        /// Additive binary operation of (bool, ∨, ∧). I.e. Logical inclusive disjunction.
        /// </summary>
        /// <inheritdoc cref="BooleanOrMonoidInstance.Operation"/>
        public bool Add(bool x, bool y) => x | y;

        /// <summary>
        /// Multiplicative binary operation of (bool, ∨, ∧). I.e. Logical conjunction.
        /// </summary>
        /// <inheritdoc cref="BooleanAndMonoidInstance.Operation"/>
        public bool Multiply(bool x, bool y) => x & y;

        /// <summary>
        /// Scalar multiplication.
        /// </summary>
        /// <param name="n">Scalar factor.</param>
        /// <param name="x">Boolean element.</param>
        /// <returns>
        /// True if x is true and n > 0; otherwise, false.
        /// </returns>
        public bool Multiply(uint n, bool x) => x && n > 0;

        /// <summary>
        /// Square of boolean.
        /// </summary>
        /// <param name="x">Element to square.</param>
        /// <returns>Element <paramref name="x"/>.</returns>
        public bool Square(bool x) => x;

        /// <returns>
        /// True if b is true or n = 0; otherwise, false.
        /// </returns>
        public bool Pow(uint n, bool b) => b || n == 0;
    }

    /// <summary>
    /// (bool, ⨁) is an abelian group. It's invertible with every element being its own inverse.
    /// </summary>
    public struct BooleanXorGroupInstance : IAbelianGroup<bool>
    {
        /// <summary>
        /// Gets identity element of (bool, ⨁). I.e. false.
        /// </summary>
        public bool Identity => false;

        /// <summary>
        /// Produce inverse element of <paramref name="x"/> in (bool, ⨁). I.e. <paramref name="x"/>.
        /// </summary>
        /// <param name="x">Element to invert.</param>
        /// <returns>Inverse of <paramref name="x"/>.</returns>
        public bool Inverse(bool x) => x;

        /// <summary>
        /// Binary operation of (bool, ⨁). I.e. Logical exclusive disjunction.
        /// </summary>
        /// <returns>Logical exclusive disjunction of operands <paramref name="x"/> and <paramref name="y"/>.</returns>
        /// <inheritdoc cref="CommutativeBinaryOperation{TS}"/>
        public bool Operation(bool x, bool y) => x ^ y;
    }

    /// <summary>
    /// (bool, ⨁, ∧) is a (commutative) ring. It's invertible in it's additive group.
    /// </summary>
    public struct BooleanXorAndRingInstance : ICommutativeRing<bool>
    {
        /// <summary>
        /// Gets additive identity element of (bool, ⨁, ∧). I.e. false.
        /// </summary>
        public bool Zero => false;

        /// <summary>
        /// Gets multiplicative identity element of (bool, ⨁, ∧). I.e. true.
        /// </summary>
        public bool One => true;

        /// <summary>
        /// Additive binary operation of (bool, ⨁, ∧). I.e. Logical exclusive disjunction.
        /// </summary>
        /// <inheritdoc cref="BooleanXorGroupInstance.Operation"/>
        public bool Add(bool x, bool y) => x ^ y;

        /// <summary>
        /// Multiplicative binary operation of (bool, ⨁, ∧). I.e. Logical conjunction.
        /// </summary>
        /// <inheritdoc cref="BooleanAndMonoidInstance.Operation"/>
        public bool Multiply(bool x, bool y) => x & y;

        /// <summary>
        /// Produce additive inverse element of <paramref name="x"/> in (bool, ⨁, ∧). I.e. <paramref name="x"/>.
        /// </summary>
        /// <inheritdoc cref="BooleanXorGroupInstance.Inverse"/>
        public bool Negate(bool x) => x;

        /// <summary>
        /// Inverse additive binary operation of (bool, ⨁, ∧). I.e. Logical exclusive disjunction.
        /// </summary>
        /// <returns>Logical exclusive disjunction of operands <paramref name="minuend"/> and <paramref name="subtrahend"/>.</returns>
        /// <remarks>
        /// Since every element in (bool, ⨁) is its own inverse, subtraction is both commutative and anticommutative.
        /// </remarks>
        /// <inheritdoc cref="IRing{TS}.Subtract"/>
        public bool Subtract(bool minuend, bool subtrahend) => minuend ^ subtrahend;
    }
}
