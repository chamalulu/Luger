using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

namespace Luger.Functional;

/// <summary>
/// Composable version of <see cref="Nullable{T}"/> for value types and non-nullable reference types.
/// </summary>
/// <remarks>
/// <para>
/// The values can be in one of two main states; Some or None.<br/>
/// Some is analogous to a <see cref="Nullable{T}"/> or nullable reference with a value.<br/>
/// None is analogous to a <see cref="Nullable{T}"/> or nullable reference without a value, i.e. the infamous
/// <see langword="null"/>.
/// </para>
/// <para>
/// You can pattern match against values of <see cref="Maybe{T}"/> by using C# 11
/// <see href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns#list-patterns">
/// List Patterns
/// </see>.
/// <code>
/// Console.WriteLine(maybeT is [var t] ? $"Got some {t}!" : "Got none.");
/// Console.WriteLine(maybeT is [] ? "Got none." : "Got some!");
/// </code>
/// </para>
/// <para>
/// <see cref="Maybe{T}"/> implements <see cref="IEquatable{T}"/> where <typeparamref name="T"/> is
/// <see cref="Maybe{T}"/> and overrides <see cref="object.Equals(object?)"/>.<br/>
/// The comparison works in the same way as equality comparison in <see cref="Nullable{T}"/>.
/// </para>
/// <para>
/// <see cref="Maybe{T}"/> implements <see cref="IFormattable"/> which implementation <see cref="ToString()"/> and
/// <see cref="ToString(string?)"/> delegates to.
/// </para>
/// <para>
/// <see cref="Maybe{T}"/> implements <see cref="IEnumerable{T}"/>. The enumerator will yield zero or one element for
/// none or some respectively. This enables <see cref="Maybe{T}"/> to be functionally bound (flattened) together with
/// any <see cref="IEnumerable{T}"/>.
/// <code>
/// var flattened = from x in xs from t in funcMaybe(x) select t; // some results from funcMaybe(x) are filtered.
/// </code>
/// </para>
/// <para>
/// <see cref="Maybe{T}"/> implements truth (<see langword="true"/>, <see langword="false"/>) and logical conjunction
/// (<see langword="&amp;"/>) and disjunction (<see langword="|"/>) operators. This combination also provides
/// conditional logical operators (<see langword="&amp;&amp;"/>, <see langword="||"/>). This enables chaining of
/// <see cref="Maybe{T}"/> values in logical expressions.<br/>
/// Using the conditional operators enables on-demand evaluation as expected.<br/>
/// </para>
/// <para>
/// <see cref="Maybe{T}"/> implements implicit cast operator from <typeparamref name="T"/>.<br/>
/// Thus, returning some value from a <see cref="Maybe{T}"/>-returning function is no effort.<br/>
/// Returning none from a <see cref="Maybe{T}"/>-returning function is equally simple as it is the default state;
/// <c>return default;</c>.
/// </para>
/// </remarks>
[DebuggerStepThrough]
public readonly struct Maybe<T> : IEquatable<Maybe<T>>, IFormattable, IEnumerable<T> where T : notnull
{
    /* I've tried using T? as inner state but it gets nasty as it is either a T or a Nullable<T> at runtime depending on
     * wether T is a reference type or value type.
     * The nullability stuff in C# could need some reworking but since that would certainly become backwards
     * incompatible maybe C# just has to bite the bullet and leave strong typing to modern languages.
     */
    readonly bool _isSome;
    readonly T _value;

    Maybe(T value)
    {
        // The following guard is necessary when T is a reference type. :(
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        _isSome = true;
        _value = value;
    }

    /// <summary>
    /// Gets the number of elements contained in the <see cref="Maybe{T}"/>
    /// </summary>
    /// <remarks>
    /// Provided for support of List Pattern of C# 11.<br/>
    /// Use this property directly if you like, but it'll look rather silly.
    /// </remarks>
    /// <value>1 if this is some; otherwise 0.</value>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public int Count => _isSome ? 1 : 0;

    /// <summary>
    /// Gets the element at the specified <paramref name="index"/>
    /// </summary>
    /// <param name="index">Index of value. Must be 0.</param>
    /// <remarks>
    /// Provided for support of List Pattern of C# 11.<br/>
    /// Don't use this property directly. It's as misbehaving as <see cref="Nullable{T}.Value"/>.
    /// </remarks>
    /// <returns>Value if this is some and <paramref name="index"/> is 0.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if this is none or <paramref name="index"/> is not 0.<br/>
    /// I trust the C# compiler never to trigger this.
    /// </exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public T this[int index]

        => _isSome && index == 0
            ? _value
            : throw new IndexOutOfRangeException();

    static readonly IEqualityComparer<T> ValueEqualityComparer = EqualityComparer<T>.Default;

    /// <summary>
    /// Non-boxing equality comparison. Delegates to <see cref="IEqualityComparer{T}.Equals(T, T)"/> of
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <param name="other"><see cref="Maybe{T}"/> comparand</param>
    /// <returns>
    /// <see langword="true"/> if values have the same state and if some, the same value;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public bool Equals(Maybe<T> other)

        => _isSome
            ? other._isSome && ValueEqualityComparer.Equals(_value, other._value)
            : !other._isSome;

    /// <summary>
    /// Possibly boxing equality comparison. Delegates to <see cref="object.Equals(object?)"/> in some case.
    /// </summary>
    /// <param name="obj">comparand</param>
    /// <returns>
    /// <see langword="true"/> if some and value equals <paramref name="obj"/> or none and <see langword="null"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj) => _isSome ? _value.Equals(obj) : obj is null;

    /// <summary>
    /// Produces hash code of value for simple collision check purposes. Delegates to
    /// <see cref="IEqualityComparer{T}.GetHashCode(T)"/> of <see cref="EqualityComparer{T}.Default"/> in some case.
    /// </summary>
    /// <returns>Hash code of value in some case; otherwise 0.</returns>
    public override int GetHashCode() => _isSome ? ValueEqualityComparer.GetHashCode(_value) : 0;

    struct Enumerator : IEnumerator<T>
    {
        readonly Maybe<T> _maybe;
        bool _moved;

        public Enumerator(Maybe<T> maybe) => _maybe = maybe;

        public T Current => _maybe._value;

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext() => !_moved && _maybe._isSome && (_moved = true);

        public void Reset() => _moved = false;
    }

    /// <summary>
    /// Produces an <see cref="IEnumerator{T}"/> over this <see cref="Maybe{T}"/>.
    /// </summary>
    /// <returns>
    /// An enumerator yielding the value in some case; otherwise not yielding any value.
    /// </returns>
    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <remarks>
    /// Some is represented as "[&lt;value&gt;]".<br/>
    /// <paramref name="format"/> and <paramref name="formatProvider"/> will be passed to the same method on the value
    /// if it is <see cref="IFormattable"/>; otherwise <see cref="object.ToString()"/> is used to produce the value
    /// representation.<br/>
    /// None is represented as "[]".
    /// </remarks>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var valueRepresentation = _isSome
            ? _value is IFormattable formattable
                ? formattable.ToString(format, formatProvider)
                : _value.ToString() ?? string.Empty
            : string.Empty;

        return $"[{valueRepresentation}]";
    }

    /// <summary>
    /// Produces the string representation of the value
    /// </summary>
    /// <remarks>
    /// Some is represented as "[&lt;value&gt;]".<br/>
    /// <see cref="object.ToString()"/> is used to produce the value representation.<br/>
    /// None is represented as "[]".
    /// </remarks>
    public override string ToString() => ToString(null, null);

    /// <remarks>
    /// Some is represented as "[&lt;value&gt;]".<br/>
    /// <paramref name="format"/> will be passed to <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> on
    /// the value if it is <see cref="IFormattable"/>; otherwise <see cref="object.ToString()"/> is used to produce the
    /// value representation.<br/>
    /// None is represented as "[]".
    /// </remarks>
    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)"/>
    public string ToString(string? format) => ToString(format, null);

    /// <summary>
    /// Truth value of <paramref name="value"/>
    /// </summary>
    /// <remarks>
    /// This operator is used when a <see cref="Maybe{T}"/> value is used in a controlling conditional expression in
    /// e.g. <see langword="if"/>, <see langword="do"/>, <see langword="while"/> and <see langword="for"/> statements
    /// and the ternary conditional operator (<see langword="?:"/>).<br/>
    /// It also plays a role implementing the conditional logical disjunction operator (<see langword="||"/>) together
    /// with the logical disjunction operator (<see langword="|"/>).
    /// </remarks>
    /// <returns><see langword="true"/> in some case; otherwise <see langword="false"/></returns>
    public static bool operator true(Maybe<T> value) => value._isSome;

    /// <summary>
    /// Falsity value of <paramref name="value"/>
    /// </summary>
    /// <remarks>
    /// This operator plays a role implementing the conditional logical conjunction operator
    /// (<see langword="&amp;&amp;"/>) together with the logical conjunction operator (<see langword="&amp;"/>).
    /// </remarks>
    /// <returns><see langword="true"/> in none case; otherwise <see langword="false"/></returns>
    public static bool operator false(Maybe<T> value) => !value._isSome;

    /// <summary>
    /// Logical conjunction of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <remarks>
    /// <c>maybeX &amp; maybeY &amp; maybeZ</c> evaluates to the rightmost operand (<c>maybeZ</c>) if all are some;
    /// otherwise none.<br/>
    /// </remarks>
    /// <returns><paramref name="left"/> if it is none; otherwise <paramref name="right"/></returns>
    public static Maybe<T> operator &(Maybe<T> left, Maybe<T> right) => left._isSome ? right : left;

    /// <summary>
    /// Logical disjunction of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <remarks>
    /// <c>maybeX | maybeY | maybeZ</c> evaluates to the leftmost operand which is some; otherwise none.<br/>
    /// </remarks>
    /// <returns><paramref name="left"/> if it is some; otherwise <paramref name="right"/></returns>
    public static Maybe<T> operator |(Maybe<T> left, Maybe<T> right) => left._isSome ? left : right;

    /// <summary>
    /// Logical disjunction of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <remarks>
    /// This operator provides semantically the same functionality as
    /// <see cref="Nullable{T}.GetValueOrDefault(T)"/>. It can be used at the end of a chain of disjunctions to provide
    /// a fallback value.<br/>
    /// <c>maybeX | maybeY | z</c> evaluates to the value of the leftmost operand which is some; otherwise <c>z</c>.
    /// </remarks>
    /// <returns>Value of <paramref name="left"/> if it is some; otherwise <paramref name="right"/></returns>
    public static T operator |(Maybe<T> left, T right) => left._isSome ? left._value : right;

    /// <summary>
    /// Logical disjunction of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <remarks>
    /// This operator provides lazy fallback value much like <c>maybeX || z</c> would if C# could handle conditional
    /// logical disjunction of different types.<br/>
    /// <c>maybeX | getZ</c> evaluates to the value of <c>maybeX</c> if it is some; otherwise the return value of
    /// <c>getZ</c>.
    /// </remarks>
    /// <returns>
    /// Value of <paramref name="left"/> if it is some; otherwise the return value of <paramref name="right"/>
    /// </returns>
    public static T operator |(Maybe<T> left, Func<T> right) => left._isSome ? left._value : right();

    /// <summary>
    /// Equality operator of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if operands have same state and in some case, the same value;
    /// otherwise <see langword="false"/>
    /// </returns>
    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    /// <summary>
    /// Inequality operator of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if operands have different state or in some case, different value;
    /// otherwise <see langword="false"/>
    /// </returns>
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

    /// <summary>
    /// Implicit cast from <typeparamref name="T"/> to <see cref="Maybe{T}"/>
    /// </summary>
    public static implicit operator Maybe<T>(T value) => new(value);
}

