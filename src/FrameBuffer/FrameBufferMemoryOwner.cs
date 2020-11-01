// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

namespace Luger.FrameBuffer
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Memory owner for frame buffer. Instances own their memory buffer which is shared by all its descendant <see cref="FrameBuffer{T}"/>s.
    /// Make sure instances are properly disposed and follow the memory usage guidelines as specified in
    /// <a href="https://docs.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines">
    /// Memory&lt;T&gt; and Span&lt;T&gt; usage guidelines
    /// </a>.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    internal sealed class FrameBufferMemoryOwner<T> : IDisposable
    {
        private IMemoryOwner<T>? memoryOwner;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameBufferMemoryOwner{T}"/> class.
        /// </summary>
        /// <param name="width">Width of frame buffer.</param>
        /// <param name="height">Height of frame buffer.</param>
        /// <param name="values">Element values as line outer, element inner sequence.</param>
        public FrameBufferMemoryOwner(int width, int height, IEnumerable<IEnumerable<T>> values)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var bufferLength = checked(width * height);

            // Allocate buffer from memory pool.
            var memoryOwner = MemoryPool<T>.Shared.Rent(bufferLength);

            // Slice off possible surplus from allocation.
            var buffer = memoryOwner.Memory.Slice(0, bufferLength);

            try
            {
                // Fill buffer with values or default value if values are exhausted
                var lineOffset = 0;

                foreach (var line in values.Take(height))
                {
                    var span = buffer.Span.Slice(lineOffset, width);

                    var elementOffset = 0;

                    foreach (var element in line.Take(width))
                    {
                        span[elementOffset++] = element;
                    }

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

            var transform = new XY2I(width);

            this.memoryOwner = memoryOwner;
            this.FrameBuffer = new FrameBuffer<T>(buffer, width, height, transform);
        }

        /// <summary>
        /// Raised when frame buffer memory owner is disposed.
        /// </summary>
        public event EventHandler<FrameBufferMemoryOwner<T>>? Disposed;

        /// <summary>
        /// Gets a frame buffer with "id" transformation. I.e. transform (x, y) coordinates to buffer index with stride = width.
        /// </summary>
        public FrameBuffer<T> FrameBuffer { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.memoryOwner is null)
            {
                return;
            }

            this.memoryOwner.Dispose();
            this.memoryOwner = null;
            this.Disposed?.Invoke(this, this);
        }
    }
}
