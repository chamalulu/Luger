namespace Luger.Functional;

/// <summary>
/// Composable generic result type with either a value or an error.
/// </summary>
/// <remarks>
/// <para>
/// The values can be in one of two main states; Ok or Error.<br/>
/// Ok holds a value of type <typeparamref name="T"/>.
/// Error holds a value of type <typeparamref name="TError"/>.
/// </para>
/// <para>
/// Since C# does not yet support closed type hierarchies or sum types, you can not in any useful way pattern match
/// against values of <see cref="Result{TError,T}"/>.
/// To branch over the state of a value of <see cref="Result{TError,T}"/> you use the instance method
/// <see cref="Match{TResult}"/> and provide functions to handle each case.
/// </para>
/// </remarks>
/// <typeparam name="TError"></typeparam>
/// <typeparam name="T"></typeparam>
public readonly struct Result<TError, T> : IEquatable<Result<TError, T>>, IFormattable where TError : class
{
    readonly TError? _error;
    readonly T _value;

    Result(TError error)
    {
        _error = error;
        _value = default!;
    }

    Result(T value)
    {
        _error = null;
        _value = value;
    }

    static readonly EqualityComparer<TError> ErrorEqualityComparer = EqualityComparer<TError>.Default;
    static readonly EqualityComparer<T> ValueEqualityComparer = EqualityComparer<T>.Default;

    /// <summary>
    /// Produces hash code of value for simple collision check purposes. Delegates to
    /// <see cref="EqualityComparer{T}.GetHashCode(T)"/> or <see cref="EqualityComparer{TError}.GetHashCode(TError)"/>
    /// depending on state.
    /// </summary>
    /// <returns>Hash code of value or error. If Ok with null value; 0.</returns>
    public override int GetHashCode() => (_error, _value) switch
    {
        (null, null) => 0,
        (null, _) => ValueEqualityComparer.GetHashCode(_value),
        _ => ErrorEqualityComparer.GetHashCode(_error)
    };

    /// <summary>
    /// Match against state of result, mapping error or value to a common return value.
    /// </summary>
    /// <remarks>
    /// This is as close to pattern matching I can figure out. When C# implements pattern matching over closed type
    /// hierarchies or sum types, <see cref="Result{TError,T}"/> will be obsolete. Maybe in C# 14?
    /// </remarks>
    /// <param name="error">Function to map error to return value</param>
    /// <param name="ok">Function to map value to return value</param>
    /// <typeparam name="TResult">Type of return value</typeparam>
    /// <returns>Return value of either <paramref name="error"/> or <paramref name="ok"/>.</returns>
    public TResult Match<TResult>(Func<TError, TResult> error, Func<T, TResult> ok)

        => _error is null ? ok(_value) : error(_error);

    /// <summary>
    /// Map ok result by given <paramref name="mapping"/>.
    /// </summary>
    /// <param name="mapping">Mapping function</param>
    /// <typeparam name="TResult">Type of return value</typeparam>
    /// <returns>Lifted return value if Ok; otherwise, preserve error.</returns>
    public Result<TError, TResult> Map<TResult>(Func<T, TResult> mapping)

        => _error is null
            ? new Result<TError, TResult>(mapping(_value))
            : new Result<TError, TResult>(_error);

    /// <summary>
    /// Map error result by given <paramref name="errorMapping"/>.
    /// </summary>
    /// <param name="errorMapping">Mapping function</param>
    /// <typeparam name="TErrorResult">Type of return error</typeparam>
    /// <returns>Lifted return error if Error; otherwise, preserve value.</returns>
    public Result<TErrorResult, T> MapError<TErrorResult>(Func<TError, TErrorResult> errorMapping)
        where TErrorResult : class

        => _error is null
            ? new Result<TErrorResult,T>(_value)
            : new Result<TErrorResult, T>(errorMapping(_error));

    /// <summary>
    /// Bind ok result by given <paramref name="binder"/>.
    /// </summary>
    /// <param name="binder">Binding function</param>
    /// <typeparam name="TResult">Type of return value</typeparam>
    /// <returns>Result of binding function if ok; otherwise, preserved error.</returns>
    public Result<TError, TResult> Bind<TResult>(Func<T, Result<TError, TResult>> binder)

        => _error is null
            ? binder(_value)
            : new Result<TError,TResult>(_error);

    /// <summary>
    /// Implicit cast from <typeparamref name="TError"/> to <see cref="Result{TError,T}"/>.
    /// </summary>
    /// <param name="error">Error</param>
    /// <returns>Error result</returns>
    public static implicit operator Result<TError, T>(TError error) => new(error);

    /// <summary>
    /// Implicit cast from <typeparamref name="T"/> to <see cref="Result{TError,T}"/>.
    /// </summary>
    /// <param name="value">Value</param>
    /// <returns>Ok result</returns>
    public static implicit operator Result<TError, T>(T value) => new(value);

    /// <summary>
    /// Non-boxing equality comparison. Delegates to <see cref="EqualityComparer{T}.Equals(T, T)"/> or
    /// <see cref="EqualityComparer{TError}.Equals(TError, TError)"/> depending on state.
    /// </summary>
    /// <param name="other"><see cref="Result{TError,T}"/> comparand</param>
    /// <returns>
    /// <see langword="true"/> if results have the same state and values or errors; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Result<TError, T> other)

        => _error is null
            ? other._error is null && ValueEqualityComparer.Equals(_value, other._value)
            : ErrorEqualityComparer.Equals(_error, other._error);

    /// <remarks>
    /// Ok is represented as "Ok &lt;value&gt;". Error is represented as "Error &lt;error&gt;".<br/>
    /// <paramref name="format"/> and <paramref name="formatProvider"/> will be passed to the same method on the value
    /// or error if it is <see cref="IFormattable"/>; otherwise <see cref="object.ToString()"/> is used to produce the
    /// value or error representation.
    /// </remarks>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (_error is null)
        {
            var valueRepresentation = _value is IFormattable formattable
                ? formattable.ToString(format, formatProvider)
                : _value?.ToString();

            return string.IsNullOrEmpty(valueRepresentation)
                ? "Ok"
                : $"Ok\u00a0{valueRepresentation}";
        }
        else
        {
            var errorRepresentation = _error is IFormattable formattable
                ? formattable.ToString(format, formatProvider)
                : _error.ToString();

            return string.IsNullOrEmpty(errorRepresentation)
                ? "Error"
                : $"Error\u00a0{errorRepresentation}";
        }
    }

    /// <summary>
    /// Produces the string representation of the result
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="ToString(string?,System.IFormatProvider?)"/> with null parameters.
    /// </remarks>
    public override string ToString() => ToString(null, null);

    /// <summary>
    /// Produces the string representation of the result using <paramref name="format"/> if supported by
    /// <typeparamref name="T"/> or <typeparamref name="TError"/> respectively.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="ToString(string?,System.IFormatProvider?)"/> with <paramref name="format"/> and null as
    /// parameters.
    /// </remarks>
    /// <param name="format">
    /// The format to use or null. See <see cref="IFormattable.ToString(string?, IFormatProvider?)"/>
    /// </param>
    public string ToString(string? format) => ToString(format, null);

    /// <summary>
    /// Possibly boxing equality comparison. Delegates to <see cref="object.Equals(object?)"/> in
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)

        => (_error, _value, obj) switch
        {
            (_, _, Result<TError, T> other) => Equals(other),   // Delegate to non-boxing comparison
            (null, null, _) => obj is null, // Ok null equals null
            (null, _, _) => _value.Equals(obj), // Delegate to T.Equals(object?)
            _ => false  // Error result only equals Error result of same type and value
        };

    /// <summary>
    /// Equality operator. Delegates to <see cref="Equals(Result{TError,T})"/>.
    /// </summary>
    public static bool operator ==(Result<TError, T> left, Result<TError, T> right) => left.Equals(right);

    /// <summary>
    /// Inequality operator. Delegates to <see cref="Equals(Result{TError,T})"/> and negates.
    /// </summary>
    public static bool operator !=(Result<TError, T> left, Result<TError, T> right) => !left.Equals(right);
}

