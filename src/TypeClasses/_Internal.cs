// Copyright © 2020 Henrik Lundberg. Licensed to you under the MIT license.

#pragma warning disable SA1604 // Element documentation should have summary
#pragma warning disable SA1615 // Element return value should be documented
#pragma warning disable SA1618 // Generic type parameters should be documented

namespace Luger.TypeClasses
{
    // Only used internally for XML documentation inheritance.

    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    internal delegate TS NoncommutativeBinaryOperation<TS>(TS left, TS right);

    /// <param name="x">One operand.</param>
    /// <param name="y">Other operand.</param>
    internal delegate TS CommutativeBinaryOperation<TS>(TS x, TS y);
}
