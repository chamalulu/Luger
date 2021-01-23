// <copyright file="IMatrix.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using System.Collections;

    /// <summary>
    /// Basic interface of non-generic matrix.
    /// </summary>
    public interface IMatrix : IStructuralEquatable
    {
        /// <summary>
        /// Gets number of columns.
        /// </summary>
        int Columns { get; }

        /// <summary>
        /// Gets number of rows.
        /// </summary>
        int Rows { get; }

        /// <summary>
        /// Gets transpose of matrix.
        /// </summary>
        IMatrix Transpose { get; }
    }

    /// <summary>
    /// Basic interface of non-generic square matrix.
    /// </summary>
    public interface ISquareMatrix : IMatrix
    {
        /// <summary>
        /// Gets order of square matrix.
        /// </summary>
        int Order => this.Rows;  // or Columns...
    }
}
