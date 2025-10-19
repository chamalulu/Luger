using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
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
[DebuggerStepThrough, JsonConverter(typeof(MaybeConverterFactory))]
public readonly struct Maybe<T> : IEquatable<Maybe<T>>, IFormattable, IEnumerable<T> where T : notnull
{
    /* I've tried using T? as inner state, but it doesn't work. A similar problem is illustrated here
     * (https://github.com/dotnet/roslyn/issues/53139).
     * The short story is; Nullability is less bad since C# 8, but still subtly broken.
     * A field of type T? where T is not constrained to reference or value type (here only notnull) will be considered
     * a nullable reference type T? if the constructed type parameter T is a reference type but, strangely,
     * a non-nullable value type T if the constructed type parameter T is a value type. Madness.
     * The nullability stuff in C# could need some reworking but since that would certainly become backwards
     * incompatible maybe C# just has to bite the bullet and leave strong typing to modern languages.
     */
    readonly bool _isSome;
    readonly T _value;

    internal Maybe(T value)
    {
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
    [PublicAPI]
    public int Count => _isSome ? 1 : 0;

    /// <summary>
    /// Gets the element at the specified <paramref name="index"/>
    /// </summary>
    /// <param name="index">Index of value. Must be 0.</param>
    /// <remarks>
    /// <para>Provided for support of List Pattern of C# 11.</para>
    /// <para>
    /// Don't use this property directly. It's as misbehaving as
    /// <see cref="Nullable{T}.Value">Nullable&lt;T&gt;.Value</see>.
    /// </para>
    /// <para>
    /// Before version 2.0 we threw <see cref="IndexOutOfRangeException"/> here because I thought it's the most
    /// appropriate.
    /// <see href="https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2201">CA2201</see>
    /// rules this out as that exception is reserved for the CLR.
    /// </para>
    /// <para>
    /// One could argue for throwing <see cref="ArgumentOutOfRangeException"/> which
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.collections.ilist.item?view=net-8.0">IList&lt;T&gt;.Item[]</see>
    /// throw.<br/>
    /// <see cref="Maybe{T}"/> is not a list. Not even a very short one.
    /// </para>
    /// <para>
    /// One could argue for throwing <see cref="InvalidOperationException"/> which
    /// <see cref="Nullable{T}.Value">Nullable&lt;T&gt;.Value</see> throw.<br/>
    /// <see cref="Maybe{T}"/> is a better <see cref="Nullable{T}"/>. That will do.
    /// </para>
    /// </remarks>
    /// <returns>Value if this is some and <paramref name="index"/> is 0.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this is none or <paramref name="index"/> is not 0.<br/>
    /// I trust the C# compiler never to trigger this.
    /// </exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [PublicAPI]
    public T this[int index] => _isSome && index == 0 ? _value : throw new InvalidOperationException();

    static readonly EqualityComparer<T> ValueEqualityComparer = EqualityComparer<T>.Default;

    /// <summary>
    /// Non-boxing equality comparison. Delegates to <see cref="EqualityComparer{T}.Equals(T, T)"/> of
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <param name="other"><see cref="Maybe{T}"/> comparand</param>
    /// <returns>
    /// <see langword="true"/> if values have the same state and if some, the same value;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If <typeparamref name="T"/> implements <see cref="IEquatable{T}"/>, that implementation is used to compare some
    /// values; otherwise, reference equality or default value type equality is used to compare some values.
    /// </remarks>
    [PublicAPI]
    public bool Equals(Maybe<T> other) => _isSome
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
    [PublicAPI]
    public override bool Equals(object? obj) => _isSome ? _value.Equals(obj) : obj is null;

    /// <summary>
    /// Produces hash code of value for simple collision check purposes. Delegates to
    /// <see cref="EqualityComparer{T}.GetHashCode(T)"/> of <see cref="EqualityComparer{T}.Default"/> in some case.
    /// </summary>
    /// <returns>Hash code of value in some case; otherwise 0.</returns>
    [PublicAPI]
    public override int GetHashCode() => _isSome ? ValueEqualityComparer.GetHashCode(_value) : 0;

    struct Enumerator(Maybe<T> maybe) : IEnumerator<T>
    {
        bool _moved;

        public readonly T Current => maybe._value;

        readonly object IEnumerator.Current => Current;

        public readonly void Dispose() { }

        public bool MoveNext() => !_moved && maybe._isSome && (_moved = true);

        public void Reset() => _moved = false;
    }

    /// <summary>
    /// Produces an <see cref="IEnumerator{T}"/> over this <see cref="Maybe{T}"/>.
    /// </summary>
    /// <returns>
    /// An enumerator yielding the value in some case; otherwise not yielding any value.
    /// </returns>
    [PublicAPI]
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
    [PublicAPI]
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (!_isSome)
        {
            return "[]";
        }

        var valueRepresentation = _value is IFormattable formattable
            ? formattable.ToString(format, formatProvider)
            : _value.ToString() ?? string.Empty;

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
    [PublicAPI]
    public override string ToString() => ToString(null, null);

    /// <remarks>
    /// Some is represented as "[&lt;value&gt;]".<br/>
    /// <paramref name="format"/> will be passed to <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> on
    /// the value if it is <see cref="IFormattable"/>; otherwise <see cref="object.ToString()"/> is used to produce the
    /// value representation.<br/>
    /// None is represented as "[]".
    /// </remarks>
    /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)"/>
    [PublicAPI]
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
    [PublicAPI]
    public static bool operator true(Maybe<T> value) => value._isSome;

    /// <summary>
    /// Falsity value of <paramref name="value"/>
    /// </summary>
    /// <remarks>
    /// This operator plays a role implementing the conditional logical conjunction operator
    /// (<see langword="&amp;&amp;"/>) together with the logical conjunction operator (<see langword="&amp;"/>).
    /// </remarks>
    /// <returns><see langword="true"/> in none case; otherwise <see langword="false"/></returns>
    [PublicAPI]
    public static bool operator false(Maybe<T> value) => !value._isSome;

    /// <summary>
    /// Logical conjunction of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <remarks>
    /// <c>maybeX &amp; maybeY &amp; maybeZ</c> evaluates to the rightmost operand (<c>maybeZ</c>) if all are some;
    /// otherwise none.<br/>
    /// </remarks>
    /// <returns><paramref name="left"/> if it is none; otherwise <paramref name="right"/></returns>
    [PublicAPI]
    public static Maybe<T> operator &(Maybe<T> left, Maybe<T> right) => left._isSome ? right : left;

    /// <summary>
    /// Logical disjunction of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <remarks>
    /// <c>maybeX | maybeY | maybeZ</c> evaluates to the leftmost operand which is some; otherwise none.<br/>
    /// </remarks>
    /// <returns><paramref name="left"/> if it is some; otherwise <paramref name="right"/></returns>
    [PublicAPI]
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
    [PublicAPI]
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
    [PublicAPI]
    public static T operator |(Maybe<T> left, Func<T> right) => left._isSome ? left._value : right();

    /// <summary>
    /// Equality operator of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if operands have same state and in some case, the same value;
    /// otherwise <see langword="false"/>
    /// </returns>
    [PublicAPI]
    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    /// <summary>
    /// Inequality operator of operands <paramref name="left"/> and <paramref name="right"/>
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if operands have different state or in some case, different value;
    /// otherwise <see langword="false"/>
    /// </returns>
    [PublicAPI]
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

    /// <summary>
    /// Implicit cast from <typeparamref name="T"/> to <see cref="Maybe{T}"/>
    /// </summary>
    [PublicAPI]
    public static implicit operator Maybe<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Maybe<T>(value);
    }
}

