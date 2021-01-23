using System;

namespace Luger.Utilities
{
    public static class IndexRangeExtensions
    {
        /// <summary>
        /// Calculates the zero-based offset given the length of the object indexed and checks for out of range conditions.
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="length">The length of the object indexed.</param>
        /// <param name="paramName">Parameter name to use with <see cref="ArgumentOutOfRangeException"/>.</param>
        /// <returns>The offset.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if calculated offset is less than zero or greater than or equal to <paramref name="length"/>.</exception>
        public static int GetCheckedOffset(this Index index, int length, string? paramName = null)
        {
            var offset = index.GetOffset(length);

            if (offset < 0 || offset >= length)
                throw new ArgumentOutOfRangeException(paramName);

            return offset;
        }

        /// <summary>
        /// Shift an index by given offset. <see cref="Index.IsFromEnd"/> is preserved.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>New shifted index.</returns>
        public static Index Shift(this Index index, int offset)
        {
            int value = index.IsFromEnd
                ? index.Value - offset
                : index.Value + offset;

            return new Index(value, index.IsFromEnd);
        }

        /// <summary>
        /// Convert a <see cref="Index"/> to a <see cref="Range"/> with length 1. <see cref="Index.IsFromEnd"/> of <paramref name="index"/> is preserved in both <see cref="Range.Start"/> and <see cref="Range.End"/>.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>New range of length 1.</returns>
        public static Range ToRange(this Index index) => new Range(index, index.Shift(1));

        /// <summary>
        /// Calculates the start offset and length of the range object using a collection length and checks for out of range conditions.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="length">The length of the object that the range will be used with.</param>
        /// <param name="paramName">Parameter name to use with <see cref="ArgumentOutOfRangeException"/>.</param>
        /// <returns>The start offset and the length of the range.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if calculated offset and length is outside the bounds of the given length. [0 .. length)</exception>
        public static (int Offset, int Length) GetCheckedOffsetAndLength(this Range range, int length, string? paramName = null)
        {
            _ = range.Start.GetCheckedOffset(length, paramName);
            _ = range.End.GetCheckedOffset(length, paramName);

            return range.GetOffsetAndLength(length);
        }

        /// <summary>
        /// Shift a range by given offset. <see cref="Index.IsFromEnd"/> of <see cref="Range.Start"/> and <see cref="Range.End"/> are preserved.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Range Shift(this Range range, int offset) => new Range(range.Start.Shift(offset), range.End.Shift(offset));

        /// <summary>
        /// Extend a range by given length. <see cref="Index.IsFromEnd"/> of <paramref name="index"/> is preserved in both <see cref="Range.Start"/> and <see cref="Range.End"/>.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="length">The length to extend range by.</param>
        /// <returns>New extended range.</returns>
        public static Range Extend(this Range range, int length) => new Range(range.Start, range.End.Shift(length));
    }
}
