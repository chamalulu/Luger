// <copyright file="Vector.cs" company="PlaceholderCompany">
// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.
// </copyright>

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers

namespace Luger.Matrices
{
    using System;
    using Luger.Utilities;

    /// <summary>
    /// Factory and extension methods for vectors.
    /// </summary>
    public static class Vector
    {
        public static IVector<T> FromFrameBuffer<T>(IFrameBuffer<T> frameBuffer)
            => frameBuffer.Width * frameBuffer.Height == 1
                ? new SingletonVector<T>(frameBuffer[0, 0])
                : new Vector<T>(frameBuffer);

        public static IVector<T, TI> FromFrameBuffer<T, TI>(IFrameBuffer<T> frameBuffer)
            => frameBuffer.Width * frameBuffer.Height == 1
                ? new SingletonVector<T, TI>(fra)
    }
}