/// <summary>
/// Factory and extension methods for <see cref="Maybe{T}"/>
/// </summary>
public static class Maybe
{
    /// <summary>
    /// Factory method for <see cref="Maybe{T}"/> with state none
    /// </summary>
    public static Maybe<T> None<T>() where T : notnull => default;

    /// <summary>
    /// Factory method for <see cref="Maybe{T}"/> with state some <paramref name="value"/>
    /// </summary>
    public static Maybe<T> Some<T>(T value) where T : notnull => value;

    /// <summary>
    /// Sequential application of <paramref name="maybeFunc"/> to <paramref name="maybeArg"/> in applicative functor of
    /// <see cref="Maybe{T}"/>.
    /// </summary>
    /// <typeparam name="TArg">Type of parameter</typeparam>
    /// <typeparam name="TResult">Type of return value</typeparam>
    /// <param name="maybeFunc">Lifted unary function</param>
    /// <param name="maybeArg">Lifted parameter</param>
    /// <remarks>
    /// This is the equivalent of the infix operator <see langword="&lt;*&gt;"/> of Applicative in Haskell.
    /// </remarks>
    /// <returns>Lifted return value</returns>
    public static Maybe<TResult> Apply<TArg, TResult>(this Maybe<Func<TArg, TResult>> maybeFunc, Maybe<TArg> maybeArg)
        where TArg : notnull
        where TResult : notnull

