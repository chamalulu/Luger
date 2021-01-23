// <copyright file="Vector{T}.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

namespace Luger.Matrices
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Luger.Utilities;

    /// <summary>
    /// A generic vector.
    /// </summary>
    /// <inheritdoc/>
    public readonly struct Vector<T> : IVector<T>
    {
        private readonly IFrameBuffer<T> frameBuffer;

        private Vector(IFrameBuffer<T> frameBuffer)
        {
            if (frameBuffer.Width == 1 || frameBuffer.Height == 1)
            {
                this.frameBuffer = frameBuffer;
            }
            else
            {
                throw new ArgumentException("At least one dimension must be equal to 1.", nameof(frameBuffer));
            }
        }

        /// <inheritdoc/>
        public int Dimensions => this.frameBuffer.Width * this.frameBuffer.Height;

        /// <inheritdoc/>
        public VectorOrientationEnum Orientation
            => (this.frameBuffer.Width, this.frameBuffer.Height) switch
            {
                (1, 1) => VectorOrientationEnum.None,
                (1, _) => VectorOrientationEnum.Column,
                (_, 1) => VectorOrientationEnum.Row,
                (_, _) => throw new InvalidOperationException()
            };

        /// <inheritdoc/>
        int IMatrix.Rows => this.frameBuffer.Height;

        /// <inheritdoc/>
        int IMatrix.Columns => this.frameBuffer.Width;

        /// <inheritdoc/>
        T IMatrix<T>.this[Index i, Index j] => this.frameBuffer[j, i];

        /// <inheritdoc/>
        IVector<T> IMatrix<T>.this[Range @is, Index j] => new Vector<T>(this.frameBuffer[j, @is]);

        /// <inheritdoc/>
        IVector<T> IMatrix<T>.this[Index i, Range js] => new Vector<T>(this.frameBuffer[js, i]);

        /// <inheritdoc/>
        IMatrix<T> IMatrix<T>.this[Range @is, Range js] => new Matrix<T>(this.frameBuffer[js, @is]);

        /// <inheritdoc/>
        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
            => other switch
            {
                IFrameBuffer<T> frameBuffer => this.frameBuffer.Equals(frameBuffer, comparer),  // Direct frame buffer comparison
                IStructuralEquatable streq => streq.Equals(this.frameBuffer, comparer),         // Double dispatch with frame buffer
                _ => comparer.Equals(this, other)
            };

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var k in Enumerable.Range(0, this.Dimensions))
            {
                yield return this.Orientation == VectorOrientationEnum.Row
                    ? this.frameBuffer[0, k]
                    : this.frameBuffer[k, 0];
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
            => this.frameBuffer.GetHashCode(comparer);
    }
}
