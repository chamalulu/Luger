using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Luger.Functional.Maybe;

namespace Luger.Functional
{
    [DebuggerStepThrough]
    public static class EnumerableExt
    {
        // Implement Map, Apply and Bind with framework Select and SelectMany.
        public static IEnumerable<TR> Map<T, TR>(this IEnumerable<T> ts, Func<T, TR> f) => Enumerable.Select(ts, f);

        public static IEnumerable<TR> Apply<T, TR>(this IEnumerable<Func<T, TR>> fs, IEnumerable<T> ts)

            => Enumerable.SelectMany(fs, _ => ts, (f, t) => f(t));

        public static IEnumerable<TR> Bind<T, TR>(this IEnumerable<T> ts, Func<T, IEnumerable<TR>> f)

            => Enumerable.SelectMany(ts, f);

        public static IEnumerable<T> Repeat<T>(T element, uint count)
        {
            for (var c = 0U; c < count; c++)
            {
                yield return element;
            }
        }

        public static IEnumerable<T> Return<T>(T t) => Repeat(t, 1u);

        public static IEnumerable<T> Empty<T>() => Repeat(default(T)!, 0u);

        private static IEnumerable<T> ContinueAsEnumerableNoneOrMore<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        private static IEnumerable<T> ContinueAsEnumerableOneOrMore<T>(this IEnumerator<T> enumerator)
        {
            do
            {
                yield return enumerator.Current;
            }
            while (enumerator.MoveNext());
        }

        /// <summary>
        /// Matches empty or other sequence to result.
        /// </summary>
        /// <typeparam name="T">Type of source sequence element</typeparam>
        /// <typeparam name="TR">Type of result</typeparam>
        /// <param name="ts">Source sequence</param>
        /// <param name="none">Factory of result for empty sequence</param>
        /// <param name="some">Map of other sequence to result</param>
        public static TR Match<T, TR>(this IEnumerable<T> ts, Func<TR> none, Func<IEnumerable<T>, TR> some)
        {
            ts = ts ?? throw new ArgumentNullException(nameof(ts));
            none = none ?? throw new ArgumentNullException(nameof(none));
            some = some ?? throw new ArgumentNullException(nameof(some));

            using var enumerator = ts.GetEnumerator();

            return enumerator.MoveNext()
                ? some(enumerator.ContinueAsEnumerableOneOrMore())
                : none();
        }

        /// <summary>
        /// Matches empty or other sequence to result.
        /// </summary>
        /// <typeparam name="T">Type of source sequence element</typeparam>
        /// <typeparam name="TR">Type of result</typeparam>
        /// <param name="ts">Source sequence</param>
        /// <param name="none">Factory of result for empty sequence</param>
        /// <param name="some">Map of head element and tail sequence to result</param>
        public static TR Match<T, TR>(this IEnumerable<T> ts, Func<TR> none, Func<T, IEnumerable<T>, TR> some)
        {
            ts = ts ?? throw new ArgumentNullException(nameof(ts));
            none = none ?? throw new ArgumentNullException(nameof(none));
            some = some ?? throw new ArgumentNullException(nameof(some));

            using var enumerator = ts.GetEnumerator();

            return enumerator.MoveNext()
                ? some(enumerator.Current, enumerator.ContinueAsEnumerableNoneOrMore())
                : none();
        }

        /// <summary>
        /// Matches empty, singleton or other sequence to result.
        /// </summary>
        /// <typeparam name="T">Type of source sequence element</typeparam>
        /// <typeparam name="TR">Type of result</typeparam>
        /// <param name="ts">Source sequence</param>
        /// <param name="none">Factory of result for empty sequence</param>
        /// <param name="one">Map of singleton element to result</param>
        /// <param name="some">Map of other sequence to result</param>
        public static TR Match<T, TR>(
            this IEnumerable<T> ts,
            Func<TR> none,
            Func<T, TR> one,
            Func<IEnumerable<T>, TR> some)
        {
            ts = ts ?? throw new ArgumentNullException(nameof(ts));
            none = none ?? throw new ArgumentNullException(nameof(none));
            one = one ?? throw new ArgumentNullException(nameof(one));
            some = some ?? throw new ArgumentNullException(nameof(some));

            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var first = enumerator.Current;

                return enumerator.MoveNext()
                    ? some(enumerator.ContinueAsEnumerableOneOrMore().Prepend(first))
                    : one(first);
            }
            else
            {
                return none();
            }
        }

