using System;
using System.Collections.Generic;
using System.Linq;

namespace Luger.Utilities
{
    public static class FullJoinExt
    {
        /// <summary>
        /// Perform full outer join of two heterogenous sequences.
        /// </summary>
        /// <param name="left">Left sequence</param>
        /// <param name="right">Right sequence</param>
        /// <param name="leftKeySelector">Key selector for left values</param>
        /// <param name="rightKeySelector">Key selector for right values</param>
        /// <param name="leftOuterResultSelector">Result selector for left outer values</param>
        /// <param name="rightOuterResultSelector">Result selector for right outer values</param>
        /// <param name="innerResultSelector">Result selector for inner values</param>
        /// <param name="keyComparer">Equality comparer for keys</param>
        /// <typeparam name="TLeft">Type of left values</typeparam>
        /// <typeparam name="TRight">Type of right values</typeparam>
        /// <typeparam name="TKey">Type of keys</typeparam>
        /// <typeparam name="TResult">Type of results</typeparam>
        public static IEnumerable<TResult> FullJoin<TLeft, TRight, TKey, TResult>(
            this IEnumerable<TLeft> left,
            IEnumerable<TRight> right,
            Func<TLeft, TKey> leftKeySelector,
            Func<TRight, TKey> rightKeySelector,
            Func<TLeft, TResult> leftOuterResultSelector,
            Func<TRight, TResult> rightOuterResultSelector,
            Func<TLeft, TRight, TResult> innerResultSelector,
            IEqualityComparer<TKey> keyComparer)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));
            if (leftKeySelector == null)
                throw new ArgumentNullException(nameof(leftKeySelector));
            if (rightKeySelector == null)
                throw new ArgumentNullException(nameof(rightKeySelector));
            if (leftOuterResultSelector == null)
                throw new ArgumentNullException(nameof(leftOuterResultSelector));
            if (rightOuterResultSelector == null)
                throw new ArgumentNullException(nameof(rightOuterResultSelector));
            if (innerResultSelector == null)
                throw new ArgumentNullException(nameof(innerResultSelector));

            var leftLookup = left.ToLookup(leftKeySelector, keyComparer);
            var rightLookup = right.ToLookup(rightKeySelector, keyComparer);

            var keys = Enumerable.Union(
                from g in leftLookup select g.Key,
                from g in rightLookup select g.Key,
                keyComparer);

            foreach (var key in keys)
            {
                var leftValues = leftLookup[key];
                var rightValues = rightLookup[key];

                foreach (var result in leftValues.Any() ? rightValues.Any()
                    ? leftValues.SelectMany(_ => rightValues, innerResultSelector)
                    : leftValues.Select(leftOuterResultSelector)
                    : rightValues.Select(rightOuterResultSelector))
                    yield return result;
            }
        }

        public static IEnumerable<TResult> FullJoin<TLeft, TRight, TKey, TResult>(
            this IEnumerable<TLeft> left,
            IEnumerable<TRight> right,
            Func<TLeft, TKey> leftKeySelector,
            Func<TRight, TKey> rightKeySelector,
            Func<TLeft, TResult> leftOuterResultSelector,
            Func<TRight, TResult> rightOuterResultSelector,
            Func<TLeft, TRight, TResult> innerResultSelector) =>
            FullJoin(
                left: left,
                right: right,
                leftKeySelector: leftKeySelector,
                rightKeySelector: rightKeySelector,
                leftOuterResultSelector: leftOuterResultSelector,
                rightOuterResultSelector: rightOuterResultSelector,
                innerResultSelector: innerResultSelector,
                keyComparer: null);

        public static IEnumerable<TResult> FullJoin<TSource, TKey, TResult>(
            this IEnumerable<TSource> left,
            IEnumerable<TSource> right,
            Func<TSource, TKey> keySelector,
            Func<TSource, TResult> outerResultSelector,
            Func<TSource, TSource, TResult> innerResultSelector,
            IEqualityComparer<TKey> keyComparer) =>
            FullJoin(
                left: left,
                right: right,
                leftKeySelector: keySelector,
                rightKeySelector: keySelector,
                leftOuterResultSelector: outerResultSelector,
                rightOuterResultSelector: outerResultSelector,
                innerResultSelector: innerResultSelector,
                keyComparer: keyComparer);

        public static IEnumerable<TResult> FullJoin<TSource, TKey, TResult>(
            this IEnumerable<TSource> left,
            IEnumerable<TSource> right,
            Func<TSource, TKey> keySelector,
            Func<TSource, TResult> outerResultSelector,
            Func<TSource, TSource, TResult> innerResultSelector) =>
            FullJoin(
                left: left,
                right: right,
                leftKeySelector: keySelector,
                rightKeySelector: keySelector,
                leftOuterResultSelector: outerResultSelector,
                rightOuterResultSelector: outerResultSelector,
                innerResultSelector: innerResultSelector,
                keyComparer: null);

        private static T Id<T>(T t) => t;

        public static IEnumerable<TResult> FullJoin<TSource, TResult>(
            this IEnumerable<TSource> left,
            IEnumerable<TSource> right,
            Func<TSource, TResult> outerResultSelector,
            Func<TSource, TSource, TResult> innerResultSelector) =>
            FullJoin(
                left: left,
                right: right,
                leftKeySelector: Id,
                rightKeySelector: Id,
                leftOuterResultSelector: outerResultSelector,
                rightOuterResultSelector: outerResultSelector,
                innerResultSelector: innerResultSelector,
                keyComparer: null);

        public static IEnumerable<T> FullJoin<T>(
            this IEnumerable<T> left,
            IEnumerable<T> right,
            Func<T, T, T> innerResultSelector) =>
            FullJoin(
                left: left,
                right: right,
                leftKeySelector: Id,
                rightKeySelector: Id,
                leftOuterResultSelector: Id,
                rightOuterResultSelector: Id,
                innerResultSelector: innerResultSelector,
                keyComparer: null);
    }
}


