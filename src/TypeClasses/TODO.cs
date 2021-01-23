// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    using System;

    /// <summary>
    /// Type class for sets of elements where square root can be (at least partially) defined.
    /// </summary>
    /// <typeparam name="TS">Type of element.</typeparam>
    public interface IQuadratic<TS>
    {
        /// <summary>
        /// Principal square root of element.
        /// </summary>
        /// <param name="x">Element to take principal square root of.</param>
        /// <returns>Principal square root of element <paramref name="x"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when no square roots are defined for <paramref name="x"/>.</exception>
        /// <remarks>
        /// Each element may have 0 or more square roots defined depending on quadratic closure and certain other algebraic voodoo.
        /// This function returns the principal square root (if any).
        /// For positive real numbers, which have two square roots symmetric around 0, the positive is the principal.
        /// For negative real numbers there is no square root due to quadratic non-closure.
        /// For real 0 both square roots are 0.
        /// For complex numbers, which have two square roots, the one with argument in (-π/2 .. π/2] is the principal. I.e. Sqrt(-1) := +i .
        /// For real and complex square matrices square roots are a bit more complicated. TBD.
        /// </remarks>
        TS Sqrt(TS x);
    }
}
