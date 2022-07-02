using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using static Luger.Functional.Maybe;

namespace Luger.Functional.Tests
{
    public class EnumerableExtTests
    {
        [Fact]
        public void MapTest()
        {
            // Given
            var source = new[] { 1, 2, 3 };
            var expected = new[] { -2, -3, -4 };

            // When
            var actual = source.Map(i => ~i);

            // Then
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ApplyTest()
        {
            // Given
            var source = new[] { 1, 2, 3 };
            var funcs = new Func<int, int>[] { i => i, i => i * 3, i => i * i };
            var expected = new[] { 1, 2, 3, 3, 6, 9, 1, 4, 9 };

            // When
            var actual = funcs.Apply(source);

            // Then
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BindTest()
        {
            // Given
            var source = new[] { 1, 2, 3 };
            var expected = new[] { 1, 2, 2, 3, 3, 3 };

            // When
            var actual = source.Bind(i => Enumerable.Repeat(i, i));

            // Then
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new int[0], "none")]
        [InlineData(new[] { 1, 2, 3 }, "some #3")]
        public void MatchNoneSomeTest(IEnumerable<int> source, string expected)
        {
            var actual = source.Match(() => "none", ts => $"some #{ts.Count()}");
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new int[0], "none")]
        [InlineData(new[] { 1, 2, 3 }, "some 1:#2")]
        public void MatchNoneSomeHeadTailTest(IEnumerable<int> source, string expected)
        {
            var actual = source.Match(() => "none", (t, ts) => $"some {t}:#{ts.Count()}");
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new int[0], "none")]
        [InlineData(new[] { 1 }, "single 1")]
        [InlineData(new[] { 1, 2, 3 }, "some #3")]
        public void MatchNoneSingleSomeTest(IEnumerable<int> source, string expected)
        {
            var actual = source.Match(() => "none", t => $"single {t}", ts => $"some #{ts.Count()}");
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new int[0], "none")]
        [InlineData(new[] { 1 }, "single 1")]
        [InlineData(new[] { 1, 2, 3 }, "some 1:#2")]
        public void MatchNoneSingleSomeHeadTailTest(IEnumerable<int> source, string expected)
        {
            var actual = source.Match(() => "none", t => $"single {t}", (t, ts) => $"some {t}:#{ts.Count()}");
            Assert.Equal(expected, actual);
        }

        //private class MaybeEnumerableEqualityComparer<T> : IEqualityComparer<Maybe<IEnumerable<T>>>
        //{
        //    public bool Equals(Maybe<IEnumerable<T>> x, Maybe<IEnumerable<T>> y) =>
        //        x.Match(
        //            some: xs => y.Match(
        //                some: ys => xs.SequenceEqual(ys),
        //                none: () => false),
        //            none: () => !y.IsSome);

        //    public int GetHashCode(Maybe<IEnumerable<T>> obj) => throw new NotImplementedException();
        //}

        public static IEnumerable<object[]> HeadTestData => new[]
        {
            new object[] { Array.Empty<int>(), None<int>() },
            new object[] { new[] { 1 }, Some(1) }
        };

        [Theory]
        [MemberData(nameof(HeadTestData))]
        public void HeadTest(IEnumerable<int> source, Maybe<int> expected)
        {
            var actual = source.Head();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })]
        [InlineData(2, new[] { 0, 2, 4, 6, 8 })]
        [InlineData(3, new[] { 0, 3, 6, 9 })]
        public void EveryNthTest(ulong n, IEnumerable<int> expected)
        {
            var actual = Enumerable.Range(0, 10).EveryNth(n);
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> PairwiseTestData => new[]
        {
            new object[] { Array.Empty<int>()  , Array.Empty<(int, int)>() },
            new object[] { new[] { 0 }         , Array.Empty<(int, int)>() },
            new object[] { new[] { 0, 1 }      , new[] { (0, 1) }          },
            new object[] { new[] { 0, 1, 2 }   , new[] { (0, 1) }          },
            new object[] { new[] { 0, 1, 2, 3 }, new[] { (0, 1), (2, 3) }  }
        };

        [Theory]
        [MemberData(nameof(PairwiseTestData))]
        public void SequentialPairsTest(IEnumerable<int> source, IEnumerable<(int, int)> expected)
        {
            var actual = source.SequentialPairs();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new int[0], 0, new int[0])]
        [InlineData(new int[0], 1, new int[0])]
        [InlineData(new int[0], 2, new int[0])]
        [InlineData(new[] { 0 }, 0, new int[0])]
        [InlineData(new[] { 0 }, 1, new[] { 0 })]
        [InlineData(new[] { 0 }, 2, new[] { 0 })]
        [InlineData(new[] { 0, 1 }, 0, new int[0])]
        [InlineData(new[] { 0, 1 }, 1, new[] { 0 })]
        [InlineData(new[] { 0, 1 }, 2, new[] { 0, 1 })]
        public void TakeTest(IEnumerable<int> source, uint count, IEnumerable<int> expected)
        {
            var actual = source.Take(count);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RangeUInt32TestFirst()
        {
            var first = EnumerableExt.RangeUInt32().First();

            Assert.Equal(0u, first);
        }

        [Theory]
        [InlineData(0u, 0u, new uint[0])]
        [InlineData(0u, 1u, new[] { 0u })]
        [InlineData(10u, 2u, new[] { 10u, 11u })]
        [InlineData(uint.MaxValue, 2u, new[] { uint.MaxValue, 0u })]
        public void RangeUInt32Test(uint start, uint count, IEnumerable<uint> expected)
        {
            var actual = EnumerableExt.RangeUInt32(start, count);

            Assert.Equal(expected, actual);
        }
    }
}
