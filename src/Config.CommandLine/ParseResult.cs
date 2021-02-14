using System;
using System.Collections.Immutable;
using System.Linq;

namespace Luger.Configuration.CommandLine
{
    public record ParseResult<TValue>(
        ImmutableList<(TValue value, ParseState state)> Successes,
        ImmutableList<(string message, ParseState state)> Failures);

    public static class ParseResult
    {
        public static ParseResult<TValue> Success<TValue>(TValue value, ParseState state)
        {
            var successes = ImmutableList.Create((value, state));
            var failures = ImmutableList.Create<(string, ParseState)>();

            return new(successes, failures);
        }

        public static ParseResult<TValue> Failure<TValue>(string message, ParseState state)
        {
            var successes = ImmutableList.Create<(TValue, ParseState)>();
            var failures = ImmutableList.Create((message, state));

            return new(successes, failures);
        }

        public static ParseResult<TResult> Select<TSource, TResult>(this ParseResult<TSource> source, Func<TSource, TResult> selector)
        {
            var results = from success in source.Successes
                          select (selector(success.value), success.state);

            var successes = ImmutableList.CreateRange(results);
            var failures = source.Failures;

            return new(successes, failures);
        }

        public static ParseResult<TResult> SelectMany<TSource, TNext, TResult>(
            this ParseResult<TSource> source,
            Func<TSource, ParseDelegate<TNext>> selector,
            Func<TSource, TNext, TResult> projection)
        {
            var sourceValueNextResultPairs = source.Successes
                .Select(s => (s.value, result: selector(s.value)(s.state)))
                .ToArray();

            var nextSuccesses = from svnrp in sourceValueNextResultPairs
                                from success in svnrp.result.Successes
                                let value = projection(svnrp.value, success.value)
                                select (value, success.state);

            var successes = ImmutableList.CreateRange(nextSuccesses);

            var failures = ImmutableList.CreateRange(successes.Count > 0
                ? Enumerable.Empty<(string, ParseState)>()
                : source.Failures.Concat(sourceValueNextResultPairs.SelectMany(svnrp => svnrp.result.Failures)));

            return new(successes, failures);
        }
    }
}
