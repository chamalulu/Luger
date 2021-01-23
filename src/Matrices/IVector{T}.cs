// <copyright file="IVector{T}.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface of generic vector.
    /// </summary>
    /// <inheritdoc cref="IMatrix{T}"/>
    public interface IVector<out T> : IVector, IMatrix<T>, IEnumerable<T>
    {
        /// <summary>
        /// Gets element of vector by index.
        /// </summary>
        /// <param name="i">Index of element.</param>
        /// <returns>Element at index <paramref name="i"/>.</returns>
        T this[Index i] => this.Orientation == VectorOrientationEnum.Column ? this[i, 0] : this[0, i];
    }
}
