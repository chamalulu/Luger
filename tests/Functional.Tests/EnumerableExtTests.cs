using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static Luger.Functional.Optional;

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

        private class OptionalEnumerableEqualityComparer<T> : IEqualityComparer<Optional<IEnumerable<T>>>
        {
            public bool Equals(Optional<IEnumerable<T>> x, Optional<IEnumerable<T>> y) =>
                x.Match(
                    none: () => !y.IsSome,
                    some: xs => y.Match(
                        none: () => false,
                        some: ys => xs.SequenceEqual(ys)
                    )
                );

            public int GetHashCode(Optional<IEnumerable<T>> obj) => throw new NotImplementedException();
        }

        public static IEnumerable<object[]> HeadTestData => new[]
        {
            new object[] { Array.Empty<int>(), None },
            new object[] { new[] { 1 }, 1}
        };

        [Theory]
        [MemberData(nameof(HeadTestData))]
        public void HeadTest(IEnumerable<int> source, Optional<int> expected)
        {
            var actual = source.Head();
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> TailTestData => new[]
        {
            new object[] { Array.Empty<int>(), None },
            new object[] { new[] { 1 }, Array.Empty<int>() },
            new object[] { new[] { 1, 2, 3 }, new[] { 2, 3 }}
        };

        [Theory]
        [MemberData(nameof(TailTestData))]
        public void TailTest(IEnumerable<int> source, Optional<IEnumerable<int>> expected)
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
    }
}
