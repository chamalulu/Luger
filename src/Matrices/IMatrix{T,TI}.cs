// <copyright file="IMatrix{T,TI}.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using System;
    using System.Linq;

    using Luger.TypeClasses;

    /// <summary>
    /// Interface of generic matrix with element type class instance.
    /// </summary>
    /// <typeparam name="TI">Type class instance of element.</typeparam>
    /// <inheritdoc cref="IMatrix{T}"/>
    public interface IMatrix<out T, TI> : IMatrix<T>
        where TI : struct, IRing<T>
    {
        /// <summary>
        /// Gets rank of matrix. The dimension of the vector space spanned by the columns.
        /// </summary>
        int Rank { get; }

        /// <inheritdoc cref="IMatrix{T}.RemoveColumn"/>
        new IMatrix<T, TI> RemoveColumn(int column);

        /// <inheritdoc cref="IMatrix{T}.RemoveRow"/>
        new IMatrix<T, TI> RemoveRow(int row);

        /// <inheritdoc cref="IMatrix{T}.Submatrix"/>
        new IMatrix<T, TI> Submatrix(int row, int column, int rows, int columns);
    }

    /// <summary>
    /// Interface of generic square matrix with element type class instance.
    /// </summary>
    /// <inheritdoc cref="IMatrix{T, TI}"/>
    public interface ISquareMatrix<out T, TI> : ISquareMatrix<T>, IMatrix<T, TI>
        where TI : struct, IField<T>
    {
        /// <summary>
        /// Gets determinant of matrix.
        /// </summary>
        T Determinant { get; }

        /// <summary>
        /// Gets inverse of matrix if invertible.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if matrix is singular.</exception>
        ISquareMatrix<T, TI> Inverse { get; }

        /// <summary>
        /// Gets trace of matrix.
        /// </summary>
        T Trace => this.Diagonal.Aggregate(default(TI).Zero, default(TI).Add);

        /// <summary>
        /// Calculate power of matrix raised to n.
        /// </summary>
        /// <param name="n">Exponent.</param>
        /// <returns>Power of matrix.</returns>
        /// <exception cref="InvalidOperationException">Thrown if matrix is singular and exponent is negative.</exception>
        ISquareMatrix<T, TI> Pow(int n);
    }
}
