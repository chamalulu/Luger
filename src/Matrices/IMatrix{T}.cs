// <copyright file="IMatrix{T}.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Interface of generic matrix.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public interface IMatrix<out T> : IMatrix
    {
        /// <summary>
        /// Gets element of matrix.
        /// </summary>
        /// <param name="row">1-based index of row.</param>
        /// <param name="column">1-based index of column.</param>
        /// <returns>Element at given row and column.</returns>
        /// <remarks>
        /// Indices are 1-based.
        /// Maths predates software engineering.
        /// </remarks>
        T this[int row, int column] { get; }

        /// <summary>
        /// Return submatrix with given column removed.
        /// </summary>
        /// <param name="column">1-based index of column to remove.</param>
        /// <returns>Submatrix with given column removed.</returns>
        /// <remarks>
        /// Indices are 1-based. Maths predates software engineering.
        /// </remarks>
        IMatrix<T> RemoveColumn(int column);

        /// <summary>
        /// Return submatrix with given row removed.
        /// </summary>
        /// <param name="row">1-based index of row to remove.</param>
        /// <returns>Submatrix with given row removed.</returns>
        /// <remarks>
        /// Indices are 1-based. Maths predates software engineering.
        /// </remarks>
        IMatrix<T> RemoveRow(int row);

        /// <summary>
        /// Return submatrix.  of continuous rows and columns.
        /// </summary>
        /// <param name="row">1-based index of first row.</param>
        /// <param name="column">1-based index of first column.</param>
        /// <param name="rows">Number of rows.</param>
        /// <param name="columns">Number of columns.</param>
        /// <returns>Submatrix with given number of rows and columns.</returns>
        /// <remarks>
        /// Indices are 1-based. Maths predates software engineering.<br/>
        /// If the sum of <paramref name="row"/> and <paramref name="rows"/> is greater than the <see cref="IMatrix.Rows"/>
        ///  of the source matrix then rows will wrap around. The result will include <paramref name="rows"/> number of rows.
        /// The same is true for <paramref name="column"/> and <paramref name="columns"/>.
        /// </remarks>
        IMatrix<T> Submatrix(int row, int column, int rows, int columns);
    }

    /// <summary>
    /// Interface of generic square matrix.
    /// </summary>
    /// <inheritdoc cref="IMatrix{T}"/>
    public interface ISquareMatrix<out T> : IMatrix<T>, ISquareMatrix
    {
        /// <summary>
        /// Gets sequence of elements on main diagonal.
        /// </summary>
        IEnumerable<T> Diagonal => Enumerable.Range(1, this.Order).Select(i => this[i, i]);
    }
}