/// <summary>
/// Factory and extension methods for <see cref="Maybe{T}"/>
/// </summary>
/// <remarks>
/// A couple of useful extensions for <see cref="IEnumerable{T}"/> are also provided.
/// </remarks>
public static class Maybe
{
    /// <summary>Factory method for <see cref="Maybe{T}"/> with state none.</summary>
    /// <remarks>If target type is known, prefer using empty collection initializer.</remarks>
    [PublicAPI]
    public static Maybe<T> None<T>() where T : notnull => [];

    /// <summary>Factory method for <see cref="Maybe{T}"/> with state some <paramref name="value"/>.</summary>
    /// <remarks>If target type is known, prefer using implicit cast from <typeparamref name="T"/>.</remarks>
    [PublicAPI]
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
    [PublicAPI]
    public static Maybe<TResult> Apply<TArg, TResult>(this Maybe<Func<TArg, TResult>> maybeFunc, Maybe<TArg> maybeArg)
        where TArg : notnull
        where TResult : notnull =>
        maybeFunc is [var func] && maybeArg is [var arg]
            ? new Maybe<TResult>(func(arg))
            : [];

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
    [PublicAPI]
    public static Maybe<TResult> Bind<TSource, TResult>(this Maybe<TSource> source, Func<TSource, Maybe<TResult>> func)
        where TSource : notnull
        where TResult : notnull =>
        source is [var s]
            ? func(s)
            : [];

