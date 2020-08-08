using System;

using Xunit;

namespace Luger.Utilities.Tests
{
    public class SpanExtTests
    {
        [Theory]
        [InlineData(0, 5, -5)]
        [InlineData(0, 0, 0)]
        [InlineData(5, 0, 5)]
        public void OffsetTest(int relStart, int baseStart, long expected)
        {
            int[] buffer = new int[10];

            ReadOnlySpan<int> relSpan = buffer.AsSpan(relStart..);
            ReadOnlySpan<int> baseSpan = buffer.AsSpan(baseStart..);

            long actual = relSpan.Offset(baseSpan);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(5, 5, true)]
        [InlineData(4, 6, false)]
        public void AreConsecutiveTest(int leftStop, int rightStart, bool expected)
        {
            int[] buffer = new int[10];

            ReadOnlySpan<int> left = buffer.AsSpan(..leftStop);
            ReadOnlySpan<int> right = buffer.AsSpan(rightStart..);

            bool actual = SpanExt.AreConsecutive(left, right);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ConcatConsecutiveTest()
        {
            int[] buffer = new int[10];

            Span<int> left = buffer.AsSpan(..5);
            Span<int> right = buffer.AsSpan(5..);

            Span<int> actual = SpanExt.ConcatConsecutive(left, right);

            Span<int> expected = buffer.AsSpan();

            Assert.True(expected == actual);
        }

        [Fact]
        public void ConcatConsecutiveTestThrows() => Assert.Throws<NonConsecutiveSpansException>(() =>
        {
            int[] buffer = new int[10];

            Span<int> left = buffer.AsSpan(..4);
            Span<int> right = buffer.AsSpan(6..);

            _ = SpanExt.ConcatConsecutive(left, right);
        });

        [Fact]
        public void ConcatConsecutiveTestReadOnly()
        {
            int[] buffer = new int[10];

            ReadOnlySpan<int> left = buffer.AsSpan(..5);
            ReadOnlySpan<int> right = buffer.AsSpan(5..);

            ReadOnlySpan<int> actual = SpanExt.ConcatConsecutive(left, right);

            ReadOnlySpan<int> expected = buffer.AsSpan();

            Assert.True(expected == actual);
        }

        [Fact]
        public void ConcatConsecutiveTestReadOnlyThrows() => Assert.Throws<NonConsecutiveSpansException>(() =>
        {
            int[] buffer = new int[10];

            ReadOnlySpan<int> left = buffer.AsSpan(..4);
            ReadOnlySpan<int> right = buffer.AsSpan(6..);

            _ = SpanExt.ConcatConsecutive(left, right);
        });
    }
}