        => maybeFunc is [var func] && maybeArg is [var arg]
            ? Some(func(arg))
            : None<TResult>();

    /// <summary>
    /// Sequential application of <paramref name="maybeFunc"/> to <paramref name="maybeArg1"/> in applicative functor of
    /// <see cref="Maybe{T}"/>.
    /// </summary>
    /// <typeparam name="TArg1">Type of first parameter</typeparam>
    /// <typeparam name="TArg2">Type of second parameter</typeparam>
    /// <typeparam name="TResult">Type of return value</typeparam>
    /// <param name="maybeFunc">Lifted binary function</param>
    /// <param name="maybeArg1">Lifted parameter</param>
    /// <remarks>
    /// With a little squinting and currying, this is the equivalent of the infix operator <see langword="&lt;*&gt;"/>
    /// of Applicative in Haskell.
    /// </remarks>
    /// <returns>Lifted unary, since partially applied, function</returns>
    public static Maybe<Func<TArg2, TResult>> Apply<TArg1, TArg2, TResult>(
        this Maybe<Func<TArg1, TArg2, TResult>> maybeFunc,
        Maybe<TArg1> maybeArg1)
        where TArg1 : notnull

        => maybeFunc is [var func] && maybeArg1 is [var arg1]
            ? Some<Func<TArg2, TResult>>(arg2 => func(arg1, arg2))
            : None<Func<TArg2, TResult>>();