    /// <summary>
    /// Filters a <see cref="Maybe{T}"/> value based on a predicate function.
    /// </summary>
    /// <typeparam name="TSource">The type of the value of <paramref name="source"/></typeparam>
    /// <param name="source">A <see cref="Maybe{T}"/> to filter.</param>
    /// <param name="predicate">A function to test some value for a condition.</param>
    /// <returns>
    /// The <paramref name="source"/> in some case and the value satisfy the condition; otherwise none.
    /// </returns>
    [PublicAPI]
    public static Maybe<TSource> Filter<TSource>(this Maybe<TSource> source, Func<TSource, bool> predicate)
        where TSource : notnull =>
        source is [var s] && predicate(s)
            ? source
            : [];

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
    [PublicAPI]
    public static Maybe<TResult> Map<TSource, TResult>(this Maybe<TSource> source, Func<TSource, TResult> func)
        where TSource : notnull
        where TResult : notnull =>
        source is [var s]
            ? new Maybe<TResult>(func(s))
            : [];

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
    [PublicAPI]
    public static Maybe<TResult> Select<TSource, TResult>(this Maybe<TSource> source, Func<TSource, TResult> selector)
        where TSource : notnull
        where TResult : notnull =>
        source.Map(selector);

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
    [PublicAPI]
    public static Maybe<TResult> SelectMany<TSource, TNext, TResult>(
        this Maybe<TSource> source,
        Func<TSource, Maybe<TNext>> selector,
        Func<TSource, TNext, TResult> resultSelector)
        where TSource : notnull
        where TNext : notnull
        where TResult : notnull =>
        source is [var s] && selector(s) is [var n]
            ? new Maybe<TResult>(resultSelector(s, n))
            : [];

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
    [PublicAPI]
    public static async Task<Maybe<TResult>> Traverse<TSource, TResult>(
        this Maybe<TSource> source,
        Func<TSource, Task<TResult>> func)
        where TSource : notnull
        where TResult : notnull =>
        source is [var s]
            ? new Maybe<TResult>(await func(s))
            : [];

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
    [PublicAPI]
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
    [PublicAPI]
    public static Maybe<TSource> Where<TSource>(this Maybe<TSource> source, Func<TSource, bool> predicate)
        where TSource : notnull =>
        source.Filter(predicate);

    /* FromNullable, FromReference, ToNullable and ToReference may seem a bit redundant. Their implementations w.r.t.
     * ...Nullable vs. ...Reference are syntactically equivalent (except for the type parameter constraints).
     * The reason for separate implementations for value types and reference types is that the handling of nullability
     * is very different at runtime and trying to be generic about it (using notnull type constraint) makes the C#
     * compiler very confused indeed.
     * Nullable value types are implemented with the strongly typed Nullable<T> struct and a lot of special handling in
     * the compiler. At runtime T and T? are very distinct w.r.t. value types.
     * Nullable reference types are implemented by an attribute and some compile time rules. At runtime T and T? are
     * in practice the same w.r.t. reference types.
     * To avoid a lot of squiggly lines in the IDE, confused warnings and type parameter inference bugs I choose to
     * handle value types and reference types separately here.
     */

    /// <summary>
    /// Conversion from <see cref="Nullable{T}"/> to <see cref="Maybe{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of some value</typeparam>
    /// <param name="value">Nullable value to convert</param>
    /// <returns>A <see cref="Maybe{T}"/> with <paramref name="value"/> if it has one; otherwise none.</returns>
    [PublicAPI]
    public static Maybe<T> FromNullable<T>(T? value) where T : struct =>
        value.HasValue ? new Maybe<T>(value.Value) : [];

