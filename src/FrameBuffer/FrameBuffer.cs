// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

namespace Luger.FrameBuffer
{
    using System;
    using System.Collections;

    /* About coordinates
     * -----------------
     * A source of confusion are the different coordinate systems used in different contexts.
     * The FrameBuffer class implements a framebuffer indexed by x and y coordinates where x is
     *  increasing to the right and y is increasing downwards, like most frame buffers used in
     *  computer graphics.
     * It is going to be used by the Matrix classes, among other things. Elements in matrices are
     *  indexed by row and column in that order and often with variables i and j where i is
     *  increasing downwards and j is increasing to the right.
     * These coordinate systems differ in the order of the coordinates specified and both differ
     *  from how 2d cartesian coordinates are mostly used where x is increasing to the right but y
     *  is increasing upwards.
     * Another difference is also that cartesian coordinates are often real numbers and not
     *  integers as in frame buffers and matrices.
     * Also, coordinates in frame buffers are 0-based while indices in matrices for historical
     *  reasons are 1-based.
     * Anyway. Your best takeaway from this is to mind the order of the coordinate elements and
     *  look out for off by one errors.
     */

    /// <summary>
    /// Transformed frame buffer. Shares memory buffer with its parent <see cref="FrameBufferMemoryOwner{T}"/>
    ///  and other descendant <see cref="FrameBuffer{T}"/>s.
    /// Make sure instances are out of scope before parent <see cref="FrameBufferMemoryOwner{T}"/>
    ///  is disposed and follow the memory usage guidelines as specified in
    ///  https://docs.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines .
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public readonly struct FrameBuffer<T> : IStructuralEquatable
    {
        private readonly Memory<T> buffer;
        private readonly XY2I transform;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameBuffer{T}"/> struct.
        /// </summary>
        /// <param name="buffer">Memory buffer holding element values.</param>
        /// <param name="width">Width of frame buffer.</param>
        /// <param name="height">Height of frame buffer.</param>
        /// <param name="transform">Transformation of 0-based (x, y) coordinates to buffer index.</param>
        internal FrameBuffer(Memory<T> buffer, int width, int height, XY2I transform)
        {
            this.buffer = buffer;
            this.transform = transform;
            this.Width = width;
            this.Height = height;
        }

        /// <summary>
        /// Gets width of frame buffer.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets height of frame buffer.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets transpose of frame buffer.
        /// </summary>
        /// <remarks>
        /// Frame buffers are a coordinate transformation and a reference to a memory buffer owned by a <see cref="FrameBufferMemoryOwner{T}"/>.
        /// Transpose will create a new frame buffer with a transformation swapping x and y coordinates. This is done without memory allocations.
        /// Taking the transpose of the transpose will return exactly the same frame buffer.
        /// </remarks>
        public FrameBuffer<T> Transpose => new FrameBuffer<T>(this.buffer, this.Height, this.Width, this.transform * XY2XY.Transpose);

        /// <summary>
        /// Gets or sets element at coordinates (<paramref name="x"/>, <paramref name="y"/>).
        /// </summary>
        /// <param name="x">x coordinate.</param>
        /// <param name="y">y coordinate.</param>
        /// <returns>Element of frame buffer.</returns>
        public T this[Index x, Index y]
        {
            get => this.ElementReference(x, y);
            set => this.ElementReference(x, y) = value;
        }

        /// <summary>
        /// Gets frame buffer window of width 1 into this frame buffer.
        /// </summary>
        /// <param name="x">x coordinate.</param>
        /// <param name="ys">Range of y coordinates.</param>
        /// <returns>Frame buffer.</returns>
        public FrameBuffer<T> this[Index x, Range ys]
        {
            get
            {
                var xOffset = x.GetOffset(this.Width);
                var (yOffset, height) = ys.GetOffsetAndLength(this.Height);

                return this.Window(xOffset, yOffset, 1, height);
            }
        }

        /// <summary>
        /// Gets frame buffer window of height 1 into this frame buffer.
        /// </summary>
        /// <param name="xs">Range of x coordinates.</param>
        /// <param name="y">y coordinate.</param>
        /// <returns>Frame buffer.</returns>
        public FrameBuffer<T> this[Range xs, Index y]
        {
            get
            {
                var (xOffset, width) = xs.GetOffsetAndLength(this.Width);
                var yOffset = y.GetOffset(this.Height);

                return this.Window(xOffset, yOffset, width, 1);
            }
        }

        /// <summary>
        /// Gets frame buffer window into this frame buffer.
        /// </summary>
        /// <inheritdoc cref="this[Range, Index]"/>
        /// <inheritdoc cref="this[Index, Range]"/>
        public FrameBuffer<T> this[Range xs, Range ys]
        {
            get
            {
                var (xOffset, width) = xs.GetOffsetAndLength(this.Width);
                var (yOffset, height) = ys.GetOffsetAndLength(this.Height);

                return this.Window(xOffset, yOffset, width, height);
            }
        }

        /// <summary>
        /// Create a frame buffer of given dimensions over given memory buffer.
        /// </summary>
        /// <param name="buffer">Buffer to map elements to.</param>
        /// <param name="width">Width of frame buffer.</param>
        /// <param name="height">Height of frame buffer.</param>
        /// <returns>Frame buffer.</returns>
        /// <remarks>
        /// Transform will be initialized to [ 1 width 0 ].
        /// </remarks>
        public static FrameBuffer<T> Create(Memory<T> buffer, int width, int height)
            => new FrameBuffer<T>(buffer, width, height, new XY2I(width));

        /// <inheritdoc/>
        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
            => other is FrameBuffer<T> frameBuffer && new FrameBufferEqualityComparer<T>(comparer).Equals(this, frameBuffer);

        /// <inheritdoc/>
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
            => new FrameBufferEqualityComparer<T>(comparer).GetHashCode(this);

        private ref T ElementReference(Index x, Index y)
        {
            var coordinates = (x.GetOffset(this.Width), y.GetOffset(this.Height));

            var index = this.transform * coordinates;

            return ref this.buffer.Span[index];
        }

        private FrameBuffer<T> Window(int xOffset, int yOffset, int width, int height)
        {
            var translation = XY2XY.Translation(xOffset, yOffset);
            var mirror = XY2XY.Scale(Math.Sign(width), Math.Sign(height));

            var transform = this.transform * translation * mirror;

            return new FrameBuffer<T>(this.buffer, width, height, transform);
        }
    }

