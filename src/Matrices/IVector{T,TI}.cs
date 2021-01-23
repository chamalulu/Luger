// <copyright file="IVector{T,TI}.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using Luger.TypeClasses;

    /// <summary>
    /// Interface of generic vector with element type class instance.
    /// </summary>
    /// <inheritdoc cref="IMatrix{T, TI}"/>
    public interface IVector<out T, TI> : IVector<T>, IMatrix<T, TI>
        where TI : struct, IRing<T>
    {
    }
}
