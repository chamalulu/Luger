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

        [Fact]
        public void DeconstructIOExTest() =>
            Assert.Throws<InvalidOperationException>(() => { var (actualHead, actualTail) = Enumerable.Empty<int>(); });

        [Theory]
        [InlineData(new[] { 1 }, 1, new int[0])]
        [InlineData(new[] { 1, 2, 3 }, 1, new[] { 2, 3 })]
        public void DeconstructTest(IEnumerable<int> source, int expectedHead, IEnumerable<int> expectedTail)
        {
            var (actualHead, actualTail) = source;
            Assert.Equal(expectedHead, actualHead);
            Assert.Equal(expectedTail, actualTail);
        }

        private class OptionalEnumerableEqualityComparer<T> : IEqualityComparer<Maybe<IEnumerable<T>>>
        {
            public bool Equals(Maybe<IEnumerable<T>> x, Maybe<IEnumerable<T>> y) =>
                x.Match(
                    some: xs => y.Match(
                        some: ys => xs.SequenceEqual(ys),
                        none: () => false),
                    none: () => !y.IsSome);

            public int GetHashCode(Maybe<IEnumerable<T>> obj) => throw new NotImplementedException();
        }

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

        public static IEnumerable<object[]> TailTestData => new[]
        {
            new object[] { Array.Empty<int>(), None<IEnumerable<int>>() },
            new object[] { new [] { 1 },       Some<IEnumerable<int>>(Array.Empty<int>()) },
            new object[] { new [] { 1, 2, 3 }, Some<IEnumerable<int>>(new[] { 2, 3 }) }
        };

        [Theory]
        [MemberData(nameof(TailTestData))]
        public void TailTest(IEnumerable<int> source, Maybe<IEnumerable<int>> expected)
        {
            var actual = source.Tail();
            Assert.Equal(expected, actual, new OptionalEnumerableEqualityComparer<int>());
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
            new object[] { new[] { 0 }         , new[] { (0, 0) }          },
            new object[] { new[] { 0, 1 }      , new[] { (0, 1) }          },
            new object[] { new[] { 0, 1, 2 }   , new[] { (0, 1), (2, 0) }  },
            new object[] { new[] { 0, 1, 2, 3 }, new[] { (0, 1), (2, 3) }  }
        };

        [Theory]
        [MemberData(nameof(PairwiseTestData))]
        public void PairwiseTest(IEnumerable<int> source, IEnumerable<(int, int)> expected)
        {
            var actual = source.Pairwise();
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

        [Fact(Skip = "Very slow test")]
        public void RangeUInt32TestFirstLast()
        {
            var first = EnumerableExt.RangeUInt32().First();
            var last = EnumerableExt.RangeUInt32().Last();

            Assert.Equal(0u, first);
            Assert.Equal(uint.MaxValue, last);
        }

        [Theory]
        [InlineData(0u, 0u, new uint[0], false)]
        [InlineData(0u, 1u, new[] { 0u }, false)]
        [InlineData(10u, 2u, new[] { 10u, 11u }, false)]
        [InlineData(uint.MaxValue, 2u, new[] { uint.MaxValue, 0u }, true)]
        public void RangeUInt32Test(uint start, uint count, IEnumerable<uint> expected, bool allowOEx)
        {
            var actual = EnumerableExt.RangeUInt32(start, count);

            try
            {
                Assert.Equal(expected, actual);
            }
            catch (OverflowException)
            {
                if (!allowOEx)
                    throw;
            }
        }
    }
}