    /// <summary>
    /// Read only wrapper for <see cref="FrameBuffer{T}"/>.
    /// </summary>
    /// <inheritdoc cref="FrameBuffer{T}"/>
    public readonly struct ReadOnlyFrameBuffer<T> : IStructuralEquatable
    {
        private readonly FrameBuffer<T> frameBuffer;

        private ReadOnlyFrameBuffer(FrameBuffer<T> frameBuffer) => this.frameBuffer = frameBuffer;

        /// <inheritdoc cref="FrameBuffer{T}.Width"/>
        public int Width => this.frameBuffer.Width;

        /// <inheritdoc cref="FrameBuffer{T}.Height"/>
        public int Height => this.frameBuffer.Height;

        /// <inheritdoc cref="FrameBuffer{T}.Transpose"/>
        public ReadOnlyFrameBuffer<T> Transpose => this.frameBuffer.Transpose;

        /// <summary>
        /// Gets element at coordinates (<paramref name="x"/>, <paramref name="y"/>).
        /// </summary>
        /// <inheritdoc cref="FrameBuffer{T}.this[Index, Index]"/>
        public T this[Index x, Index y] => this.frameBuffer[x, y];

        /// <returns>Read only frame buffer.</returns>
        /// <inheritdoc cref="FrameBuffer{T}.this[Index, Range]"/>
        public ReadOnlyFrameBuffer<T> this[Index x, Range ys] => this.frameBuffer[x, ys];

        /// <returns>Read only frame buffer.</returns>
        /// <inheritdoc cref="FrameBuffer{T}.this[Range, Index]"/>
        public ReadOnlyFrameBuffer<T> this[Range xs, Index y] => this.frameBuffer[xs, y];

        /// <returns>Read only frame buffer.</returns>
        /// <inheritdoc cref="FrameBuffer{T}.this[Range, Range]"/>
        public ReadOnlyFrameBuffer<T> this[Range xs, Range ys] => this.frameBuffer[xs, ys];

        public static implicit operator ReadOnlyFrameBuffer<T>(FrameBuffer<T> frameBuffer) => new ReadOnlyFrameBuffer<T>(frameBuffer);

        /// <inheritdoc/>
        public bool Equals(object? other, IEqualityComparer comparer) => ((IStructuralEquatable)this.frameBuffer).Equals(other, comparer);

        /// <inheritdoc/>
        public int GetHashCode(IEqualityComparer comparer) => ((IStructuralEquatable)this.frameBuffer).GetHashCode(comparer);
    }
}
