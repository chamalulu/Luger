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

        public static IEnumerable<T> Repeat<T>(T element)
        {
            while (true)
            {
                yield return element;
            }
        }

        public static IEnumerable<T> Repeat<T>(T element, uint count)
        {
            for (uint c = 0; c < count; c++)
            {
                yield return element;
            }
        }

        private struct SingletonEnumerator<T> : IEnumerator<T>
        {
            private readonly T _element;
            private bool _exhausted;

            public SingletonEnumerator(T element)
            {
                _element = element;
                _exhausted = false;
            }

            public T Current => _element;

            object? IEnumerator.Current => _element;

            public void Dispose() => _exhausted = true;

            public bool MoveNext() => !_exhausted;

            public void Reset() => _exhausted = false;
        }

        private readonly struct SingletonEnumerable<T> : IEnumerable<T>
        {
            private readonly T _element;

            public SingletonEnumerable(T element) => _element = element;

            public IEnumerator<T> GetEnumerator() => new SingletonEnumerator<T>(_element);

            IEnumerator IEnumerable.GetEnumerator() => new SingletonEnumerator<T>(_element);
        }

        // TODO: Add unit test
        public static IEnumerable<T> Return<T>(T element) => new SingletonEnumerable<T>(element);

        private static IEnumerable<T> FromEnumerator<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        private static IEnumerable<T> FromEnumeratorCurrent<T>(this IEnumerator<T> enumerator)
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
            using var enumerator = ts.GetEnumerator();

            return enumerator.MoveNext()
                ? some(FromEnumeratorCurrent(enumerator))
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
            using var enumerator = ts.GetEnumerator();

            return enumerator.MoveNext()
                ? some(enumerator.Current, FromEnumerator(enumerator))
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
            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var first = enumerator.Current;

                return enumerator.MoveNext()
                    ? some(FromEnumeratorCurrent(enumerator).Prepend(first))
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
            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var first = enumerator.Current;

                return enumerator.MoveNext()
                    ? some(first, FromEnumeratorCurrent(enumerator))
                    : one(first);
            }
            else
            {
                return none();
            }
        }

        /// <summary>
        /// Produce head of sequence if any.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="ts">Source sequence.</param>
        /// <returns><see cref="Maybe{T}"/> with zero or one element first in sequence.</returns>
        /// <remarks>
        /// This is functionally analogous to <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>.
        /// </remarks>
        public static Maybe<T> Head<T>(this IEnumerable<T> ts) where T : notnull
        {
            using var enumerator = ts.GetEnumerator();

            return enumerator.MoveNext()
                ? Some(enumerator.Current)
                : None<T>();
        }

        /// <summary>
        /// Convert an empty or singleton <see cref="IEnumerable{T}"/> to a <see cref="Maybe{T}"/>
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="ts">Sequence to convert.</param>
        /// <returns><see cref="Maybe{T}"/> with zero or one element from source sequence.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="ts"/> contains more than one element.</exception>
        /// <remarks>
        /// This is functionally analogous to <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource})"/>.
        /// </remarks>
        // TODO: Add unit tests
        public static Maybe<T> ToMaybe<T>(this IEnumerable<T> ts) where T : notnull
        {
            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var element = enumerator.Current;

                if (enumerator.MoveNext())
                {
                    throw new InvalidOperationException();
                }

                return Some(element);
            }
            else
            {
                return None<T>();
            }
        }

        public static IEnumerable<T> EveryNth<T>(this IEnumerable<T> ts, ulong n)
        {
            if (n == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n));
            }

            ulong p = 0;

            foreach (var element in ts)
            {
                if (p == 0)
                {
                    yield return element;
                    p = n;
                }

                p -= 1;
            }
        }

        // TODO: Add unit tests
        public static IEnumerable<T> EveryOther<T>(this IEnumerable<T> ts)// => ts.EveryNth(2);
        {
            var other = true;

            foreach (var element in ts)
            {
                if (other)
                {
                    yield return element;
                }

                other = !other;
            }
        }

        /// <summary>
        /// Yield sequential pairs of source.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="ts">Source sequence.</param>
        /// <returns>Sequence of sequential pairs.</returns>
        /// <remarks>Number of pairs is #ts / 2. Last item in source with odd number of elements will be discarded.</remarks>
        public static IEnumerable<(T first, T second)> SequentialPairs<T>(this IEnumerable<T> ts)
        {
            using var enumerator = ts.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var first = enumerator.Current;

                if (enumerator.MoveNext())
                {
                    yield return (first, enumerator.Current);
                }
            }
        }

        /// <summary>
        /// Yield overlapping pairs of source. 
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="ts">Source sequence.</param>
        /// <returns>Sequence of overlapping pairs.</returns>
        /// <remarks>Number of pairs is #ts - 1. I.e. a singleton source will not yield any pairs.</remarks>
        // TODO: Add unit tests
        public static IEnumerable<(T first, T second)> OverlappingPairs<T>(this IEnumerable<T> ts)
        {
            using var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var previous = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    yield return (_, previous) = (previous, enumerator.Current);
                }
            }
        }

        public static IEnumerable<T> Take<T>(this IEnumerable<T> ts, uint count)
        {
            using var enumerator = ts.GetEnumerator();

            while (count > 0 && enumerator.MoveNext())
            {
                yield return enumerator.Current;
                count--;
            }
        }

        public static uint UCount<T>(this IEnumerable<T> sequence)
        {
            using var etor = sequence.GetEnumerator();

            uint count = 0;

            while (etor.MoveNext())
            {
                checked
                {
                    count += 1;
                }
            }

            return count;
        }

        /// <summary>
        /// Generates an ordered sequence of all unsigned 32-bit numbers in the range [0 .. 2^32).
        /// </summary>
        public static IEnumerable<uint> RangeUInt32()
        {
            uint i = 0;

            do
            {
                yield return unchecked(i++);
            }
            while (i > 0);
        }

        /// <summary>
        /// Generates an ordered sequence of unsigned 32-bit numbers in the range [0 .. count).  
        /// </summary>
        public static IEnumerable<uint> RangeUInt32(uint count)
        {
            for (uint i = 0; i < count; i++)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Generates a sequence of unsigned 32-bit numbers in the range [start .. start + count).
        /// </summary>
        /// <remarks>
        /// Wraps around to 0 if start + count > 2^32.
        /// </remarks>
        public static IEnumerable<uint> RangeUInt32(uint start, uint count)
        {
            while (count-- > 0)
            {
                yield return unchecked(start++);
            }
        }
    }
}