/// <summary>
/// Factory and extension methods for <see cref="Result{TError,T}"/>
/// </summary>
public static class Result
{
    /// <summary>
    /// Factory method for Ok result
    /// </summary>
    /// <param name="value">Value of result</param>
    /// <typeparam name="TError">Type of error</typeparam>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>Ok value</returns>
    public static Result<TError, T> Ok<TError, T>(T value) where TError : class => value;

    /// <summary>
    /// Factory method for Error result
    /// </summary>
    /// <param name="error">Error of result</param>
    /// <typeparam name="TError">Type of error</typeparam>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>Error error</returns>
    public static Result<TError, T> Error<TError, T>(TError error) where TError : class => error;

    /// <summary>
    /// Projects the value of <see cref="Result{TError,T}"/> into a new form.
    /// </summary>
    /// <param name="source">Result to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to ok value.</param>
    /// <typeparam name="TError">The type of the error of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TSource">The type of the value of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <returns>
    /// A <see cref="Result{TError,TResult}"/> whose value is the result of invoking the transform function on ok value
    /// of <paramref name="source"/> or with preserved error.
    /// </returns>
    /// <remarks>
    /// Provided for support of LINQ query syntax. The expression
    /// <code>
    /// from s in source
    /// select selector(s)
    /// </code>
    /// is syntax sugar for
    /// <code>
    /// source.Select(selector)
    /// </code>
    /// This is exactly the same functionality as
    /// <see cref="Result{TError,T}.Map{TResult}(Func{T,TResult})"/> and so
    /// <see cref="Result.Select{TError,TSource,TResult}(Result{TError,TSource}, Func{TSource, TResult})"/> delegates
    /// directly to it.
    /// </remarks>
    public static Result<TError, TResult> Select<TError, TSource, TResult>(
        this Result<TError, TSource> source,
        Func<TSource, TResult> selector)
        where TError : class

