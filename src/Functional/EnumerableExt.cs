using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using static Luger.Functional.Optional;

namespace Luger.Functional
{
    [DebuggerStepThrough]
    public static class EnumerableExt
    {
        // Implement Map, Apply and Bind with framework Select and SelectMany.
        public static IEnumerable<TR> Map<T, TR>(this IEnumerable<T> ts, Func<T, TR> f)
            => Enumerable.Select(ts, f);

        public static IEnumerable<TR> Apply<T, TR>(this IEnumerable<Func<T, TR>> fs, IEnumerable<T> ts)
            => Enumerable.SelectMany(fs, _ => ts, (f, t) => f(t));

        public static IEnumerable<TR> Bind<T, TR>(this IEnumerable<T> ts, Func<T, IEnumerable<TR>> f)
            => Enumerable.SelectMany(ts, f);

        private static TR Match<T, TR>(this IEnumerable<T> ts, Func<TR> none, Func<ImmutableArray<T>, TR> some)
        {
            var list = ts.ToImmutableArray();
            return list.Length == 0 ? none() : some(list);
        }

        public static TR Match<T, TR>(this IEnumerable<T> ts, Func<TR> none, Func<IEnumerable<T>, TR> some)
            => ts.Match(none, (ImmutableArray<T> array) => some(array));

        public static TR Match<T, TR>(this IEnumerable<T> ts, Func<TR> none, Func<T, IEnumerable<T>, TR> some)
            => ts.Match(none, array => some(array[0], array.Skip(1)));

        public static TR Match<T, TR>(
            this IEnumerable<T> ts,
            Func<TR> none,
            Func<T, TR> single,
            Func<IEnumerable<T>, TR> some
        )
            => ts.Match(none, array => array.Length == 1 ? single(array[0]) : some(array));

        public static TR Match<T, TR>(
            this IEnumerable<T> ts,
            Func<TR> none,
            Func<T, TR> single,
            Func<T, IEnumerable<T>, TR> some
        )
            => ts.Match(none, array => array.Length == 1 ? single(array[0]) : some(array[0], array.Skip(1)));

        public static void Deconstruct<T>(this IEnumerable<T> ts, out T head, out IEnumerable<T> tail)
            => (head, tail) = ts.Match(() => throw new InvalidOperationException(), (h, t) => (h, t));

        public static Optional<T> Head<T>(this IEnumerable<T> ts)
            => ts.Match(() => None, (head, _) => Some(head));

        public static Optional<IEnumerable<T>> Tail<T>(this IEnumerable<T> ts)
            => ts.Match(() => None, (_, tail) => Some(tail));

        /* Rotation step is the odd integer closest to [bits per int] / (golden ratio)
         * Being odd makes it coprime with any normal field width since they are 2^n
         *  which should improve spread of element GetHashCode implementation results
         *  and make the result more sensitive of element order.
         * 1,696,631 = ⌊(1 + √5)/2 * 2^20⌋
         * BTW, do not use this on sets since element order does not matter.
         */

        private const int BitsPerInt = sizeof(int) << 3;
        private const int LShift = (BitsPerInt << 20) / 1696631 | 1;
        private const int RShift = BitsPerInt - LShift;

        // This function can not be implemented and used as an extension method
        //  since GetHashCode is defined on System.Object .
        /// <summary>Calculate hash code of a sequence.</summary>
        public static int GetHashCode<T>(IEnumerable<T> sequence)
            => sequence
                .Map(t => t?.GetHashCode() ?? 0)
                .Aggregate(0, (acc, hashcode) => acc << LShift ^ acc >> RShift ^ hashcode);

        public static int GetHashCode(params object[] args) => GetHashCode(args.AsEnumerable());

        public static IEnumerable<(T value, uint index)> WithIndex<T>(this IEnumerable<T> ts)
        {
            var index = 0U;

            foreach (var t in ts)
                yield return (t, index++);
        }

        public static IEnumerable<(T value, ulong index)> WithLongIndex<T>(this IEnumerable<T> ts)
        {
            var index = 0UL;

            foreach (var t in ts)
                yield return (t, index++);
        }

        public static IEnumerable<T> EveryNth<T>(this IEnumerable<T> ts, ulong n)
            => n > 0
                ? from t in ts.WithLongIndex()
                  where t.index % n == 0
                  select t.value
                : throw new ArgumentOutOfRangeException(nameof(n));

        public static IEnumerable<T> EveryOther<T>(this IEnumerable<T> ts) => ts.EveryNth(2);

        public static IEnumerable<(T first, T second)> Pairwise<T>(this IEnumerable<T> ts)
        {
            using (var etor = ts.GetEnumerator())
                while (etor.MoveNext())
                {
                    var first = etor.Current;
                    var second = etor.MoveNext() ? etor.Current : default;

                    yield return (first, second);
                }
        }

        public static IEnumerable<T> Repeat<T>(T element, uint count)
        {
            for (uint c = 0; c < count; c++)
                yield return element;
        }

        public static IEnumerable<T> Take<T>(this IEnumerable<T> source, uint count)
        {
            using (var etor = source.GetEnumerator())
                while (count > 0 && etor.MoveNext())
                {
                    yield return etor.Current;
                    count--;
                }
        }

        public static uint Count<T>(this IEnumerable<T> sequence)
            => sequence.Aggregate(0U, (c, _) => c + 1);

        public static ulong LongCount<T>(this IEnumerable<T> sequence)
            => sequence.Aggregate(0UL, (c, _) => c + 1);

        public static IEnumerable<uint> RangeUInt32()
        {
            for (var i = uint.MinValue; i < uint.MaxValue; i++)
                yield return i;

            yield return uint.MaxValue;
        }

        public static IEnumerable<uint> RangeUInt32(uint count)
            => RangeUInt32().Take(count);

        public static IEnumerable<uint> RangeUInt32(uint start, uint count)
            => start + count - 1 >= start
                ? RangeUInt32(count).Map(e => e + start)
                : count == 0
                    ? System.Linq.Enumerable.Empty<uint>()
                    : throw new ArgumentException("start + count > uint.MaxValue");
    }
}