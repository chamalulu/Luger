// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1649 // File name should match first type name

namespace Luger.TypeClasses
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /* Using C# interfaces as a way to declare Haskell-like type classes is a work in progress and may prove to be a bad idea. We'll see...
     * The idea is to use an interface as a generic type constraint and the implementation as a generic type argument.
     * The implementation is accessed with the 'default' operator.
     * The interfaces and implementations correspond to what is known as Type Classes and Type Class Instances, respectively, in Haskell.
     * With a bit of luck the C# compiler can handle (and statically optimize) this craziness...
     */

    /// <summary>
    /// Type class Eq defines equality comparison. A.k.a identity relation.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public interface IEq<T>
    {
        /// <summary>
        /// Compares elements for equality.
        /// </summary>
        /// <returns>True if element <paramref name="x"/> is equal to <paramref name="y"/>; otherwise, false.</returns>
        /// <inheritdoc cref="CommutativeBinaryOperation{TS}"/>
        bool Equals(T x, T y);
    }

    /// <summary>
    /// Type class for providing a 32-bit hash code function.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public interface IHashable<T>
    {
        /// <summary>
        /// Hash function.
        /// </summary>
        /// <param name="x">Element to hash.</param>
        /// <returns>Hash code of <paramref name="x"/>.</returns>
        int GetHashCode(T x) => HashCode.Combine(x);

        /// <summary>
        /// Hash function with salt.
        /// </summary>
        /// <param name="salt">Salt to season hash code with.</param>
        /// <inheritdoc cref="GetHashCode(T)"/>
        int GetHashCode(int salt, T x) => HashCode.Combine(salt, x);
    }

    /// <summary>
    /// <see cref="EqualityComparer{T}"/> directly delegating to <see cref="IEq{T}"/>, <see cref="IHashable{T}"/> instance.
    /// </summary>
    /// <typeparam name="TI">Instance of <see cref="IEq{T}"/> and <see cref="IHashable{T}"/> for <typeparamref name="T"/>.</typeparam>
    /// <inheritdoc cref="EqualityComparer{T}"/>
    public class EqualityComparer<T, TI> : EqualityComparer<T>
        where TI : IEq<T>, IHashable<T>
    {
        /// <summary>
        /// Gets the <see cref="EqualityComparer{T, TI}"/>. It's stateless.
        /// </summary>
        public static new readonly EqualityComparer<T, TI> Default = new EqualityComparer<T, TI>();

        /// <summary>
        /// Determines if two elements of <typeparamref name="T"/> are equal using type class instance <typeparamref name="TI"/>.
        /// </summary>
        /// <param name="x">One element.</param>
        /// <param name="y">Other element.</param>
        /// <returns>True if elements are equal; otherwise, false.</returns>
        public override bool Equals([AllowNull] T x, [AllowNull] T y) => default(TI).Equals(x, y);

        /// <summary>
        /// Produces the hash code for an element of <typeparamref name="T"/> using type class instance <typeparamref name="TI"/>.
        /// </summary>
        /// <param name="obj">Element to hash.</param>
        /// <returns>Hash code of <paramref name="obj"/>.</returns>
        public override int GetHashCode([DisallowNull] T obj) => default(TI).GetHashCode(obj);
    }
}
