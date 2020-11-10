using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Luger.Functional
{
    public readonly struct Maybe<T> : IEquatable<Maybe<T>>
    {
        private readonly T _value;

        public bool IsSome { get; }

        internal Maybe(T value)
        {
            _value = value;
            IsSome = value != null;
        }

        internal TR MatchInternal<TR>(Func<T, TR> some, Func<TR> none) =>
            IsSome ? some(_value) : none();

        public TR Match<TR>(Func<T, TR> some, Func<TR> none) =>
            MatchInternal(
                some ?? throw new ArgumentNullException(nameof(some)),
                none ?? throw new ArgumentNullException(nameof(none)));

        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "maybe1.IsSome ? maybe1 : maybe2")]
        public static Maybe<T> operator |(Maybe<T> maybe1, Maybe<T> maybe2)
            => maybe1.IsSome ? maybe1 : maybe2;

        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "maybe.Match(() => t, v => v)")]
        public static T operator |(Maybe<T> maybe, T t)
            => maybe.IsSome ? maybe._value : t;

        public string ToString(Func<T, string?> formatter)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            return IsSome ? $"Some({formatter(_value)})" : "None";
        }

        public override string ToString() => ToString(t => t?.ToString());

        private static readonly IEqualityComparer<T> ValueEqualityComparer = EqualityComparer<T>.Default;

        public bool Equals(Maybe<T> other) =>
            IsSome
                ? other.IsSome && ValueEqualityComparer.Equals(_value, other._value)
                : !other.IsSome;

        public override bool Equals(object? obj) =>
            obj is Maybe<T> other && Equals(other);

        public override int GetHashCode() =>
            IsSome
                ? ValueEqualityComparer.GetHashCode(_value!)
                : 0;

        public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

        public static bool operator !=(Maybe<T> left, Maybe<T> right) => !(left == right);
    }

    public static class Maybe
    {
        public static Maybe<T> None<T>() => default;

        public static Maybe<T> Some<T>(T value) => new Maybe<T>(value ?? throw new ArgumentNullException(nameof(value)));

        public static T ValueUnsafe<T>(this Maybe<T> maybeT) =>
            maybeT.MatchInternal(
                some: t => t,
                none: () => throw new InvalidOperationException());

        public static Maybe<TR> Apply<T, TR>(this Maybe<Func<T, TR>> maybeFunc, Maybe<T> maybeT) =>
            maybeFunc.MatchInternal(
                some: f => maybeT.MatchInternal(
                    some: t => Some(f(t)),
                    none: None<TR>),
                none: None<TR>);

        public static Maybe<TR> Bind<T, TR>(this Maybe<T> maybeT, Func<T, Maybe<TR>> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            return maybeT.MatchInternal(
                some: func,
                none: None<TR>);
        }

        public static Maybe<TR> Map<T, TR>(this Maybe<T> maybeT, Func<T, TR> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            return maybeT.MatchInternal(
                some: t => Some(func(t)),
                none: None<TR>);
        }

        public static Maybe<TR> Select<T, TR>(this Maybe<T> maybeT, Func<T, TR> func) => maybeT.Map(func);

        public static Maybe<TR> SelectMany<T, TR>(this Maybe<T> maybeT, Func<T, Maybe<TR>> func) => maybeT.Bind(func);

        public static Maybe<TR> SelectMany<T, TC, TR>(this Maybe<T> maybeT, Func<T, Maybe<TC>> func, Func<T, TC, TR> proj)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));

            if (proj is null)
                throw new ArgumentNullException(nameof(proj));

            return maybeT.MatchInternal(
                some: t => func(t).MatchInternal(
                    some: tc => Some(proj(t, tc)),
                    none: None<TR>),
                none: None<TR>);
        }

        public static Maybe<T> Where<T>(this Maybe<T> maybeT, Func<T, bool> func)
            => maybeT.Bind(t => func(t) ? maybeT : None<T>());

        public static IEnumerable<T> AsEnumerable<T>(this Maybe<T> maybeT) =>
            maybeT.MatchInternal(
                some: EnumerableExt.Return,
                none: EnumerableExt.Empty<T>);
    }
}