    /// <summary>
    /// Sequential application of <paramref name="maybeFunc"/> to <paramref name="maybeArg1"/> in applicative functor of
    /// <see cref="Maybe{T}"/>.
    /// </summary>
    /// <typeparam name="TArg1">Type of first parameter</typeparam>
    /// <typeparam name="TArg2">Type of second parameter</typeparam>
    /// <typeparam name="TArg3">Type of third parameter</typeparam>
    /// <typeparam name="TResult">Type of return value</typeparam>
    /// <param name="maybeFunc">Lifted ternary function</param>
    /// <param name="maybeArg1">Lifted parameter</param>
    /// <remarks>
    /// With a little squinting and currying, this is the equivalent of the infix operator <see langword="&lt;*&gt;"/>
    /// of Applicative in Haskell.
    /// </remarks>
    /// <returns>Lifted binary, since partially applied, function</returns>
    public static Maybe<Func<TArg2, TArg3, TResult>> Apply<TArg1, TArg2, TArg3, TResult>(
        this Maybe<Func<TArg1, TArg2, TArg3, TResult>> maybeFunc,
        Maybe<TArg1> maybeArg1)
        where TArg1 : notnull

        => maybeFunc is [var func] && maybeArg1 is [var arg1]
            ? Some<Func<TArg2, TArg3, TResult>>((arg2, arg3) => func(arg1, arg2, arg3))
            : None<Func<TArg2, TArg3, TResult>>();

