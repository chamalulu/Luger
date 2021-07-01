using System;
using System.Collections;
using System.Collections.Generic;

namespace Luger.Functional
{
    public readonly struct Maybe<T> : IEquatable<Maybe<T>>, IEnumerable<T> where T : notnull
    {
        private readonly T _value;

        public bool IsSome { get; }

        internal Maybe(T value)
        {
            _value = value;
            IsSome = true;
        }

        public TR Match<TR>(Func<T, TR> some, Func<TR> none) => IsSome ? some(_value) : none();

        public static Maybe<T> operator &(Maybe<T> left, Maybe<T> right) => left.IsSome ? right : left;

        public static Maybe<T> operator |(Maybe<T> left, Maybe<T> right) => left.IsSome ? left : right;

        public static T operator |(Maybe<T> left, T right) => left.IsSome ? left._value : right;

        public string ToString(Func<T, string?> formatter) => IsSome ? $"Some({formatter(_value)})" : "None";

        public override string ToString() => ToString(t => t.ToString());

        private static readonly IEqualityComparer<T> ValueEqualityComparer = EqualityComparer<T>.Default;

        public bool Equals(Maybe<T> other)

            => IsSome
                ? other.IsSome && ValueEqualityComparer.Equals(_value, other._value)
                : !other.IsSome;

        public override bool Equals(object? obj) => obj is Maybe<T> other && Equals(other);

        public override int GetHashCode() => IsSome ? ValueEqualityComparer.GetHashCode(_value) : 0;

        public IEnumerator<T> GetEnumerator()
        {
            if (IsSome)
            {
                yield return _value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

        public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

        public static implicit operator Maybe<T>(T? value) => value is not null ? new(value) : default;

        public static implicit operator T?(Maybe<T> value) => value.IsSome ? value._value : default;
    }

    public static class Maybe
    {
        public static Maybe<T> None<T>() where T : notnull => default;

        public static Maybe<T> Some<T>(T value) where T : notnull => new(value);

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

        public static Maybe<TR> Bind<T, TR>(this Maybe<T> maybeT, Func<T, Maybe<TR>> func) where T : notnull where TR : notnull

            => maybeT.Match(
                some: func,
                none: None<TR>);

        public static Maybe<TR> Map<T, TR>(this Maybe<T> maybeT, Func<T, TR> func) where T : notnull where TR : notnull

            => maybeT.Match(
                some: t => Some(func(t)),
                none: None<TR>);

        public static Maybe<TResult> Select<TSource, TResult>(this Maybe<TSource> source, Func<TSource, TResult> selector)
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

        public static Maybe<TSource> Where<TSource>(this Maybe<TSource> source, Func<TSource, bool> predicate)
            where TSource : notnull

            => source.Bind(s => predicate(s) ? source : None<TSource>());
    }
}