        => source.Map(selector);

    /// <summary>
    /// Projects ok value of <see cref="Result{TError,T}"/> to another <see cref="Result{TError,TNext}"/>, and invokes a
    /// result selector function on the pair to produce the result.
    /// </summary>
    /// <param name="source">Result to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to the value of <paramref name="source"/>.</param>
    /// <param name="resultSelector">
    /// A projection to apply to the value of <paramref name="source"/> and the value of the return value of
    /// <paramref name="selector"/>.
    /// </param>
    /// <typeparam name="TError">The type of the error of <paramref name="source"/>.</typeparam>
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
    /// <returns>
    /// A <see cref="Result{TError,TResult}"/> whose value is the result of invoking the transform function
    /// <paramref name="selector"/> on ok value of <paramref name="source"/> and then mapping ok result value and the
    /// source value to a result value.
    /// </returns>
    /// <remarks>
    /// Provided for support of LINQ query syntax. The expression
    /// <code>
    /// from s in source
    /// from n in selector(s)
    /// select resultSelector(s, n)
    /// </code>
    /// is syntax sugar for
    /// <code>
    /// source.SelectMany(selector, resultSelector)
    /// </code>
    /// The difference between <c>Bind</c> and <c>SelectMany</c> is that <c>SelectMany</c> takes a binary projection
    /// function, <paramref name="resultSelector"/>, as a parameter and as such can chain calls to <c>SelectMany</c>
    /// instead of encapsulating calls to <c>Bind</c> in nested closures.
    /// </remarks>
    public static Result<TError, TResult> SelectMany<TError, TSource, TNext, TResult>(
        this Result<TError, TSource> source,
        Func<TSource, Result<TError, TNext>> selector,
        Func<TSource, TNext, TResult> resultSelector)
        where TError : class

        => source.Bind(s => selector(s).Map(n => resultSelector(s, n)));
}

/// <summary>
/// Factory methods for <see cref="Result{TError,T}"/> closed over <typeparamref name="TError"/>.
/// </summary>
/// <typeparam name="TError">Type of error</typeparam>
public static class Result<TError> where TError : class
{
    /// <summary>
    /// Factory method for Ok result.
    /// </summary>
    /// <param name="value">Value of result</param>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>Ok value</returns>
    public static Result<TError, T> Ok<T>(T value) => value;

    /// <summary>
    /// Factory method for Error result.
    /// </summary>
    /// <param name="error">Error of result</param>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>Error error</returns>
    /// <remarks>
    /// This factory method may seem redundant with <see cref="Result.Error{TError,T}(TError)"/>. It is, until you
    /// declare a type alias for <see cref="Result{TError}"/> for use in your code scoped around a particular type of
    /// error.
    /// </remarks>
    public static Result<TError, T> Error<T>(TError error) => error;
}