    /// <summary>
    /// Sequential composition of <paramref name="func"/> to <paramref name="source"/> in monad of
    /// <see cref="Maybe{T}"/>.
    /// </summary>
    /// <typeparam name="TSource">Type of parameter</typeparam>
    /// <typeparam name="TResult">Type of return value</typeparam>
    /// <param name="source">Lifted parameter</param>
    /// <param name="func">Function to bind</param>
    /// <remarks>
    /// This is the equivalent of the infix operator <see langword="&gt;&gt;="/> of Monad in Haskell.
    /// </remarks>
    /// <returns>Lifted return value</returns>
    public static Maybe<TResult> Bind<TSource, TResult>(this Maybe<TSource> source, Func<TSource, Maybe<TResult>> func)
        where TSource : notnull
        where TResult : notnull

        => source is [var s]
            ? func(s)
            : None<TResult>();

    /// <summary>
    /// Filters a <see cref="Maybe{T}"/> value based on a predicate function.
    /// </summary>
    /// <typeparam name="TSource">The type of the value of <paramref name="source"/></typeparam>
    /// <param name="source">A <see cref="Maybe{T}"/> to filter.</param>
    /// <param name="predicate">A function to test some value for a condition.</param>
    /// <returns>
    /// The <paramref name="source"/> in some case and the value satisfy the condition; otherwise none.
    /// </returns>
    public static Maybe<TSource> Filter<TSource>(this Maybe<TSource> source, Func<TSource, bool> predicate)
        where TSource : notnull

        => source is [var s] && predicate(s)
            ? source
            : None<TSource>();

    /// <summary>
    /// Application of <paramref name="func"/> to <paramref name="source"/> in functor of <see cref="Maybe{T}"/>.
    /// </summary>
    /// <typeparam name="TSource">Type of parameter</typeparam>
    /// <typeparam name="TResult">Type of return value</typeparam>
    /// <param name="source">Lifted parameter</param>
    /// <param name="func">Mapping function</param>
    /// <remarks>
    /// This is the equivalent of the infix operator <see langword="&lt;$&gt;"/> of Functor in Haskell.
    /// </remarks>
    /// <returns>Lifted return value</returns>
    public static Maybe<TResult> Map<TSource, TResult>(this Maybe<TSource> source, Func<TSource, TResult> func)
        where TSource : notnull
        where TResult : notnull

        => source is [var s]
            ? Some(func(s))
            : None<TResult>();

    /// <summary>
    /// Projects the value of <see cref="Maybe{T}"/> into a new form.
    /// </summary>
    /// <typeparam name="TSource">The type of the value of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="source">Maybe value to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to some value.</param>
    /// <returns>
    /// A <see cref="Maybe{T}"/> whose value is the result of invoking the transform function on some value of
    /// <paramref name="source"/>.
    /// </returns>
    /// <remarks>
    /// Provided for support of LINQ query syntax mapping in the functor of <see cref="Maybe{T}"/>. The expression
    /// <code>
    /// from s in source
    /// select selector(s)
    /// </code>
    /// is precompiled into
    /// <code>
    /// source.Select(selector)
    /// </code>
    /// This is exactly the same functionality as
    /// <see cref="Maybe.Map{TSource, TResult}(Maybe{TSource}, Func{TSource, TResult})"/> and so
    /// <see cref="Maybe.Select{TSource, TResult}(Maybe{TSource}, Func{TSource, TResult})"/> delegates directly to it.
    /// </remarks>
    public static Maybe<TResult> Select<TSource, TResult>(
        this Maybe<TSource> source,
        Func<TSource, TResult> selector)
        where TSource : notnull
        where TResult : notnull

