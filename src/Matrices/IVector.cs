// <copyright file="IVector.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    /// <summary>
    /// Orientations of a vector.
    /// </summary>
    public enum VectorOrientationEnum
    {
        /// <summary>
        /// Orientation of singleton vectors.
        /// </summary>
        None = 0,

        /// <summary>
        /// Orientation of row vectors.
        /// </summary>
        Row,

        /// <summary>
        /// Orientation of column vectors.
        /// </summary>
        Column,
    }

    /// <summary>
    /// Basic interface of non-generic vectors.
    /// </summary>
    public interface IVector : IMatrix
    {
        /// <summary>
        /// Gets dimensions of vector.
        /// </summary>
        int Dimensions => this.Rows * this.Columns;

        /// <summary>
        /// Gets orientation of vector.
        /// </summary>
        VectorOrientationEnum Orientation { get; }
    }
}
