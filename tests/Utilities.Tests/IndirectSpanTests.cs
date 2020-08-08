using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Luger.Utilities.Tests
{
    public class IndirectSpanTests
    {
        private static Span<char> NonEmptySpan(string s) => s.ToCharArray().AsSpan();

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void IsEmptyTest(bool leftEmpty, bool rightEmpty, bool expected)
        {
            var left = leftEmpty ? Span<char>.Empty : NonEmptySpan("left");
            var right = rightEmpty ? Span<char>.Empty : NonEmptySpan("right");

            var actual = new IndirectSpan<char>(left, right).IsEmpty;

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void IsHalfEmptyTest(bool leftEmpty, bool rightEmpty, bool expected)
        {
            var left = leftEmpty ? Span<char>.Empty : NonEmptySpan("left");
            var right = rightEmpty ? Span<char>.Empty : NonEmptySpan("right");

            var actual = new IndirectSpan<char>(left, right).IsHalfEmpty;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AsSpanTestEmpty()
        {
            var left = Span<char>.Empty;
            var right = Span<char>.Empty;

            var actual = new IndirectSpan<char>(left, right).AsSpan();

            Assert.True(actual.IsEmpty);
        }

        [Fact]
        public void AsSpanTestLeft()
        {
            var left = NonEmptySpan("left");
            var right = Span<char>.Empty;

            var actual = new IndirectSpan<char>(left, right).AsSpan();

            Assert.True(left == actual);
        }

        [Fact]
        public void AsSpanTestRight()
        {
            var left = Span<char>.Empty;
            var right = NonEmptySpan("right");

            var actual = new IndirectSpan<char>(left, right).AsSpan();

            Assert.True(right == actual);
        }

        [Fact]
        public void AsSpanTestThrows()
        {
            static void testCode() => new IndirectSpan<char>(NonEmptySpan("left"), NonEmptySpan("right")).AsSpan();

            Assert.Throws<InvalidOperationException>(testCode);
        }

        [Fact]
        public void IndexerTestReadLeft()
        {
            var a = new[] { 42 };
            var b = new[] { 128 };

            var target = new IndirectSpan<int>(a.AsSpan(), b.AsSpan());

            var actual = target[0];

            Assert.Equal(a[0], actual);
        }

        [Fact]
        public void IndexerTestReadRight()
        {
            var a = new[] { 42 };
            var b = new[] { 128 };

            var target = new IndirectSpan<int>(a.AsSpan(), b.AsSpan());

            var actual = target[1];

            Assert.Equal(b[0], actual);
        }

        [Fact]
        public void IndexerTestWriteLeft()
        {
            var a = new[] { 42 };
            var b = new[] { 128 };
            var expected = 213;

            var target = new IndirectSpan<int>(a.AsSpan(), b.AsSpan());

            target[0] = expected;

            Assert.Equal(expected, a[0]);
        }

        [Fact]
        public void IndexerTestWriteRight()
        {
            var a = new[] { 42 };
            var b = new[] { 128 };
            var expected = 213;

            var target = new IndirectSpan<int>(a.AsSpan(), b.AsSpan());

            target[1] = expected;

            Assert.Equal(expected, b[0]);
        }

        [Fact]
        public void ClearTest()
        {
            var a = new[] { 42 };
            var b = new[] { 128 };

            var target = new IndirectSpan<int>(a.AsSpan(), b.AsSpan());

            target.Clear();

            Assert.Equal(0, a[0]);
            Assert.Equal(0, b[0]);
        }

        public static object?[][] TryCopyFromTestData() => new (Range left, Range right, Range source, IEnumerable<int>? expected)[]
        {
            ( ..2, 2..4, 4.. , null),   // Source too long
            ( ..2, 2..5, 5.. , new [] { 5, 6, 7, 8, 9, 5, 6, 7, 8, 9 }),    // No overlap
            (4..7, 7.. ,  ..6, new [] { 0, 1, 2, 3, 0, 1, 2, 3, 4, 5 }),    // Left overlap
            ( ..3, 3..6, 4.. , new [] { 4, 5, 6, 7, 8, 9, 6, 7, 8, 9 }),    // Right overlap
            (5.. ,  ..5,  .. , new [] { 5, 6, 7, 8, 9, 0, 1, 2, 3, 4 })     // Both overlap
        }.Select(t => new object?[] { t.left, t.right, t.source, t.expected }).ToArray();

        [Theory]
        [MemberData(nameof(TryCopyFromTestData))]
        public void TryCopyFromTest(Range left, Range right, Range source, IEnumerable<int>? expected)
        {
            var seq = Enumerable.Range(0, 10);
            var buffer = seq.ToArray();
            var span = buffer.AsSpan();

            var target = new IndirectSpan<int>(span[left], span[right]);

            if (target.TryCopyFrom(span[source]))
                Assert.Equal(expected, buffer);
            else
            {
                Assert.Null(expected);
                Assert.Equal(seq, buffer);
            }
        }

        [Fact]
        public void CopyFromTestLongSource() => Assert.Throws<SourceTooLongException>(() =>
        {
            var buffer = Enumerable.Range(0, 10).ToArray();
            var span = buffer.AsSpan();

            var target = new IndirectSpan<int>(span[..2], span[2..4]);

            target.CopyFrom(span[4..]);
        });

        public static object[][] SliceStartTestData() => new (int start, bool isHalfEmpty, bool isEmpty)[]
        {
            ( 0, false, false),
            ( 5, true, false),
            (10, false, true)
        }.Select(t => new object[] { t.start, t.isHalfEmpty, t.isEmpty }).ToArray();

        [Theory]
        [MemberData(nameof(SliceStartTestData))]
        public void SliceStartTest(int start, bool isHalfEmpty, bool isEmpty)
        {
            var buffer = Enumerable.Range(0, 10).ToArray();
            var span = buffer.AsSpan();

            var target = new IndirectSpan<int>(span[..5], span[5..]);

            var actual = target.Slice(start);

            Assert.Equal(Enumerable.Range(start, 10 - start), actual.ToArray());
            Assert.Equal(isHalfEmpty, actual.IsHalfEmpty);
            Assert.Equal(isEmpty, actual.IsEmpty);
        }

        public static object[][] SliceStartLengthTestData() => new (int start, int length, bool isHalfEmpty, bool isEmpty)[]
        {
            ( 0,  0, false, true),
            ( 0,  5,  true, false),
            ( 0, 10, false, false),
            ( 5,  0, false, true),
            ( 5,  5,  true, false),
            (10,  0, false, true)
        }.Select(t => new object[] { t.start, t.length, t.isHalfEmpty, t.isEmpty }).ToArray();

        [Theory]
        [MemberData(nameof(SliceStartLengthTestData))]
        public void SliceStartLengthTest(int start, int length, bool isHalfEmpty, bool isEmpty)
        {
            var buffer = Enumerable.Range(0, 10).ToArray();
            var span = buffer.AsSpan();

            var target = new IndirectSpan<int>(span[..5], span[5..]);

            var actual = target.Slice(start, length);

            Assert.Equal(Enumerable.Range(start, length), actual.ToArray());
            Assert.Equal(isHalfEmpty, actual.IsHalfEmpty);
            Assert.Equal(isEmpty, actual.IsEmpty);
        }
    }

    public class IndirectReadOnlySpanTests
    {
        private static ReadOnlySpan<char> NonEmptyReadOnlySpan(string s) => s.AsSpan();

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void IsEmptyTest(bool leftEmpty, bool rightEmpty, bool expected)
        {
            var left = leftEmpty ? ReadOnlySpan<char>.Empty : NonEmptyReadOnlySpan("left");
            var right = rightEmpty ? ReadOnlySpan<char>.Empty : NonEmptyReadOnlySpan("right");

            var actual = new IndirectReadOnlySpan<char>(left, right).IsEmpty;

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void IsHalfEmptyTest(bool leftEmpty, bool rightEmpty, bool expected)
        {
            var left = leftEmpty ? ReadOnlySpan<char>.Empty : NonEmptyReadOnlySpan("left");
            var right = rightEmpty ? ReadOnlySpan<char>.Empty : NonEmptyReadOnlySpan("right");

            var actual = new IndirectReadOnlySpan<char>(left, right).IsHalfEmpty;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AsReadOnlySpanTestEmpty()
        {
            var left = ReadOnlySpan<char>.Empty;
            var right = ReadOnlySpan<char>.Empty;

            var actual = new IndirectReadOnlySpan<char>(left, right).AsReadOnlySpan();

            Assert.True(actual.IsEmpty);
        }

        [Fact]
        public void AsReadOnlySpanTestLeft()
        {
            var left = NonEmptyReadOnlySpan("left");
            var right = ReadOnlySpan<char>.Empty;

            var actual = new IndirectReadOnlySpan<char>(left, right).AsReadOnlySpan();

            Assert.True(left == actual);
        }

        [Fact]
        public void AsReadOnlySpanTestRight()
        {
            var left = ReadOnlySpan<char>.Empty;
            var right = NonEmptyReadOnlySpan("right");

            var actual = new IndirectReadOnlySpan<char>(left, right).AsReadOnlySpan();

            Assert.True(right == actual);
        }

        [Fact]
        public void AsReadOnlySpanTestThrows()
        {
            static void testCode() => new IndirectReadOnlySpan<char>(NonEmptyReadOnlySpan("left"), NonEmptyReadOnlySpan("right")).AsReadOnlySpan();

            Assert.Throws<InvalidOperationException>(testCode);
        }

        [Fact]
        public void IndexerTestReadLeft()
        {
            var a = new[] { 42 };
            var b = new[] { 128 };

            var target = new IndirectReadOnlySpan<int>(a.AsSpan(), b.AsSpan());

            var actual = target[0];

            Assert.Equal(a[0], actual);
        }

        [Fact]
        public void IndexerTestReadRight()
        {
            var a = new[] { 42 };
            var b = new[] { 128 };

            var target = new IndirectReadOnlySpan<int>(a.AsSpan(), b.AsSpan());

            var actual = target[1];

            Assert.Equal(b[0], actual);
        }

        public static object?[][] TryCopyToTestData() => new (Range left, Range right, Range destination, IEnumerable<int>? expected)[]
        {
            ( ..3, 3..6, 6.. , null),   // Destination too short
            ( ..2, 2..5, 5.. , new [] { 0, 1, 2, 3, 4, 0, 1, 2, 3, 4 }),    // No overlap
            (4..7, 7.. ,  ..6, new [] { 4, 5, 6, 7, 8, 9, 6, 7, 8, 9 }),    // Left overlap
            ( ..3, 3..6, 4.. , new [] { 0, 1, 2, 3, 0, 1, 2, 3, 4, 5 }),    // Right overlap
            (5.. ,  ..5,  .. , new [] { 5, 6, 7, 8, 9, 0, 1, 2, 3, 4 })     // Both overlap
        }.Select(t => new object?[] { t.left, t.right, t.destination, t.expected }).ToArray();

        [Theory]
        [MemberData(nameof(TryCopyToTestData))]
        public void TryCopyToTest(Range left, Range right, Range destination, IEnumerable<int>? expected)
        {
            var seq = Enumerable.Range(0, 10);
            var buffer = seq.ToArray();
            var span = buffer.AsSpan();

            var target = new IndirectReadOnlySpan<int>(span[left], span[right]);

            if (target.TryCopyTo(span[destination]))
                Assert.Equal(expected, buffer);
            else
            {
                Assert.Null(expected);
                Assert.Equal(seq, buffer);
            }
        }

        [Fact]
        public void CopyToTestShortDestination() => Assert.Throws<DestinationTooShortException>(() =>
        {
            var buffer = Enumerable.Range(0, 10).ToArray();
            var span = buffer.AsSpan();

            var target = new IndirectReadOnlySpan<int>(span[..3], span[3..6]);

            target.CopyTo(span[6..]);
        });

        //public static object[][] SliceStartTestData() => new (int start, bool isHalfEmpty, bool isEmpty)[]
        //{
        //    ( 0, false, false),
        //    ( 5, true, false)
        //}

        [Theory]
        [MemberData(nameof(IndirectSpanTests.SliceStartTestData), MemberType = typeof(IndirectSpanTests))]
        public void SliceStartTest(int start, bool isHalfEmpty, bool isEmpty)
        {
            var buffer = Enumerable.Range(0, 10).ToArray();
            var span = buffer.AsSpan();

            var target = new IndirectReadOnlySpan<int>(span[..5], span[5..]);

            var actual = target.Slice(start);

            Assert.Equal(Enumerable.Range(start, 10 - start), actual.ToArray());
            Assert.Equal(isHalfEmpty, actual.IsHalfEmpty);
            Assert.Equal(isEmpty, actual.IsEmpty);
        }

        [Theory]
        [MemberData(nameof(IndirectSpanTests.SliceStartLengthTestData), MemberType = typeof(IndirectSpanTests))]
        public void SliceStartLengthTest(int start, int length, bool isHalfEmpty, bool isEmpty)
        {
            var buffer = Enumerable.Range(0, 10).ToArray();
            var span = buffer.AsSpan();

            var target = new IndirectReadOnlySpan<int>(span[..5], span[5..]);

            var actual = target.Slice(start, length);

            Assert.Equal(Enumerable.Range(start, length), actual.ToArray());
            Assert.Equal(isHalfEmpty, actual.IsHalfEmpty);
            Assert.Equal(isEmpty, actual.IsEmpty);
        }

        [Fact]
        public void ToArrayTestEmpty()
        {
            var buffer = new[] { 0, 1, 2, 3 };
            var span = buffer.AsSpan();

            var target = new IndirectReadOnlySpan<int>(span[..2], span[2..]);

            var actual = target.ToArray();

            Assert.Equal(buffer, actual);
        }
    }
}