        => source.Map(selector);

    /// <summary>
    /// Projects some value of <see cref="Maybe{T}"/> to another <see cref="Maybe{T}"/>, and invokes a result selector
    /// function on the pair to produce the result.
    /// </summary>
    /// <typeparam name="TSource">The type of the value of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TNext">
    /// The type of the intermediate value produced by <paramref name="selector"/>.
    /// In the documentation of
    /// <see cref="System.Linq.Enumerable.SelectMany{TSource, TCollection, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TCollection}}, Func{TSource, TCollection, TResult})"/>
    /// (which this documentation is based on in case you haven't noticed) the corresponding type parameter is
    /// <c>TCollection</c>. I think <typeparamref name="TNext"/> is a better name since it is the type of the value
    /// passed to the next composed function in the monadic sequential composition and there are lots of monads beside
    /// sequences.
    /// </typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="resultSelector"/>.</typeparam>
    /// <param name="source">Maybe value to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to the value of <paramref name="source"/>.</param>
    /// <param name="resultSelector">
    /// A projection to apply to the value of <paramref name="source"/> and the value of the return value of
    /// <paramref name="selector"/>.</param>
    /// <returns>
    /// A <see cref="Maybe{T}"/> whose value is the result of invoking the transform function
    /// <paramref name="selector"/> on some value of <paramref name="source"/> and then mapping some result value and
    /// the source value to a result value.
    /// </returns>
    /// <remarks>
    /// Provided for support of LINQ query syntax binding in the monad of <see cref="Maybe{T}"/>. The expression
    /// <code>
    /// from s in source
    /// from n in selector(s)
    /// select resultSelector(s, n)
    /// </code>
    /// is precompiled into
    /// <code>
    /// source.SelectMany(selector, resultSelector)
    /// </code>
    /// The difference between <c>Bind</c> and <c>SelectMany</c> is that <c>SelectMany</c> takes a binary projection
    /// function, <paramref name="resultSelector"/>, as a parameter and as such can chain calls to <c>SelectMany</c>
    /// instead of encapsulating calls to <c>Bind</c> in nested closures.<br/>
    /// <c>SelectMany</c> can be implemented in terms of <c>Bind</c> and <c>Map</c> but type-specific implementations
    /// are probably more efficient.
    /// </remarks>
    public static Maybe<TResult> SelectMany<TSource, TNext, TResult>(
        this Maybe<TSource> source,
        Func<TSource, Maybe<TNext>> selector,
        Func<TSource, TNext, TResult> resultSelector)
        where TSource : notnull
        where TNext : notnull
        where TResult : notnull

        => source is [var s] && selector(s) is [var n]
            ? Some(resultSelector(s, n))
            : None<TResult>();

    /// <summary>
    /// Traverse some value of <see cref="Maybe{T}"/> with an asynchronous function.
    /// </summary>
    /// <typeparam name="TSource">Type of source</typeparam>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <param name="source">Maybe value to traverse</param>
    /// <param name="func"></param>
    /// <returns>
    /// A task yielding a <see cref="Maybe{T}"/> with some result if <paramref name="source"/> is some; otherwise, a
    /// task yielding none.
    /// </returns>
    public static async Task<Maybe<TResult>> Traverse<TSource, TResult>(
        this Maybe<TSource> source,
        Func<TSource, Task<TResult>> func)
        where TSource : notnull
        where TResult : notnull

        => source is [var s]
            ? Some(await func(s))
            : None<TResult>();

