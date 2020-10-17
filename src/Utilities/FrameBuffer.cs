using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Luger.Utilities
{
    /// <summary>
    /// Common interface of FrameBuffer and the transformed frame buffer returned from range indexers and <see cref="Transpose"/>.
    /// </summary>
    /// <typeparam name="T">Type of element</typeparam>
    public interface IFrameBuffer<T>
    {
        /// <summary>
        /// Width of frame buffer
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Height of frame buffer
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Index a single element of the frame buffer
        /// </summary>
        /// <param name="x">X coordinate of element.</param>
        /// <param name="y">Y coordinate of element.</param>
        /// <returns>Value of element.</returns>
        T this[Index x, Index y] { get; }

        /// <summary>
        /// Return a 1xN window of the frame buffer.
        /// </summary>
        /// <param name="x">X coordinate of the window.</param>
        /// <param name="ys">Range of Y coordinates of the window.</param>
        /// <returns>1xN window frame buffer.</returns>
        IFrameBuffer<T> this[Index x, Range ys] { get; }

        /// <summary>
        /// Return a Nx1 window of the frame buffer.
        /// </summary>
        /// <param name="xs">Range of X coordinates of the window.</param>
        /// <param name="y">Y coordinate of the window.</param>
        /// <returns>Nx1 window frame buffer.</returns>
        IFrameBuffer<T> this[Range xs, Index y] { get; }

        /// <summary>
        /// Return a MxN window of the frame buffer.
        /// </summary>
        /// <param name="xs">Range of X coordinates of the window.</param>
        /// <param name="ys">Range of Y coordinates of the window.</param>
        /// <returns>MxN window frame buffer.</returns>
        IFrameBuffer<T> this[Range xs, Range ys] { get; }

        /// <summary>
        /// Return a transposed frame buffer. A transposed MxN frame buffer has dimensions NxM.
        /// </summary>
        IFrameBuffer<T> Transpose { get; }
    }

    /* About coordinates
     * -----------------
     * A source of confusion are the different coordinate systems used in different contexts.
     * The class below implements a framebuffer indexed by x and y coordinates where x is
     *  increasing to the right and y is increasing downwards, like most framebuffers used in
     *  computer graphics.
     * It is going to be used by the Matrix classes, among other things. Elements in matrices are
     *  indexed by row and column in that order and often with variables i and j where i is
     *  increasing downwards and j is increasing to the right.
     * These coordinate systems differ in the order of the coordinates specified and both differ
     *  from how 2d cartesian coordinates are mostly used where x is increasing to the right but y
     *  is increasing upwards.
     * Another difference is also that cartesian coordinates are often floating point numbers and
     *  not integers as in framebuffers and matrices.
     * Anyway. Your best takeaway from this is to mind the order of the coordinate elements.
     */

    /// <summary>
    /// Root frame buffer. Instances own their memory buffer which is shared by all its transformations.
    /// Make sure instances are properly disposed and follow the memory usage guidelines as specified in
    /// https://docs.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public sealed class FrameBuffer<T> : IFrameBuffer<T>, IDisposable
    {
        /// <summary>
        /// 1x3 matrix transforming x,y coordinates to one-dimensional offset-based index.
        /// </summary>
        //tex: $$\begin{pmatrix} k_x & k_y & m \\ \end{pmatrix}$$
        private readonly struct XY2I
        {
            public readonly int k_x, k_y, m;

            private XY2I(int k_x, int k_y, int m)
            {
                this.k_x = k_x;
                this.k_y = k_y;
                this.m = m;
            }

            /// <summary>
            /// Create 1x3 matrix transforming zero-based x,y coordinates to one-dimensional zero-based index.
            /// </summary>
            /// <param name="stride">Width of domain plane (i.e. frame buffer).</param>
            //tex: $$\begin{pmatrix} 1 & stride & 0 \\ \end{pmatrix}$$
            public XY2I(int stride) : this(1, stride, 0) { }

            /// <summary>
            /// Transform x,y coordinates to index
            /// </summary>
            /// <param name="matrix">Transformation matrix</param>
            /// <param name="coordinates">x,y coordinates. Third element in column vector is assumed 1.</param>
            /// <returns>One-dimensional index</returns>
            //tex: $$matrix \cdot coordinates = \begin{pmatrix} k_x & k_y & m \\ \end{pmatrix} \begin{pmatrix} x \\ y \\ 1 \\ \end{pmatrix} = k_x x + k_y y + m$$
            public static int operator *(XY2I matrix, (int x, int y) coordinates)
                => matrix.k_x * coordinates.x + matrix.k_y * coordinates.y + matrix.m;

            /// <summary>
            /// Combine x,y coordinate to index transformation with x,y coordinate to x,y coordinate transformation.
            /// </summary>
            /// <param name="a">x,y coordinate to index transformation.</param>
            /// <param name="b">x,y coordinate to x,y coordinate transformation.</param>
            /// <returns>Combined x,y coordinate to index transformation</returns>
            //tex: $$
            // a \cdot b =
            // \begin{pmatrix}
            //  k_x & k_y & m
            // \end{pmatrix} \begin{pmatrix}
            //  k_{x,x} & k_{x,y} & m_x \\
            //  k_{y,x} & k_{y,y} & m_y \\
            //  0 & 0 & 1 \\
            // \end{pmatrix} = \begin{pmatrix}
            //  k_x k_{x,x} + k_y k_{y,x} & k_x k_{x,y} + k_y k_{y,y} & k_x m_x + k_y m_y + m \\
            // \end{pmatrix} $$
            public static XY2I operator *(XY2I a, XY2XY b)
            {
                int k_x = a.k_x * b.k_xx + a.k_y * b.k_yx;
                int k_y = a.k_x * b.k_xy + a.k_y * b.k_yy;
                int m = a.k_x * b.m_x + a.k_y * b.m_y + a.m;

                return new XY2I(k_x, k_y, m);
            }
        }

        /// <summary>
        /// 3x3 matrix transforming x,y coordinates to x,y coordinates.
        /// </summary>
        //tex: $$\begin{pmatrix} k_{x,x} & k_{x,y} & m_x \\ k_{y,x} & k_{y,y} & m_y \\ 0 & 0 & 1 \\ \end{pmatrix}$$
        private readonly struct XY2XY
        {
            public readonly int k_xx, k_xy, m_x, k_yx, k_yy, m_y;

            /// <summary>
            /// Identity transformation.
            /// </summary>
            //tex: $$\begin{pmatrix} 1 & 0 & 0 \\ 0 & 1 & 0 \\ 0 & 0 & 1 \\ \end{pmatrix}$$

            public static XY2XY Identity = new XY2XY(1, 0, 0, 0, 1, 0);

            /// <summary>
            /// Transpose transformation.
            /// </summary>
            //tex: $$\begin{pmatrix} 0 & 1 & 0 \\ 1 & 0 & 0 \\ 0 & 0 & 1 \\ \end{pmatrix}$$
            public static XY2XY Transpose = new XY2XY(0, 1, 0, 1, 0, 0);

            private XY2XY(int k_xx, int k_xy, int m_x, int k_yx, int k_yy, int m_y)
            {
                this.k_xx = k_xx;
                this.k_xy = k_xy;
                this.m_x = m_x;
                this.k_yx = k_yx;
                this.k_yy = k_yy;
                this.m_y = m_y;
            }

            /// <summary>
            /// Create scale transformation. Mirror by using negative scale values.
            /// </summary>
            /// <param name="s_x">Scaling in x direction.</param>
            /// <param name="s_y">Scaling in y direction.</param>
            /// <returns>Scale transformation.</returns>
            //tex: $$\begin{pmatrix} s_x & 0 & 0 \\ 0 & s_y & 0 \\ 0 & 0 & 1 \\ \end{pmatrix}$$
            public static XY2XY Scale(int s_x, int s_y) => new XY2XY(s_x, 0, 0, 0, s_y, 0);

            /// <summary>
            /// Create translation transformation.
            /// </summary>
            /// <param name="m_x">Translation in x direction.</param>
            /// <param name="m_y">Translation in y direction.</param>
            /// <returns>Translation transformation.</returns>
            //tex: $$\begin{pmatrix} 1 & 0 & m_x \\ 0 & 1 & m_y \\ 0 & 0 & 1 \\ \end{pmatrix}$$
            public static XY2XY Translation(int m_x, int m_y) => new XY2XY(1, 0, m_x, 0, 1, m_y);

            // NOTE: Do not implement operator *(XY2XY, XY2XY).
            // We're forcing left associativity since XY2I * XY2XY is faster than XY2XY * XY2XY. (10 arithmetic ops vs. 20)
        }

        private readonly IMemoryOwner<T> _memoryOwner;
        private readonly ReadOnlyMemory<T> _buffer;
        private readonly XY2I _transform;
        private readonly TransformedFrameBuffer _idFrameBuffer;

        /// <summary>
        /// Constructor for frame buffer.
        /// </summary>
        /// <param name="width">Width of frame buffer.</param>
        /// <param name="height">Height of frame buffer.</param>
        /// <param name="values">Element values as line outer, element inner sequence.</param>
        public FrameBuffer(int width, int height, IEnumerable<IEnumerable<T>> values)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            if (values is null)
                throw new ArgumentNullException(nameof(values));

            int bufferLength = width * height;

            // Allocate buffer from memory pool.
            var memoryOwner = MemoryPool<T>.Shared.Rent(bufferLength);

            // Slice off possible surplus from allocation.
            var buffer = memoryOwner.Memory.Slice(0, bufferLength);

            try
            {
                // Fill buffer with values or default value if values are exhausted
                int lineOffset = 0;

                foreach (var line in values.Take(height))
                {
                    var span = buffer.Span.Slice(lineOffset, width);

                    int elementOffset = 0;

                    foreach (var element in line.Take(width))
                        span[elementOffset++] = element;

                    // Fill rest of line with default value
                    span[elementOffset..].Fill(default!);

                    lineOffset += width;
                }

                // Fill rest of buffer with default value.
                buffer.Span[lineOffset..].Fill(default!);
            }
            catch
            {
                memoryOwner.Dispose();
                throw;
            }

            Width = width;
            Height = height;
            _memoryOwner = memoryOwner;
            _buffer = buffer;
            _transform = new XY2I(width);
            _idFrameBuffer = new TransformedFrameBuffer(buffer, width, height, _transform);
        }

        /// <summary>
        /// Transformed frame buffer. Shares memory buffer with its root frame buffer.
        /// </summary>
        private readonly struct TransformedFrameBuffer : IFrameBuffer<T>
        {
            private readonly ReadOnlyMemory<T> _buffer;
            private readonly XY2I _transform;

            public TransformedFrameBuffer(ReadOnlyMemory<T> buffer, int width, int height, XY2I transform)
            {
                _buffer = buffer;
                Width = width;
                Height = height;
                _transform = transform;
            }

            public int Width { get; }

            public int Height { get; }

            public T this[Index x, Index y]
            {
                get
                {
                    var coordinates = (x.GetOffset(Width), y.GetOffset(Height));
                    var index = _transform * coordinates;
                    return _buffer.Span[index];
                }
            }

            private TransformedFrameBuffer Window(int xOffset, int yOffset, int width, int height)
            {
                var translation = XY2XY.Translation(xOffset, yOffset);
                var mirror = XY2XY.Scale(Math.Sign(width), Math.Sign(height));

                var transform = _transform * translation * mirror;

                return new TransformedFrameBuffer(_buffer, width, height, transform);
            }

            public IFrameBuffer<T> this[Index x, Range ys]
            {
                get
                {
                    var (xOffset, width) = (x.GetOffset(Width), 1);
                    var (yOffset, height) = ys.GetOffsetAndLength(Height);

                    return Window(xOffset, yOffset, width, height);
                }
            }

            public IFrameBuffer<T> this[Range xs, Index y]
            {
                get
                {
                    var (xOffset, width) = xs.GetOffsetAndLength(Width);
                    var (yOffset, height) = (y.GetOffset(Height), 1);

                    return Window(xOffset, yOffset, width, height);
                }
            }

            public IFrameBuffer<T> this[Range xs, Range ys]
            {
                get
                {
                    var (xOffset, width) = xs.GetOffsetAndLength(Width);
                    var (yOffset, height) = ys.GetOffsetAndLength(Height);

                    return Window(xOffset, yOffset, width, height);
                }
            }

            public IFrameBuffer<T> Transpose => new TransformedFrameBuffer(_buffer, Height, Width, _transform * XY2XY.Transpose);
        }

        public int Width { get; }

        public int Height { get; }

        public T this[Index x, Index y] => _idFrameBuffer[x, y];

        public IFrameBuffer<T> this[Index x, Range ys] => _idFrameBuffer[x, ys];

        public IFrameBuffer<T> this[Range xs, Index y] => _idFrameBuffer[xs, y];

        public IFrameBuffer<T> this[Range xs, Range ys] => _idFrameBuffer[xs, ys];

        public IFrameBuffer<T> Transpose => _idFrameBuffer.Transpose;

        public void Dispose() => _memoryOwner.Dispose();
    }
}
