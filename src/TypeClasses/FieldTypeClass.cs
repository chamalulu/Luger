// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    /// <summary>
    /// Type class of field.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <remarks>
    /// A field is a <see cref="ICommutativeRing{T}">commutative ring</see>
    ///  which contains a multiplicative inverse for every nonzero element.<br/>
    /// The sets of rational, real and complex numbers with respective addition and multiplication are examples of fields.
    /// </remarks>
    public interface IField<TS> : ICommutativeRing<TS>
    {
        /// <summary>
        /// Reciprocation, the inverse elements of multiplicative group.
        /// </summary>
        /// <param name="x">Element to get multiplicative inverse of.</param>
        /// <returns>Reciprocal of <paramref name="x"/>.</returns>
        TS Reciprocate(TS x);

        /// <summary>
        /// Division, the inverse binary operation of multiplicative group.
        /// </summary>
        /// <param name="dividend">The element being divided.</param>
        /// <param name="divisor">The dividing element.</param>
        /// <returns>Quotient of <paramref name="dividend"/> divided by <paramref name="divisor"/>.</returns>
        TS Divide(TS dividend, TS divisor);
    }

    /// <summary>
    /// Generic field type class instance delegating to group type class instances.
    /// </summary>
    /// <typeparam name="TS">Element type of set.</typeparam>
    /// <typeparam name="TAdditiveGroup">Additive group type class instance.</typeparam>
    /// <typeparam name="TMultiplicativeGroup">Multiplicative group type class instance.</typeparam>
    public struct Field<TS, TAdditiveGroup, TMultiplicativeGroup> : IField<TS>
        where TAdditiveGroup : struct, IAbelianGroup<TS>
        where TMultiplicativeGroup : struct, IAbelianGroup<TS>
    {
        /// <inheritdoc cref="ISemiring{TS}.Zero"/>
        public TS Zero => default(TAdditiveGroup).Identity;

        /// <inheritdoc cref="ISemiring{TS}.One"/>
        public TS One => default(TMultiplicativeGroup).Identity;

        /// <inheritdoc cref="ISemiring{TS}.Add"/>
        public TS Add(TS x, TS y) => default(TAdditiveGroup).Operation(x, y);

        /// <inheritdoc cref="IField{TS}.Divide"/>
        public TS Divide(TS dividend, TS divisor) => this.Multiply(dividend, this.Reciprocate(divisor));

        /// <inheritdoc cref="ICommutativeRing{TS}.Multiply"/>
        public TS Multiply(TS x, TS y) => default(TMultiplicativeGroup).Operation(x, y);

        /// <inheritdoc cref="IRing{TS}.Negate"/>
        public TS Negate(TS x) => default(TAdditiveGroup).Inverse(x);

        /// <inheritdoc cref="IField{TS}.Reciprocate"/>
        public TS Reciprocate(TS x) => default(TMultiplicativeGroup).Inverse(x);

        /// <inheritdoc cref="IRing{TS}.Subtract"/>
        public TS Subtract(TS minuend, TS subtrahend) => this.Add(minuend, this.Negate(subtrahend));
    }
}
