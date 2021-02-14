using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Xunit;

namespace Luger.Configuration.CommandLine.Tests
{
    public class ParseResultTests
    {
        [Fact]
        public void SuccessTest()
        {
            var actual = ParseResult.Success(0, ParseState.Empty);

            Assert.Single(actual.Successes);
            Assert.Equal((0, ParseState.Empty), actual.Successes.Single());
            Assert.Empty(actual.Failures);
        }

        [Fact]
        public void FailureTest()
        {
            var actual = ParseResult.Failure<int>("message", ParseState.Empty);

            Assert.Empty(actual.Successes);
            Assert.Single(actual.Failures);
            Assert.Equal(("message", ParseState.Empty), actual.Failures.Single());
        }

        [Fact]
        public void SelectTest()
        {
            var successes = ImmutableList.Create((0, ParseState.Empty));
            var failures = ImmutableList.Create(("message", ParseState.Empty));
            var source = new ParseResult<int>(successes, failures);
            static string selector(int i) => i.ToString();
            var actual = source.Select(selector);

            Assert.Single(actual.Successes);
            Assert.Equal(("0", ParseState.Empty), actual.Successes.Single());
            Assert.Equal(failures, actual.Failures);
        }

        public static IEnumerable<object[]> SelectManyTestData =>

            new ((int[] vs, string[] fs) source, (int v, (int[] vs, string[] fs) pr)[] next, ((int, int)[] vs, string[] fs) expected)[]
            {
                ((Array.Empty<int>(), new[] { "s" }),
                 Array.Empty<(int v, (int[] vs, string[] fs) pr)>(),
                 (Array.Empty<(int, int)>(), new[] { "s" })),

                ((new[] { 0 }, new[] { "s" }),
                 new[] { (0, (Array.Empty<int>(), new[] { "n0" })) },
                 (Array.Empty<(int, int)>(), new[] { "s", "n0" })),

                ((new[] { 0 , 1 }, new[] { "s" }),
                 new[] { (0, (new[] { 2, 3 }, new[] { "n0" })), (1, (new[] { 4, 5 }, new[] { "n1" })) },
                 (new[] { (0, 2), (0, 3), (1, 4), (1, 5) }, Array.Empty<string>())),

            }.Select(data =>
            new object[]
            {
                new ParseResult<int>(
                    ImmutableList.CreateRange(from v in data.source.vs select (v, ParseState.Empty)),
                    ImmutableList.CreateRange(from f in data.source.fs select (f, ParseState.Empty))),
                data.next.ToDictionary(n => n.v, n => new ParseResult<int>(
                    ImmutableList.CreateRange(from v in n.pr.vs select (v, ParseState.Empty)),
                    ImmutableList.CreateRange(from f in n.pr.fs select (f, ParseState.Empty)))),
                new ParseResult<(int, int)>(
                    ImmutableList.CreateRange(from v in data.expected.vs select (v, ParseState.Empty)),
                    ImmutableList.CreateRange(from f in data.expected.fs select (f, ParseState.Empty)))
            });

        [Theory]
        [MemberData(nameof(SelectManyTestData))]
        public void SelectManyTest(
            ParseResult<int> source,
            Dictionary<int, ParseResult<int>> selectorDict,
            ParseResult<(int, int)> expected)
        {
            ParseDelegate<int> selector(int s) => state => selectorDict[s];
            var actual = source.SelectMany<int, int, (int, int)>(selector, (s, n) => (s, n));

            Assert.Equal(expected.Successes, actual.Successes);
            Assert.Equal(expected.Failures, actual.Failures);
        }

    }
}