        /// <summary>
        /// Matches empty, singleton or other sequence to result.
        /// </summary>
        /// <typeparam name="T">Type of source sequence element</typeparam>
        /// <typeparam name="TR">Type of result</typeparam>
        /// <param name="ts">Source sequence</param>
        /// <param name="none">Factory of result for empty sequence</param>
        /// <param name="one">Map of singleton element to result</param>
        /// <param name="some">Map of head element and tail sequence to result</param>
        public static TR Match<T, TR>(
            this IEnumerable<T> ts,
            Func<TR> none,
            Func<T, TR> one,
            Func<T, IEnumerable<T>, TR> some
        )
        {
            ts = ts ?? throw new ArgumentNullException(nameof(ts));
            none = none ?? throw new ArgumentNullException(nameof(none));
            one = one ?? throw new ArgumentNullException(nameof(one));
            some = some ?? throw new ArgumentNullException(nameof(some));

            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var first = enumerator.Current;

                return enumerator.MoveNext()
                    ? some(first, enumerator.ContinueAsEnumerableOneOrMore())
                    : one(first);
            }
            else
            {
                return none();
            }
        }

        public static void Deconstruct<T>(this IEnumerable<T> ts, out T head, out IEnumerable<T> tail)

            => (head, tail) = ts.Match(() => throw new InvalidOperationException(), (h, t) => (h, t));

        public static Maybe<T> Head<T>(this IEnumerable<T> ts) => ts.Match(None<T>, (head, _) => Some(head));

        public static Maybe<IEnumerable<T>> Tail<T>(this IEnumerable<T> ts)

            => ts.Match(None<IEnumerable<T>>, (_, tail) => Some(tail));

        public static IEnumerable<(T value, uint index)> WithUInt32Index<T>(this IEnumerable<T> ts)
        {
            ts = ts ?? throw new ArgumentNullException(nameof(ts));

            var i = 0U;

            foreach (var t in ts)
            {
                yield return (t, checked(i++));
            }
        }

        public static IEnumerable<(T value, ulong index)> WithUInt64Index<T>(this IEnumerable<T> ts)
        {
            ts = ts ?? throw new ArgumentNullException(nameof(ts));

            var i = 0UL;

            foreach (var t in ts)
            {
                yield return (t, checked(i++));
            }
        }

        public static IEnumerable<T> EveryNth<T>(this IEnumerable<T> ts, ulong n)

            => n > 0
                ? from ti in ts.WithUInt64Index()
                  where ti.index % n == 0
                  select ti.value
                : throw new ArgumentOutOfRangeException(nameof(n));

        public static IEnumerable<T> EveryOther<T>(this IEnumerable<T> ts) => ts.EveryNth(2);

        public static IEnumerable<(T? first, T? second)> Pairwise<T>(this IEnumerable<T> ts)
        {
            ts = ts ?? throw new ArgumentNullException(nameof(ts));

            using var etor = ts.GetEnumerator();

            while (etor.MoveNext())
            {
                var first = etor.Current;
                var second = etor.MoveNext() ? etor.Current : default;

                yield return (first, second);
            }
        }

        public static IEnumerable<T> Take<T>(this IEnumerable<T> source, uint count)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));

            using var etor = source.GetEnumerator();

            while (count > 0 && etor.MoveNext())
            {
                yield return etor.Current;
                count--;
            }
        }

        public static uint UInt32Count<T>(this IEnumerable<T> sequence)
        {
            sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));

            static uint count(IEnumerable<T> sequence)
            {
                var i = 0U;
                using var etor = sequence.GetEnumerator();

                while (etor.MoveNext())
                {
                    checked { i++; }
                }

                return i;
            }

            return sequence switch
            {
                ICollection<T> collection => (uint)collection.Count,
                ICollection collection => (uint)collection.Count,
                _ => count(sequence),
            };
        }

        public static ulong UInt64Count<T>(this IEnumerable<T> sequence)
        {
            sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));

            static ulong count(IEnumerable<T> sequence)
            {
                var i = 0UL;
                using var etor = sequence.GetEnumerator();

                while (etor.MoveNext())
                {
                    checked { i++; }
                }

                return i;
            }

            return sequence switch
            {
                ICollection<T> collection => (ulong)collection.Count,
                ICollection collection => (ulong)collection.Count,
                _ => count(sequence),
            };
        }

        /// <summary>
        /// Generates an ordered sequence of all unsigned 32-bit numbers in the range [0 .. 2^32).
        /// </summary>
        public static IEnumerable<uint> RangeUInt32()
        {
            var i = uint.MinValue;

            do
            {
                yield return i++;
            }
            while (i > uint.MinValue);
        }

        /// <summary>
        /// Generates an ordered sequence of unsigned 32-bit numbers in the range [0 .. count).  
        /// </summary>
        public static IEnumerable<uint> RangeUInt32(uint count) => RangeUInt32().Take(count);

        /// <summary>
        /// Generates a sequence of unsigned 32-bit numbers in the range [start .. start + count).
        /// May wrap around to 0 in unchecked context.
        /// </summary>
        /// <exception cref="OverflowException">
        /// Thrown in checked context if and when sequence wraps around to 0.
        /// </exception>
        public static IEnumerable<uint> RangeUInt32(uint start, uint count) => RangeUInt32(count).Map(i => i + start);
    }
}
