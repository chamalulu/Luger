using System;
using System.Collections.Immutable;
using System.Linq;

namespace Luger.Utilities
{
    public static class HashCodeExtensions
    {
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="hashCode">The <see cref="HashCode"/> to add the <paramref name="value"/> to.</param>
        /// <param name="value">The value to add to <paramref name="hashCode"/>.</param>
        /// <returns><paramref name="hashCode"/> for further additions</returns>
        /// <inheritdoc cref="HashCode.Add{T}(T)"/>
        public static HashCode AddF<T>(this HashCode hashCode, T value)
        {
            hashCode.Add(value);
            return hashCode;
        }

        /// <summary>
        /// Aggregate hash codes from elements of <paramref name="array"/> into <paramref name="hashCode"/>.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="array"/> elements.</typeparam>
        /// <param name="hashCode">The <see cref="HashCode"/> to aggregate into.</param>
        /// <param name="array">The elements to aggregate hash codes from.</param>
        /// <returns><paramref name="hashCode"/> for further additions</returns>
        public static HashCode AddF<T>(this HashCode hashCode, ImmutableArray<T> array) => array.Aggregate(hashCode, AddF);

        /// <summary>
        /// Aggregate hash codes from elements of <paramref name="list"/> into <paramref name="hashCode"/>.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="list"/> elements.</typeparam>
        /// <param name="hashCode">The <see cref="HashCode"/> to aggregate into.</param>
        /// <param name="list">The elements to aggregate hash codes from.</param>
        /// <returns><paramref name="hashCode"/> for further additions</returns>
        public static HashCode AddF<T>(this HashCode hashCode, IImmutableList<T> list) => list.Aggregate(hashCode, AddF);

        /// <summary>
        /// Aggregate hash codes from elements of <paramref name="set"/> into <paramref name="hashCode"/>.
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="set"/> elements.</typeparam>
        /// <param name="hashCode">The <see cref="HashCode"/> to aggregate into.</param>
        /// <param name="set">The elements to aggregate hash codes from.</param>
        /// <returns><paramref name="hashCode"/> for further additions</returns>
        /// <remarks>The hash code does not depend on the order of elements.</remarks>
        public static HashCode AddF<T>(this HashCode hashCode, IImmutableSet<T> set)
        {
            static int func(int hc, T t) => hc ^ t?.GetHashCode() ?? 0;

            var hc = set.Aggregate(set.Count, func);    // Seed with #set to distinguish between {} and {null}.

            hashCode.Add(hc);

            return hashCode;
        }
    }
}
