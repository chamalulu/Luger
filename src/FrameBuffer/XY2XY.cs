// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

// Disabled because tex: comments must start immediately after "//"
#pragma warning disable SA1005 // Single line comments should begin with single space

// Disable some StyleCop warnings interfering with my element variable names.
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1308 // Variable names should not be prefixed

namespace Luger.FrameBuffer
{
    /// <summary>
    /// 3x3 matrix transforming x,y coordinates to x,y coordinates.
    /// </summary>
    //tex: $$\begin{pmatrix} k_{x,x} & k_{x,y} & m_x \\ k_{y,x} & k_{y,y} & m_y \\ 0 & 0 & 1 \\ \end{pmatrix}$$
    public readonly struct XY2XY
    {
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

        /// <summary>
        /// xx coefficient.
        /// </summary>
        public readonly int k_xx;

        /// <summary>
        /// xy coefficient.
        /// </summary>
        public readonly int k_xy;

        /// <summary>
        /// x translation.
        /// </summary>
        public readonly int m_x;

        /// <summary>
        /// yx coefficient.
        /// </summary>
        public readonly int k_yx;

        /// <summary>
        /// yy coefficient.
        /// </summary>
        public readonly int k_yy;

        /// <summary>
        /// y translation.
        /// </summary>
        public readonly int m_y;

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
}
