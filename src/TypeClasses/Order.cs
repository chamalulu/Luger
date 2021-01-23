// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The cases of an order comparison.
    /// </summary>
    public enum OrderEnum
    {
        /// <summary>
        /// Less than.
        /// </summary>
        LT = -1,

        /// <summary>
        /// Equal to.
        /// </summary>
        EQ = 0,

        /// <summary>
        /// Greater than.
        /// </summary>
        GT = 1,
    }

    /// <summary>
    /// Type class Ord defines order comparison.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public interface IOrd<T>
    {
        /// <summary>
        /// Compares two elements and returns a value indicating if the first is less than, equal to or greater than the second.
        /// </summary>
        /// <param name="x">First element.</param>
        /// <param name="y">Second element.</param>
        /// <returns>Value indicating order among given elements.</returns>
        OrderEnum Compare(T x, T y);
    }

    /// <summary>
    /// <see cref="Comparer{T}"/> directly delegating to <see cref="IOrd{T}"/> instance.
    /// </summary>
    /// <typeparam name="TI">Instance of <see cref="IOrd{T}"/> for <typeparamref name="T"/>.</typeparam>
    /// <inheritdoc cref="Comparer{T}"/>
    public class Comparer<T, TI> : Comparer<T>
        where TI : IOrd<T>
    {
        /// <summary>
        /// Gets the <see cref="Comparer{T, TI}"/>. It's stateless.
        /// </summary>
        public static new readonly Comparer<T, TI> Default = new Comparer<T, TI>();

        /// <summary>
        /// Compares two elements and returns a value indicating if the first is less than, equal to or greater than the second.
        /// </summary>
        /// <param name="x">First element.</param>
        /// <param name="y">Second element.</param>
        /// <returns>Value indicating order among given elements.</returns>
        public override int Compare([AllowNull] T x, [AllowNull] T y) => (int)default(TI).Compare(x, y);
    }
}
