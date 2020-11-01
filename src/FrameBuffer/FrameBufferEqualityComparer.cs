// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

namespace Luger.FrameBuffer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="EqualityComparer{T}">EqualityComparer&lt;FrameBuffer&lt;T&gt;&gt;</see> wrapping
    ///  comparer given to <see cref="IStructuralEquatable"/> methods for boxing optimization when comparer
    ///  is <see cref="IEqualityComparer{T}">IEqualityComparer&lt;IFrameBuffer&lt;T&gt;&gt;</see>.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public class FrameBufferEqualityComparer<T> : EqualityComparer<FrameBuffer<T>>
    {
        private readonly IEqualityComparer structuralEqualityComparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameBufferEqualityComparer{T}"/> class.
        /// </summary>
        /// <param name="structuralEqualityComparer"><see cref="IEqualityComparer"/> passed to <see cref="IStructuralEquatable"/> methods.</param>
        public FrameBufferEqualityComparer(IEqualityComparer structuralEqualityComparer)
            => this.structuralEqualityComparer = structuralEqualityComparer;

        /// <inheritdoc/>
        public override bool Equals(FrameBuffer<T> x, FrameBuffer<T> y)
            => x!.Height == y!.Height && x.Width == y.Width
            && (this.structuralEqualityComparer is IEqualityComparer<T> tComparer
                ? Equals(x, y, tComparer) // Avoid boxing every element
                : Equals(x, y, this.structuralEqualityComparer));

        /// <inheritdoc/>
        public override int GetHashCode(FrameBuffer<T> obj)
            => this.structuralEqualityComparer is IEqualityComparer<T> tComparer
                ? GetHashCode(obj, tComparer) // Avoid boxing every element
                : GetHashCode(obj, this.structuralEqualityComparer);

        private static bool Equals(FrameBuffer<T> left, FrameBuffer<T> right, IEqualityComparer<T> comparer)
        {
            for (int y = 0; y < left.Height; y++)
            {
                for (int x = 0; x < left.Width; x++)
                {
                    if (!comparer.Equals(left[x, y], right[x, y]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Equals(FrameBuffer<T> left, FrameBuffer<T> right, IEqualityComparer comparer)
        {
            for (int y = 0; y < left.Height; y++)
            {
                for (int x = 0; x < left.Width; x++)
                {
                    if (!comparer.Equals(left[x, y], right[x, y]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static int GetHashCode(FrameBuffer<T> fb, IEqualityComparer<T> comparer)
        {
            HashCode hc = default;

            for (int y = 0; y < fb.Height; y++)
            {
                for (int x = 0; x < fb.Width; x++)
                {
                    hc.Add(fb[x, y], comparer);
                }
            }

            return hc.ToHashCode();
        }

        private static int GetHashCode(FrameBuffer<T> fb, IEqualityComparer comparer)
        {
            HashCode hc = default;

            for (int y = 0; y < fb.Height; y++)
            {
                for (int x = 0; x < fb.Width; x++)
                {
                    var e = fb[x, y];

                    hc.Add(e is null ? 0 : comparer.GetHashCode(e));
                }
            }

            return hc.ToHashCode();
        }
    }
}
