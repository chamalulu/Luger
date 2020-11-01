// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Luger.FrameBuffer.Tests")]

namespace Luger.FrameBuffer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Lifetime manager for frame buffers.
    /// </summary>
    public sealed class FrameBufferManager : IDisposable
    {
        private readonly ISet<IDisposable> disposables;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameBufferManager"/> class.
        /// </summary>
        public FrameBufferManager() => this.disposables = new HashSet<IDisposable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameBufferManager"/> class. Used by tests.
        /// </summary>
        /// <param name="disposables">Mock disposables set provided by test code.</param>
        internal FrameBufferManager(ISet<IDisposable> disposables) => this.disposables = disposables;

        /// <summary>
        /// Create new frame buffer from sequence of sequence of values. Memory owner is managed by this manager.
        /// </summary>
        /// <typeparam name="T">Type of elements.</typeparam>
        /// <param name="width">Width of frame buffer.</param>
        /// <param name="height">Height of frame buffer.</param>
        /// <param name="values">Values to initialize frame buffer with.</param>
        /// <returns>New frame buffer.</returns>
        public FrameBuffer<T> CreateFrameBuffer<T>(int width, int height, IEnumerable<IEnumerable<T>> values)
        {
            // ArrayPool handles zero-length rents without array allocation.
            var root = new FrameBufferMemoryOwner<T>(width, height, values);

            this.Add(root);

            return root.FrameBuffer;
        }

        /// <summary>
        /// Create new frame buffer from jagged array of values. Memory owner is managed by this manager.
        /// </summary>
        /// <inheritdoc cref="CreateFrameBuffer{T}(int, int, IEnumerable{IEnumerable{T}})"/>
        public FrameBuffer<T> CreateFrameBuffer<T>(T[][] values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            var width = values.Length > 0 ? values.Max(row => row.Length) : 0;
            var height = values.Length;

            return this.CreateFrameBuffer(width, height, values);
        }

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

        /// <summary>
        /// Create new frame buffer from two-dimensional array of values. Memory owner is managed by this manager.
        /// </summary>
        /// <inheritdoc cref="CreateFrameBuffer{T}(int, int, IEnumerable{IEnumerable{T}})"/>
        public FrameBuffer<T> CreateFrameBuffer<T>(T[,] values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            var rows = values.GetLength(0);
            var columns = values.GetLength(1);

            var (width, height) = (columns, rows);

            var valuesQuery = from i in Enumerable.Range(values.GetLowerBound(0), rows)
                              select from j in Enumerable.Range(values.GetLowerBound(1), columns)
                                     select values[i, j];

            return this.CreateFrameBuffer(columns, rows, valuesQuery);
        }

#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional

        /// <summary>
        /// Create new frame buffer builder. Memory owner of built frame buffer is managed by this manager.
        /// </summary>
        /// <typeparam name="T">Type of elements.</typeparam>
        /// <param name="defaultValue">Default element value.</param>
        /// <returns>New <see cref="FrameBufferBuilder{T}"/>.</returns>
        public FrameBufferBuilder<T> CreateFrameBufferBuilder<T>(T defaultValue = default) => new FrameBufferBuilder<T>(this, defaultValue);

        /// <summary>
        /// Dispose all memory owners.
        /// </summary>
        public void Dispose()
        {
            // Defensive copy since disposing will alter the set of disposables.
            var disposables = this.disposables.ToArray();

            foreach (var d in disposables)
            {
                d.Dispose();
            }
        }

        /// <summary>
        /// Add frame buffer memory owner to set of disposables and register removal on disposed.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="frameBufferMemoryOwner"><see cref="FrameBufferMemoryOwner{T}"/> to add.</param>
        internal void Add<T>(FrameBufferMemoryOwner<T> frameBufferMemoryOwner)
        {
            if (this.disposables.Add(frameBufferMemoryOwner))
            {
                frameBufferMemoryOwner.Disposed += (_, fb) => _ = this.disposables.Remove(fb);
            }
        }
    }
}
