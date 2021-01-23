// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1005 // Single line comments should begin with single space

namespace Luger.TypeClasses
{
    /// <summary>
    /// Extension methods for ring-like type classes.
    /// </summary>
    public static class RingExtensions
    {
        // TODO: Define scalar multiplication by Module- and Algebra-like type classes.

        /// <summary>
        /// Scalar multiplication.
        /// </summary>
        /// <typeparam name="TS">Element type of set.</typeparam>
        /// <param name="semiring">Semiring type class instance.</param>
        /// <param name="n">Scalar factor.</param>
        /// <param name="x">Element to multiply.</param>
        /// <returns>Scalar product.</returns>
        /// <remarks>
        /// Scalar multiplication is not a property of semiring by itself but we cheat and
        ///  specialize it with scalar uint, which is a semiring with total order.
        /// </remarks>
        public static TS Multiply<TS>(this ISemiring<TS> semiring, uint n, TS x)
            => (n > 1, (n & 1) == 1) switch
            {
                //tex: $0x = 0$
                (false, false) => semiring.Zero,

                //tex: $1x = x$
                (false, true) => x,

                //tex: $2kx = k(x + x)$
                (true, false) => semiring.Multiply(n >> 1, semiring.Add(x, x)),

                //tex: $(2k + 1)x = 2kx + x$
                (true, true) => semiring.Add(semiring.Multiply(n - 1, x), x)
            };

        /// <param name="ring">Ring type class instance.</param>
        /// <remarks>
        /// Scalar multiplication is not a property of ring by itself but we cheat and specialize
        ///  it with scalar int, which is a ring with total order.
        /// </remarks>
        /// <inheritdoc cref="Multiply{TS}(ISemiring{TS}, uint, TS)"/>
        public static TS Multiply<TS>(this IRing<TS> ring, int n, TS x)
            => n < 0 ? ring.Multiply((uint)-n, ring.Negate(x)) : ring.Multiply((uint)n, x);

        /// <summary>
        /// Square of element.
        /// </summary>
        /// <typeparam name="TS">Element type of set.</typeparam>
        /// <param name="semiring">Semiring type class instance.</param>
        /// <param name="x">Element to square.</param>
        /// <returns>Square of element <paramref name="x"/>.</returns>
        public static TS Square<TS>(this ISemiring<TS> semiring, TS x) => semiring.Multiply(x, x);

        /// <summary>
        /// Power of element to a non-negative integer.
        /// </summary>
        /// <typeparam name="TS">Element type of set.</typeparam>
        /// <param name="semiring">Semiring type class instance.</param>
        /// <param name="n">Exponent.</param>
        /// <param name="b">Base.</param>
        /// <returns>Base <paramref name="b"/> raised to the power of <paramref name="n"/>.</returns>
        /// <remarks>
        /// The case 0^0 seems to be disagreed upon but we use the convention of 0^0 = 1.<br/>
        /// See Wikipedia article <a href="https://en.wikipedia.org/wiki/Zero_to_the_power_of_zero">Zero to the power of zero</a>.<br/>
        /// BTW, this implementation already exist as <see cref="GroupExtensions.Pow{TS}(IMonoid{TS}, uint, TS)"/>
        ///  but we don't have access to the multiplicative monoid here. See <see cref="ISemiring{TS, TAdditiveGroup, TMultiplicativeGroup}.Pow(uint, TS)"/>.
        /// </remarks>
        public static TS Pow<TS>(this ISemiring<TS> semiring, uint n, TS b)
            => (n > 1, (n & 1) == 1) switch
            {
                //tex: $b^0 = 1$
                (false, false) => semiring.One,

                //tex: $b^1 = b$
                (false, true) => b,

                //tex: $b^{2k} = (b^2)^k$
                (true, false) => semiring.Pow(n >> 1, semiring.Square(b)),

                //tex: $b^{2k+1} = b^{2k} \cdot b$
                (true, true) => semiring.Multiply(semiring.Pow(n - 1, b), b)
            };

        /// <summary>
        /// Power of element to an integer.
        /// </summary>
        /// <param name="field">Field type class instance.</param>
        /// <inheritdoc cref="Pow{TS}(ISemiring{TS}, uint, TS)"/>
        public static TS Pow<TS>(this IField<TS> field, int n, TS b)
            => n < 0 ? field.Pow((uint)-n, field.Reciprocate(b)) : field.Pow((uint)n, b);
    }
}