    /// <summary>
    /// Conversion from nullable reference type <typeparamref name="T"/>? to <see cref="Maybe{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of some value</typeparam>
    /// <param name="value">Nullable reference to convert</param>
    /// <returns>A <see cref="Maybe{T}"/> with <paramref name="value"/> if it has one; otherwise none.</returns>
    [PublicAPI]
    public static Maybe<T> FromReference<T>(T? value) where T : class =>
        value is not null ? new Maybe<T>(value) : [];

    /// <summary>
    /// Conversion from <see cref="Maybe{T}"/> where <typeparamref name="T"/> is a value type to
    /// <see cref="Nullable{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of some value</typeparam>
    /// <param name="value"><see cref="Maybe{T}"/> to convert</param>
    /// <returns>A <see cref="Nullable{T}"/> with <paramref name="value"/> in some case.</returns>
    [PublicAPI]
    public static T? ToNullable<T>(this Maybe<T> value) where T : struct => value is [var t] ? t : null;

    /// <summary>
    /// Conversion from <see cref="Maybe{T}"/> where <typeparamref name="T"/> is a non-nullable reference type to
    /// nullable reference type <typeparamref name="T"/>?.
    /// </summary>
    /// <typeparam name="T">Type of some value</typeparam>
    /// <param name="value"><see cref="Maybe{T}"/> to convert</param>
    /// <returns>
    /// A nullable reference to value of <paramref name="value"/> in some case; otherwise <see langword="null"/>.
    /// </returns>
    [PublicAPI]
    public static T? ToReference<T>(this Maybe<T> value) where T : class => value is [var t] ? t : null;

    /// <summary>
    /// Returns some only element of the input sequence, or none if the sequence is empty. This method throws an
    /// exception if there is more than one element in the sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements of <paramref name="source"/></typeparam>
    /// <param name="source">An<see cref="IEnumerable{T}"/> to return the single element of.</param>
    /// <returns>Some only element of the input sequence, or none if the sequence contains no elements.</returns>
    /// <exception cref="InvalidOperationException">The input sequence contains more than one element.</exception>
    [PublicAPI]
    public static Maybe<T> MaybeSingle<T>(this IEnumerable<T> source) where T : notnull
    {
        if (source is IList<T> list)
        {
            return list switch
            {
                // Allocation free in happy path
                [] => [],
                [var element] => new Maybe<T>(element),
                _ => throw new InvalidOperationException()
            };
        }

        using var enumerator = source.GetEnumerator();

        // ReSharper disable once InvertIf . I want the scope for element.
        if (enumerator.MoveNext())
        {
            var element = enumerator.Current;

            return enumerator.MoveNext()
                ? throw new InvalidOperationException()
                : new Maybe<T>(element);
        }

        return [];
    }

    /// <summary>
    /// Returns some only element of a sequence that satisfies a specified condition or none if no such element exists.
    /// This method throws an exception if more than one element satisfies the condition.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return a single element from.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <returns>
    /// Some single element of the input sequence that satisfies the condition in <paramref name="predicate"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// More than one element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    [PublicAPI]
    public static Maybe<T> MaybeSingle<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : notnull

        => source.Where(predicate).MaybeSingle();

    /// <summary>
    /// Returns some first element of a sequence, or none if the sequence contains no elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to return some first element of.</param>
    /// <returns>Some first element in <paramref name="source"/> if not empty; otherwise none.</returns>
    [PublicAPI]
    public static Maybe<T> MaybeFirst<T>(this IEnumerable<T> source) where T : notnull
    {
        if (source is IList<T> list)
        {
            // Allocation free
            return list is [var element, ..] ? new Maybe<T>(element) : [];
        }

        using var enumerator = source.GetEnumerator();

        return enumerator.MoveNext() ? new Maybe<T>(enumerator.Current) : [];
    }

    /// <summary>
    /// Returns some first element of the sequence that satisfies a condition or none if no such element is found.
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <returns>
    /// Some first element in <paramref name="source"/> that passes the test specified by <paramref name="predicate"/>
    /// if such an element is found; otherwise none.
    /// </returns>
    [PublicAPI]
    public static Maybe<T> MaybeFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : notnull

        => source.Where(predicate).MaybeFirst();
}
