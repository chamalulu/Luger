using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Luger.Configuration.CommandLine
{
    // TODO: This should be a DU
    public record ParseResult<TValue>(
        ImmutableList<(TValue value, ParseState state)> Successes,
        ImmutableList<(string message, ParseState state)> Failures)
    {
        public sealed class EqualityComparer : IEqualityComparer<ParseResult<TValue>>
        {
            public EqualityComparer(
                IEqualityComparer<TValue>? valueEqualityComparer = null,
                StringComparer? messageComparer = null)
            {
                valueEqualityComparer ??= EqualityComparer<TValue>.Default;
                messageComparer ??= StringComparer.InvariantCulture;

                SuccessComparer = EqualityComparer<(TValue value, ParseState state)>.Create(
                    (xItem, yItem) =>
                        valueEqualityComparer.Equals(xItem.value, yItem.value) && xItem.state.Equals(yItem.state));
                FailureComparer = EqualityComparer<(string message, ParseState state)>.Create(
                    (xItem, yItem) =>
                        messageComparer.Equals(xItem.message, yItem.message) && xItem.state.Equals(yItem.state));
            }

            EqualityComparer<(TValue value, ParseState state)> SuccessComparer { get; }

            EqualityComparer<(string message, ParseState state)> FailureComparer { get; }

            public bool Equals(ParseResult<TValue>? x, ParseResult<TValue>? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null || x.GetType() != y.GetType())
                {
                    return false;
                }

                var successesAreEqual = x.Successes.SequenceEqual(y.Successes, SuccessComparer);

                var failuresAreEqual = x.Failures.SequenceEqual(y.Failures, FailureComparer);

                return successesAreEqual && failuresAreEqual;
            }

            /// <remarks>Do not use <see cref="ParseResult{TValue}"/> objects as dictionary keys.</remarks>
            /// <returns>0</returns>
            /// <inheritdoc/>
            public int GetHashCode(ParseResult<TValue> obj) => 0;
        }
    }

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

            var nextSuccesses =
                from svnrp in sourceValueNextResultPairs
                from success in svnrp.result.Successes
                let value = projection(svnrp.value, success.value)
                select (value, success.state);

            var successes = ImmutableList.CreateRange(nextSuccesses);

            var nextFailures = successes.Count > 0
                ? Enumerable.Empty<(string, ParseState)>()
                : source.Failures.Concat(sourceValueNextResultPairs.SelectMany(svnrp => svnrp.result.Failures));

            var failures = ImmutableList.CreateRange(nextFailures);

            return new(successes, failures);
        }
    }
}
