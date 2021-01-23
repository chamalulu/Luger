// <copyright file="Matrix{T}.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using System;
    using System.Collections;
    using Luger.Utilities;

    /// <summary>
    /// A generic matrix.
    /// </summary>
    /// <inheritdoc cref="IMatrix{T}"/>
    public readonly struct Matrix<T> : IMatrix<T>
    {
        private readonly IFrameBuffer<T> frameBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix{T}"/> struct using frame buffer.
        /// </summary>
        /// <param name="frameBuffer">Frame buffer holding elements.</param>
        internal Matrix(IFrameBuffer<T> frameBuffer)
            => this.frameBuffer = frameBuffer ?? throw new ArgumentNullException(nameof(frameBuffer));

        /// <inheritdoc/>
        public int Rows => this.frameBuffer.Height;

        /// <inheritdoc/>
        public int Columns => this.frameBuffer.Width;

        /// <inheritdoc cref="IMatrix.Transpose"/>
        public IMatrix<T> Transpose => new Matrix<T>(this.frameBuffer.Transpose);

        /// <inheritdoc/>
        IMatrix IMatrix.Transpose => this.Transpose;

        /// <inheritdoc/>
        public T this[int row, int column]
        {
            get
            {
                if (row < 1 || row > this.frameBuffer.Height)
                {
                    throw new ArgumentOutOfRangeException(nameof(row));
                }

                if (column < 1 || column > this.frameBuffer.Width)
                {
                    throw new ArgumentOutOfRangeException(nameof(column));
                }

                return this.frameBuffer[column - 1, row - 1];
            }
        }

        /// <inheritdoc/>
        public IMatrix<T> RemoveColumn(int column)
        {
            if (column == 1)
            {
                return new Matrix<T>(this.frameBuffer[1.., ..]);
            }

            if (column == this.frameBuffer.Width)
            {
                return new Matrix<T>(this.frameBuffer[..^1, ..]);
            }

            // TODO: Implement non-linear remapping matrix type
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IMatrix<T> RemoveRow(int row)
        {
            if (row == 1)
            {
                return new Matrix<T>(this.frameBuffer[.., 1..]);
            }

            if (row == this.frameBuffer.Height)
            {
                return new Matrix<T>(this.frameBuffer[.., ..^1]);
            }

            // TODO: Implement non-linear remapping matrix type
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IMatrix<T> Submatrix(int row, int column, int rows, int columns)
        {
            if (row < 1 || row > this.frameBuffer.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }

            if (column < 1 || column > this.frameBuffer.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            // TODO: Implement non-linear remapping matrix type
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
            => other switch
            {
                IFrameBuffer<T> frameBuffer => this.frameBuffer.Equals(frameBuffer, comparer),
                IStructuralEquatable streq => streq.Equals(this, comparer),
                _ => comparer.Equals(this, other)
            };

        /// <inheritdoc/>
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
            => this.frameBuffer.GetHashCode(comparer);
    }
}
