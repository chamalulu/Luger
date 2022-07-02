using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Luger.Functional
{
    /// <summary>
    /// Composable version of <see cref="Nullable{T}"/> for value types and non-nullable reference types.
    /// Provides functional mapping and binding.
    /// </summary>
    [DebuggerStepThrough]
    public readonly struct Maybe<T> : IEquatable<Maybe<T>>, IFormattable, IEnumerable<T> where T : notnull
    {
        private readonly T? _value;

        private Maybe(T value) => _value = value;

        public TR Match<TR>(Func<T, TR> some, Func<TR> none) => _value is null ? none() : some(_value);

        private static readonly IEqualityComparer<T> ValueEqualityComparer = EqualityComparer<T>.Default;

        public bool Equals(Maybe<T> other)

            => _value is null
                ? other._value is null
                : other._value is not null && ValueEqualityComparer.Equals(_value, other._value);

        public override bool Equals(object? obj) => obj is Maybe<T> other && Equals(other);

        public override int GetHashCode() => _value is null ? 0 : ValueEqualityComparer.GetHashCode(_value);

        public IEnumerator<T> GetEnumerator()
        {
            if (_value is not null)
            {
                yield return _value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string ToString(string? format, IFormatProvider? formatProvider)

            => _value switch
            {
                null => "None",
                IFormattable formattable => $"Some({formattable.ToString(format, formatProvider)})",
                _ => $"Some({_value})"
            };

        public override string ToString() => ToString(null, null);

        public string ToString(string? format) => ToString(format, null);

        public static bool operator true(Maybe<T> value) => value._value is not null;

        public static bool operator false(Maybe<T> value) => value._value is null;

        public static Maybe<T> operator &(Maybe<T> left, Maybe<T> right) => left._value is null ? left : right;

        public static Maybe<T> operator |(Maybe<T> left, Maybe<T> right) => left._value is null ? right : left;

        public static T operator |(Maybe<T> left, T right) => left._value is null ? right : left._value;

        public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

        public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

        public static implicit operator Maybe<T>(T? value) => value is null ? default : new(value);

        public static implicit operator T?(Maybe<T> value) => value._value;
    }

    public static class Maybe
    {
        public static Maybe<T> None<T>() where T : notnull => default;

        public static Maybe<T> Some<T>(T value) where T : notnull => value;

        [Obsolete("Baaad", true)]
        public static T ValueUnsafe<T>(this Maybe<T> maybeT) where T : notnull

            => maybeT.Match(
                some: t => t,
                none: () => throw new InvalidOperationException());

        public static Maybe<TR> Apply<T, TR>(this Maybe<Func<T, TR>> maybeFunc, Maybe<T> maybeT)
            where T : notnull
            where TR : notnull

            => maybeFunc.Match(
                some: f => maybeT.Match(
                    some: t => Some(f(t)),
                    none: None<TR>),
                none: None<TR>);

        public static Maybe<TR> Bind<T, TR>(this Maybe<T> maybeT, Func<T, Maybe<TR>> func)
            where T : notnull
            where TR : notnull

            => maybeT.Match(
                some: func,
                none: None<TR>);

        public static Maybe<TR> Map<T, TR>(this Maybe<T> maybeT, Func<T, TR> func)
            where T : notnull
            where TR : notnull

            => maybeT.Match(
                some: t => Some(func(t)),
                none: None<TR>);

        public static Maybe<TResult> Select<TSource, TResult>(
            this Maybe<TSource> source,
            Func<TSource, TResult> selector)
            where TSource : notnull
            where TResult : notnull

            => source.Map(selector);

        public static Maybe<TResult> SelectMany<TSource, TNext, TResult>(
            this Maybe<TSource> source,
            Func<TSource, Maybe<TNext>> selector,
            Func<TSource, TNext, TResult> resultSelector)
            where TSource : notnull
            where TNext : notnull
            where TResult : notnull

            => source.Match(
                some: s => selector(s).Match(
                    some: n => Some(resultSelector(s, n)),
                    none: None<TResult>),
                none: None<TResult>);

        public static bool Try<T>(this Maybe<T> source, out T value) where T : notnull
        {
            T? nullable = source;

            if (nullable is null)
            {
                value = default!;
                return false;
            }
            else
            {
                value = nullable;
                return true;
            }
        }

        public static Maybe<TSource> Where<TSource>(this Maybe<TSource> source, Func<TSource, bool> predicate)
            where TSource : notnull

            => source.Bind(s => predicate(s) ? source : None<TSource>());
    }
}
