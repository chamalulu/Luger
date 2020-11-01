// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

// Disabled because tex: comments must start immediately after "//"
#pragma warning disable SA1005 // Single line comments should begin with single space

// Disable some StyleCop warnings interfering with my element variable names.
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1310 // Field names should not contain underscore

namespace Luger.FrameBuffer
{
    /// <summary>
    /// 1x3 matrix transforming x,y coordinates to one-dimensional offset-based index.
    /// </summary>
    //tex: $$\begin{pmatrix} k_x & k_y & m \\ \end{pmatrix}$$
    public readonly struct XY2I
    {
        /// <summary>
        /// x coefficient.
        /// </summary>
        public readonly int k_x;

        /// <summary>
        /// y coefficient.
        /// </summary>
        public readonly int k_y;

        /// <summary>
        /// Offset.
        /// </summary>
        public readonly int m;

        /// <summary>
        /// Initializes a new instance of the <see cref="XY2I"/> struct with k_x = 1, k_y = <paramref name="stride"/> and m = 0.
        /// </summary>
        /// <param name="stride">Width of domain plane (i.e. frame buffer).</param>
        //tex: $$\begin{pmatrix} 1 & stride & 0 \\ \end{pmatrix}$$
        public XY2I(int stride)
            : this(1, stride, 0)
        {
        }

        private XY2I(int k_x, int k_y, int m)
        {
            this.k_x = k_x;
            this.k_y = k_y;
            this.m = m;
        }

        /// <summary>
        /// Transform x,y coordinates to index.
        /// </summary>
        /// <param name="matrix">Transformation matrix.</param>
        /// <param name="coordinates">x,y coordinates. Third element in column vector is assumed 1.</param>
        /// <returns>One-dimensional index.</returns>
        //tex: $$matrix \cdot coordinates = \begin{pmatrix} k_x & k_y & m \\ \end{pmatrix} \begin{pmatrix} x \\ y \\ 1 \\ \end{pmatrix} = k_x x + k_y y + m$$
        public static int operator *(XY2I matrix, (int x, int y) coordinates)
            => (matrix.k_x * coordinates.x) + (matrix.k_y * coordinates.y) + matrix.m;

        /// <summary>
        /// Combine x,y coordinate to index transformation with x,y coordinate to x,y coordinate transformation.
        /// </summary>
        /// <param name="a">x,y coordinate to index transformation.</param>
        /// <param name="b">x,y coordinate to x,y coordinate transformation.</param>
        /// <returns>Combined x,y coordinate to index transformation.</returns>
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
            int k_x = (a.k_x * b.k_xx) + (a.k_y * b.k_yx);
            int k_y = (a.k_x * b.k_xy) + (a.k_y * b.k_yy);
            int m = (a.k_x * b.m_x) + (a.k_y * b.m_y) + a.m;

            return new XY2I(k_x, k_y, m);
        }
    }
}
