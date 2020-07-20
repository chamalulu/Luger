using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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

        private static IEnumerable<T> ContinueAsEnumerable<T>(this IEnumerator<T> enumerator)
        {
            Contract.Requires(enumerator != null);

            do { yield return enumerator.Current; }
            while (enumerator.MoveNext());
        }

        public static TR Match<T, TR>(this IEnumerable<T> ts, Func<TR> none, Func<IEnumerable<T>, TR> some)
        {
            Contract.Requires(ts != null);
            Contract.Requires(none != null);
            Contract.Requires(some != null);

            using var enumerator = ts.GetEnumerator();

            return enumerator.MoveNext()
                ? some(enumerator.ContinueAsEnumerable())
                : none();
        }

        public static TR Match<T, TR>(this IEnumerable<T> ts, Func<TR> none, Func<T, IEnumerable<T>, TR> some)
        {
            Contract.Requires(ts != null);
            Contract.Requires(none != null);
            Contract.Requires(some != null);

            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var first = enumerator.Current;

                return enumerator.MoveNext()
                    ? some(first, enumerator.ContinueAsEnumerable())
                    : some(first, Enumerable.Empty<T>());
            }
            else
                return none();
        }

        public static TR Match<T, TR>(
            this IEnumerable<T> ts,
            Func<TR> none,
            Func<T, TR> one,
            Func<IEnumerable<T>, TR> some
        )
        {
            Contract.Requires(ts != null);
            Contract.Requires(none != null);
            Contract.Requires(one != null);
            Contract.Requires(some != null);

            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var first = enumerator.Current;

                return enumerator.MoveNext()
                    ? some(enumerator.ContinueAsEnumerable().Prepend(first))
                    : one(first);
            }
            else
                return none();
        }

        public static TR Match<T, TR>(
            this IEnumerable<T> ts,
            Func<TR> none,
            Func<T, TR> one,
            Func<T, IEnumerable<T>, TR> some
        )
        {
            Contract.Requires(ts != null);
            Contract.Requires(none != null);
            Contract.Requires(one != null);
            Contract.Requires(some != null);

            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var first = enumerator.Current;

                return enumerator.MoveNext()
                    ? some(first, enumerator.ContinueAsEnumerable())
                    : one(first);
            }
            else
                return none();
        }

        public static void Deconstruct<T>(this IEnumerable<T> ts, out T head, out IEnumerable<T> tail)
            => (head, tail) = ts.Match(() => throw new InvalidOperationException(), (h, t) => (h, t));

        public static Optional<T> Head<T>(this IEnumerable<T> ts)
            => ts.Match(() => None, (head, _) => Some(head));

        public static Optional<IEnumerable<T>> Tail<T>(this IEnumerable<T> ts)
            => ts.Match(() => None, (_, tail) => Some(tail));

        public static IEnumerable<(T value, uint index)> WithUInt32Index<T>(this IEnumerable<T> ts)
        {
            var index = 0U;

            foreach (var t in ts)
                yield return (t, checked(index++));
        }

        public static IEnumerable<(T value, ulong index)> WithUInt64Index<T>(this IEnumerable<T> ts)
        {
            var index = 0UL;

            foreach (var t in ts)
                yield return (t, checked(index++));
        }

        public static IEnumerable<T> EveryNth<T>(this IEnumerable<T> ts, ulong n)
            => n > 0
                ? from ti in ts.WithUInt64Index()
                  where ti.index % n == 0
                  select ti.value
                : throw new ArgumentOutOfRangeException(nameof(n));

        public static IEnumerable<T> EveryOther<T>(this IEnumerable<T> ts)
            => ts.EveryNth(2);

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

        public static uint UInt32Count<T>(this IEnumerable<T> sequence)
        {
            switch (sequence)
            {
                case ICollection<T> collection:
                    return (uint)collection.Count;
                case ICollection collection:
                    return (uint)collection.Count;
                default:
                    var i = 0U;
                    using (var etor = sequence.GetEnumerator())
                        while (etor.MoveNext())
                            checked { i++; }

                    return i;
            }
        }

        public static ulong UInt64Count<T>(this IEnumerable<T> sequence)
        {
            switch (sequence)
            {
                case ICollection<T> collection:
                    return (ulong)collection.Count;
                case ICollection collection:
                    return (ulong)collection.Count;
                default:
                    var i = 0UL;
                    using (var etor = sequence.GetEnumerator())
                        while (etor.MoveNext())
                            checked { i++; }

                    return i;
            }
        }

        public static IEnumerable<uint> RangeUInt32()
        {
            var i = uint.MinValue;

            do
                yield return i++;
            while (i > uint.MinValue);
        }

        public static IEnumerable<uint> RangeUInt32(uint count)
            => RangeUInt32().Take(count);

        public static IEnumerable<uint> RangeUInt32(uint start, uint count)
            => count == 0
                ? System.Linq.Enumerable.Empty<uint>()
                : count - 1 <= uint.MaxValue - start
                    ? RangeUInt32(count).Map(i => i + start)
                    : throw new ArgumentException("start + count - 1 > uint.MaxValue");
    }
}
