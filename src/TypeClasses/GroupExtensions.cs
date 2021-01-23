// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1005 // Single line comments should begin with single space

namespace Luger.TypeClasses
{
    using System;

    /// <summary>
    /// Extension methods for group-like type classes.
    /// </summary>
    public static class GroupExtensions
    {
        /// <summary>
        /// Power of magma element to a positive integer exponent.
        /// </summary>
        /// <typeparam name="TS">Element type of set.</typeparam>
        /// <param name="magma">Magma type class instance.</param>
        /// <param name="n">Exponent.</param>
        /// <param name="b">Base.</param>
        /// <returns>Base <paramref name="b"/> raised to the power of <paramref name="n"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Pow(0, b) is undefined.</exception>
        /// <remarks>
        /// Time complexity: O(n).
        /// </remarks>
        public static TS Pow<TS>(this IMagma<TS> magma, uint n, TS b)
        {
            if (n == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "Power of degree 0 is undefined for magma since it has no identity element.");
            }

            var r = b;
            n -= 1;

            while (n > 0)
            {
                r = magma.Operation(r, b);
            }

            return r;
        }

        /// <summary>
        /// Power of semigroup element to a positive integer exponent.
        /// </summary>
        /// <param name="semigroup">Semigroup type class instance.</param>
        /// <remarks>
        /// This function uses associativity of <see cref="ISemigroup{TS}"/>.Operation to optimize time complexity to O(log(n)).
        /// </remarks>
        /// <inheritdoc cref="Pow{TS}(IMagma{TS}, uint, TS)"/>
        public static TS Pow<TS>(this ISemigroup<TS> semigroup, uint n, TS b)
            => (n > 1, (n & 1) == 1) switch
            {
                //tex: $b^0$ is undefined
                (false, false) => throw new ArgumentOutOfRangeException(nameof(n), "Power of degree 0 is undefined for semigroup since it has no identity element."),

                //tex: $b^1 = b$
                (false, true) => b,

                //tex: $b^{2k} = (b^2)^k$
                (true, false) => semigroup.Pow(n >> 1, semigroup.Operation(b, b)),

                //tex: $b^{2k+1} = b^{2k} \cdot b$
                (true, true) => semigroup.Operation(semigroup.Pow(n - 1, b), b)
            };

        /// <summary>
        /// Power of monoid element to a non-negative integer exponent.
        /// </summary>
        /// <typeparam name="TS">Element type of set.</typeparam>
        /// <param name="monoid">Monoid type class instance.</param>
        /// <param name="n">Exponent.</param>
        /// <param name="b">Base.</param>
        /// <returns>Base <paramref name="b"/> raised to the power of <paramref name="n"/>.</returns>
        /// <remarks>
        /// Pow(0, b) = Identity element.<br/>
        /// For exponents > 0, delegates to <see cref="Pow{TS}(ISemigroup{TS}, uint, TS)"/>.
        /// </remarks>
        public static TS Pow<TS>(this IMonoid<TS> monoid, uint n, TS b)
            => n switch
            {
                //tex: $b^0 = 1$
                0 => monoid.Identity,

                _ => ((ISemigroup<TS>)monoid).Pow(n, b)
            };

        /// <summary>
        /// Power of group element to an integer exponent.
        /// </summary>
        /// <param name="group">Group type class instance.</param>
        /// <remarks>
        /// For negative exponents, delegate to <see cref="Pow{TS}(ISemigroup{TS}, uint, TS)"/> with -<paramref name="n"/> and inverse of <paramref name="b"/>.<br/>
        /// For non-negative exponents, delegate to <see cref="Pow{TS}(IMonoid{TS}, uint, TS)"/>.
        /// </remarks>
        /// <inheritdoc cref="Pow{TS}(IMonoid{TS}, uint, TS)"/>
        public static TS Pow<TS>(this IGroup<TS> group, int n, TS b)
            => n switch
            {
                //tex: $b^{-n} = (b^{-1})^n$
                < 0 => ((ISemigroup<TS>)group).Pow((uint)-n, group.Inverse(b)),

                _ => ((IMonoid<TS>)group).Pow((uint)n, b)
            };
    }
}
