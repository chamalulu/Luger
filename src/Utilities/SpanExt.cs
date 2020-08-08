using System;

namespace Luger.Utilities
{

#pragma warning disable CA1032 // Implement standard exception constructors

    public class NonConsecutiveSpansException : InvalidOperationException { }

#pragma warning restore CA1032 // Implement standard exception constructors

    public static class SpanExt
    {
        /// <summary>
        /// Determine the offset of <paramref name="relSpan"/> span from <paramref name="baseSpan"/> span.
        /// </summary>
        /// <remarks>
        /// Offset is in terms of <typeparamref name="T"/> elements. To get byte offset, multiply by <c>sizeof(T)</c>.
        /// </remarks>
        public static unsafe long Offset<T>(this ReadOnlySpan<T> relSpan, ReadOnlySpan<T> baseSpan) where T : unmanaged
        {
            fixed (T* relPtr = relSpan, basePtr = baseSpan)
                return relPtr - basePtr;
        }

        /// <inheritdoc cref="Offset{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
        public static long Offset<T>(this Span<T> relSpan, ReadOnlySpan<T> baseSpan) where T : unmanaged =>
            ((ReadOnlySpan<T>)relSpan).Offset(baseSpan);

        /// <inheritdoc cref="Offset{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
        public static long Offset<T>(this ReadOnlySpan<T> relSpan, Span<T> baseSpan) where T : unmanaged =>
            (relSpan).Offset((ReadOnlySpan<T>)baseSpan);

        /// <inheritdoc cref="Offset{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
        public static long Offset<T>(this Span<T> relSpan, Span<T> baseSpan) where T : unmanaged =>
            ((ReadOnlySpan<T>)relSpan).Offset((ReadOnlySpan<T>)baseSpan);

        /// <summary>
        /// Determine if two spans are consecutive in memory (i.e. not overlapping and right starts directly after left ends)
        /// </summary>
        public static bool AreConsecutive<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right) where T : unmanaged =>
            right.Offset(left) == left.Length;

        /// <inheritdoc cref="AreConsecutive{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
        public static bool AreConsecutive<T>(Span<T> left, ReadOnlySpan<T> right) where T : unmanaged =>
            right.Offset(left) == left.Length;

        /// <inheritdoc cref="AreConsecutive{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
        public static bool AreConsecutive<T>(ReadOnlySpan<T> left, Span<T> right) where T : unmanaged =>
            right.Offset(left) == left.Length;

        /// <inheritdoc cref="AreConsecutive{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
        public static bool AreConsecutive<T>(Span<T> left, Span<T> right) where T : unmanaged =>
            right.Offset(left) == left.Length;

        /// <summary>
        /// Creates a read only span spanning given consecutive spans where one or both are read only.
        /// </summary>
        /// <remarks>
        /// This is implemented out of curiosity. It seems to work but I have no idea how crazy dangerous this code is. :)
        /// </remarks>
        public static unsafe ReadOnlySpan<T> ConcatConsecutive<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right) where T : unmanaged
        {
            if (AreConsecutive(left, right))
                fixed (T* lptr = left, rptr = right)
                    return new ReadOnlySpan<T>(lptr, left.Length + right.Length);
            else
                throw new NonConsecutiveSpansException();
        }

        /// <inheritdoc cref="ConcatConsecutive{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
        public static ReadOnlySpan<T> ConcatConsecutive<T>(Span<T> left, ReadOnlySpan<T> right) where T : unmanaged =>
            ConcatConsecutive((ReadOnlySpan<T>)left, right);

        /// <inheritdoc cref="ConcatConsecutive{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>
        public static ReadOnlySpan<T> ConcatConsecutive<T>(ReadOnlySpan<T> left, Span<T> right) where T : unmanaged =>
            ConcatConsecutive(left, (ReadOnlySpan<T>)right);

        /// <summary>
        /// Creates a span spanning given consecutive spans.
        /// </summary>
        /// <remarks>
        /// This is implemented out of curiosity. It seems to work but I have no idea how crazy dangerous this code is. :)
        /// </remarks>
        public static unsafe Span<T> ConcatConsecutive<T>(Span<T> left, Span<T> right) where T : unmanaged
        {
            if (AreConsecutive(left, right))
                fixed (T* lptr = left, rptr = right)
                    return new Span<T>(lptr, left.Length + right.Length);
            else
                throw new NonConsecutiveSpansException();
        }
    }
}
