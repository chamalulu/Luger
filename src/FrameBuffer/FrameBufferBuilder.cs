// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

namespace Luger.FrameBuffer
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Builder for frame buffer. Created by <see cref="FrameBufferManager"/>.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public class FrameBufferBuilder<T>
    {
        private readonly List<BuildEntry> buildEntries;
        private readonly FrameBufferManager manager;
        private readonly T defaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameBufferBuilder{T}"/> class.
        /// </summary>
        /// <param name="manager">Frame buffer manager to register with.</param>
        /// <param name="defaultValue">Default element value.</param>
        internal FrameBufferBuilder(FrameBufferManager manager, T defaultValue)
        {
            this.buildEntries = new List<BuildEntry>();
            this.manager = manager;
            this.defaultValue = defaultValue;
        }

        /// <summary>
        /// Delegate for hit detection.
        /// </summary>
        /// <param name="x">x coordinate.</param>
        /// <param name="y">y coordinate.</param>
        /// <returns>True if coordinates (<paramref name="x"/>, <paramref name="y"/>) hits build entry; otherwise, false.</returns>
        public delegate bool HitFunc(int x, int y);

        /// <summary>
        /// Delegate for mapping coordinates to element value.
        /// </summary>
        /// <param name="x">x coordinate.</param>
        /// <param name="y">y coordinate.</param>
        /// <returns>Element value at coordinates (<paramref name="x"/>, <paramref name="y"/>).</returns>
        public delegate T ValueFunc(int x, int y);

        /// <summary>
        /// Add build entry of given hit and value functions.
        /// </summary>
        /// <param name="hit">Predicate for hit detection.</param>
        /// <param name="value">Map hit coordinates to element value.</param>
        /// <returns>This <see cref="FrameBufferBuilder{T}"/> for fluent build.</returns>
        public FrameBufferBuilder<T> Add(HitFunc hit, ValueFunc value)
        {
            this.buildEntries.Add(new BuildEntry(hit, value));

            return this;
        }

        /// <summary>
        /// Add build entry of given hit function and static value.
        /// </summary>
        /// <param name="value">Value of all hit elements.</param>
        /// <inheritdoc cref="Add(HitFunc, ValueFunc)"/>
        public FrameBufferBuilder<T> Add(HitFunc hit, T value) => this.Add(hit, (x, y) => value);

        /// <summary>
        /// Add build entry of given value func for all elements.
        /// </summary>
        /// <inheritdoc cref="Add(HitFunc, ValueFunc)"/>
        public FrameBufferBuilder<T> Add(ValueFunc value) => this.Add((x, y) => true, value);

        /// <summary>
        /// Add element of single value.
        /// </summary>
        /// <param name="x">x coordinate of element.</param>
        /// <param name="y">y coordinate of element.</param>
        /// <param name="value">Value of element.</param>
        /// <returns>This <see cref="FrameBufferBuilder{T}"/> for fluent build.</returns>
        public FrameBufferBuilder<T> Element(int x, int y, T value)
        {
            bool Hit(int hx, int hy) => hx == x && hy == y;

            return this.Add(Hit, value);
        }

        /// <summary>
        /// Add rectangular region of single value.
        /// </summary>
        /// <param name="x">x coordinate of left edge.</param>
        /// <param name="y">y coordinate of top edge.</param>
        /// <param name="width">Width of rectangle.</param>
        /// <param name="height">Height of rectangle.</param>
        /// <param name="value">Value of elements in rectange.</param>
        /// <returns>This <see cref="FrameBufferBuilder{T}"/> for fluent build.</returns>
        public FrameBufferBuilder<T> Rect(int x, int y, int width, int height, T value)
        {
            bool Hit(int hx, int hy)
                => hx >= x
                && hy >= y
                && hx - x < width
                && hy - y < height;

            return this.Add(Hit, value);
        }

        /// <summary>
        /// Add diagonal element values with optional offset.
        /// </summary>
        /// <param name="value">Value of elements in diagonal.</param>
        /// <param name="offset">Offset from main diagonal. Positive offset moves diagonal into upper triangular area.</param>
        /// <inheritdoc cref="Add(HitFunc, ValueFunc)"/>
        public FrameBufferBuilder<T> Diagonal(T value, int offset = 0)
        {
            bool Hit(int hx, int hy) => hx == hy + offset;

            return this.Add(Hit, value);
        }

        /// <summary>
        /// Create frame buffer of given size from builder.
        /// </summary>
        /// <param name="width">Width of frame buffer.</param>
        /// <param name="height">Height of frame buffer.</param>
        /// <returns>Frame buffer.</returns>
        public FrameBuffer<T> ToFrameBuffer(int width, int height)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            var root = new FrameBufferMemoryOwner<T>(width, height, this.GetValues(width, height));

            this.manager.Add(root);

            return root.FrameBuffer;
        }

        /// <summary>
        /// Create read only frame buffer of given size from builder.
        /// </summary>
        /// <returns>Read only frame buffer.</returns>
        /// <inheritdoc cref="ToFrameBuffer"/>
        public ReadOnlyFrameBuffer<T> ToReadOnlyFrameBuffer(int width, int height) => this.ToFrameBuffer(width, height);

        private IEnumerable<IEnumerable<T>> GetValues(int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                yield return this.Row(y, width);
            }
        }

        private IEnumerable<T> Row(int y, int width)
        {
            for (int x = 0; x < width; x++)
            {
                var entry = this.buildEntries.FindLast(e => e.Hit(x, y));

                yield return entry.Equals(BuildEntry.Empty)
                    ? this.defaultValue
                    : entry.Value(x, y);
            }
        }

        private readonly struct BuildEntry
        {
            public static readonly BuildEntry Empty = default;

            public readonly HitFunc Hit;
            public readonly ValueFunc Value;

            public BuildEntry(HitFunc hit, ValueFunc value)
            {
                this.Hit = hit;
                this.Value = value;
            }
        }
    }
}