    /// <summary>
    /// Code style extension to use Try-style method syntax with a value of <see cref="Maybe{T}"/>
    /// </summary>
    /// <typeparam name="TSource">Type of value</typeparam>
    /// <param name="source">Maybe value</param>
    /// <param name="value">
    /// Inner value in some case; otherwise undefined (<see langword="default"/>! really).
    /// </param>
    /// <returns><see langword="true"/> in some case; otherwise <see langword="false"/></returns>
    /// <remarks>
    /// Try-style methods are methods with signature <c>bool TrySomething&lt;T&gt;(out T result)</c> which offer better
    /// composability than their exception-throwing counterparts. This style can be used to extract the value of
    /// <see cref="Maybe{T}"/> in some case if consuming code is not able to use C# 11 list pattern matching.<br/>
    /// Instead of the expression
    /// <code>
    /// maybeT is [var t] ? $"Some {t}" : "None"
    /// </code>
    /// such code can use
    /// <code>
    /// maybeT.Try(out var value) ? $"Some {value}" : "None"
    /// </code>
    /// </remarks>
    public static bool Try<TSource>(this Maybe<TSource> source, out TSource value) where TSource : notnull
    {
        value = source | default(TSource)!;

        return source is [_];
    }

    /// <summary>
    /// Filters a <see cref="Maybe{T}"/> value based on a predicate.
    /// </summary>
    /// <typeparam name="TSource">The type of the value of <paramref name="source"/></typeparam>
    /// <param name="source">A <see cref="Maybe{T}"/> to filter.</param>
    /// <param name="predicate">A function to test some value for a condition.</param>
    /// <returns>
    /// The <paramref name="source"/> in some case and the value satisfy the condition; otherwise none.
    /// </returns>
    /// <remarks>
    /// Provided for support of LINQ query syntax filtering of <see cref="Maybe{T}"/>. The expression
    /// <code>
    /// from s in source
    /// where predicate(s)
    /// select s
    /// </code>
    /// is precompiled into
    /// <code>
    /// source.Where(predicate)
    /// </code>
    /// </remarks>
    public static Maybe<TSource> Where<TSource>(this Maybe<TSource> source, Func<TSource, bool> predicate)
        where TSource : notnull

        => source.Filter(predicate);

    /// <summary>
    /// Conversion from <see cref="Nullable{T}"/> to <see cref="Maybe{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of some value</typeparam>
    /// <param name="value">Nullable value to convert</param>
    /// <returns>A <see cref="Maybe{T}"/> with <paramref name="value"/> if it has one; otherwise none.</returns>
    public static Maybe<T> FromNullable<T>(T? value) where T : struct

        => value is T v
            ? Some(v)
            : None<T>();

    /// <summary>
    /// Conversion from nullable reference type <typeparamref name="T"/>? to <see cref="Maybe{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of some value</typeparam>
    /// <param name="value">Nullable reference to convert</param>
    /// <returns>A <see cref="Maybe{T}"/> with <paramref name="value"/> if it has one; otherwise none.</returns>
    public static Maybe<T> FromReference<T>(T? value) where T : class

        => value is T v
            ? Some(v)
            : None<T>();

    /// <summary>
    /// Conversion from <see cref="Maybe{T}"/> where <typeparamref name="T"/> is a value type to
    /// <see cref="Nullable{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of some value</typeparam>
    /// <param name="value"><see cref="Maybe{T}"/> to convert</param>
    /// <returns>A <see cref="Nullable{T}"/> with <paramref name="value"/> in some case.</returns>
    public static T? ToNullable<T>(this Maybe<T> value) where T : struct

        => value is [var t] ? t : null;

    /// <summary>
    /// Conversion from <see cref="Maybe{T}"/> where <typeparamref name="T"/> is a non-nullable reference type to
    /// nullable reference type <typeparamref name="T"/>?.
    /// </summary>
    /// <typeparam name="T">Type of some value</typeparam>
    /// <param name="value"><see cref="Maybe{T}"/> to convert</param>
    /// <returns>
    /// A nullable reference to value of <paramref name="value"/> in some case; otherwise <see langword="null"/>.
    /// </returns>
    public static T? ToReference<T>(this Maybe<T> value) where T : class

        => value is [var t] ? t : null;
}
