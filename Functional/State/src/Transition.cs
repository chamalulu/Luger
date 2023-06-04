namespace Luger.Functional;

/// <summary>
/// Represents a stateful transition (computation or operation). Typically called the state monad.
/// </summary>
/// <typeparam name="TState">Type of state</typeparam>
/// <typeparam name="TValue">Type of value</typeparam>
/// <param name="state">Input state</param>
/// <returns>A tuple of the result of the computation and the output state.</returns>
public delegate (TValue Value, TState State) Transition<TState, TValue>(TState state);

/// <summary>
/// Factories and extensions for <see cref="Transition{TState, TValue}"/>
/// </summary>
public static class Transition
{
    /// <summary>
    /// Run a transition with given state (discarding end state)
    /// </summary>
    /// <typeparam name="TState">Type of state</typeparam>
    /// <typeparam name="TValue">Type of value</typeparam>
    /// <param name="transition">The transition to run</param>
    /// <param name="state">The state to run over</param>
    /// <returns>Return value of transition run over state</returns>
    public static TValue Run<TState, TValue>(this Transition<TState, TValue> transition, TState state)

        => transition(state).Value;

    /// <summary>
    /// Application of <paramref name="func"/> to <paramref name="source"/> in functor of
    /// <see cref="Transition{TState, TValue}"/>.
    /// </summary>
    /// <typeparam name="TState">Type of state</typeparam>
    /// <typeparam name="TSource">Type of source value</typeparam>
    /// <typeparam name="TResult">Type of result value</typeparam>
    /// <param name="source">The transition to map over</param>
    /// <param name="func">Mapping function</param>
    /// <remarks>
    /// This is the equivalent of the infix operator <see langword="&lt;$&gt;"/> of Functor in Haskell.
    /// </remarks>
    /// <returns>Transition of state with result value mapped to type <typeparamref name="TResult"/></returns>
    public static Transition<TState, TResult> Map<TState, TSource, TResult>(
        this Transition<TState, TSource> source,
        Func<TSource, TResult> func)

        => state0 =>
        {
            var (s, state1) = source(state0);
            return (func(s), state1);
        };

    /// <summary>
    /// Sequential composition of <paramref name="func"/> to <paramref name="source"/> in monad of
    /// <see cref="Transition{TState, TValue}"/>.
    /// </summary>
    /// <typeparam name="TState">Type of state</typeparam>
    /// <typeparam name="TSource">Type of source value</typeparam>
    /// <typeparam name="TResult">Type of result value</typeparam>
    /// <param name="source">The transition to bind over</param>
    /// <param name="func">Function to bind</param>
    /// <remarks>
    /// This is the equivalent of the infix operator <see langword="&gt;&gt;="/> of Monad in Haskell.
    /// </remarks>
    /// <returns>
    /// Transition of state performing the computation from <paramref name="func"/> bound to <paramref name="source"/>.
    /// </returns>
    public static Transition<TState, TResult> Bind<TState, TSource, TResult>(
        this Transition<TState, TSource> source,
        Func<TSource, Transition<TState, TResult>> func)

        => state0 =>
        {
            var (s, state1) = source(state0);
            return func(s)(state1);
        };

    /// <summary>
    /// Projects the result of <see cref="Transition{TState, TValue}"/> into a new form.
    /// </summary>
    /// <typeparam name="TState">The type of the state</typeparam>
    /// <typeparam name="TSource">The type of the result of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">The type of the result of <paramref name="selector"/>.</typeparam>
    /// <param name="source">Transition with result to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to the result of a transition.</param>
    /// <returns>
    /// <see cref="Transition{TState, TValue}"/> whose result is the result of invoking the transform function on result
    /// of <paramref name="source"/>.
    /// </returns>
    /// <remark>
    /// Provided for support of LINQ query syntax mapping in the functor of <see cref="Transition{TState, TValue}"/>.
    /// The expression
    /// <code>
    /// from s in source
    /// select selector(s)
    /// </code>
    /// is precompiled into
    /// <code>
    /// source.Select(selector)
    /// </code>
    /// This is exactly the same functionality as
    /// <see cref="Transition.Map{TState, TSource, TResult}(Transition{TState, TSource}, Func{TSource, TResult})"/>
    /// and so
    /// <see cref="Transition.Select{TState, TSource, TResult}(Transition{TState, TSource}, Func{TSource, TResult})"/>
    /// delegates directly to it.
    /// </remark>
    public static Transition<TState, TResult> Select<TState, TSource, TResult>(
        this Transition<TState, TSource> source,
        Func<TSource, TResult> selector)

        => source.Map(selector);

    /// <summary>
    /// Projects some value of <see cref="Transition{TState, TValue}"/> to another
    /// <see cref="Transition{TState, TValue}"/>, and invokes a result selector function on the pair to produce the
    /// result.
    /// </summary>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TSource">The type of the result of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TNext">
    /// Type of the intermediate result of the <see cref="Transition{TState, TValue}"/> returned by the
    /// <paramref name="selector"/>.
    /// </typeparam>
    /// <typeparam name="TResult">Type of result of the <paramref name="resultSelector"/>.</typeparam>
    /// <param name="source">The transition to bind over</param>
    /// <param name="selector">The function to be bound</param>
    /// <param name="resultSelector">Projection of source value and intermediary value to result.</param>
    /// <returns>
    /// A <see cref="Transition{TState, TValue}"/> whose result is the result of invoking the transform function
    /// <paramref name="selector"/> on the result of <paramref name="source"/> and then mapping the result value and
    /// the source result value to a result value.
    /// </returns>
    /// <remark>
    /// Provided for support of LINQ query syntax binding in the monad of <see cref="Transition{TState, TValue}"/>. The
    /// expression
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
    /// </remark>
    public static Transition<TState, TResult> SelectMany<TState, TSource, TNext, TResult>(
        this Transition<TState, TSource> source,
        Func<TSource, Transition<TState, TNext>> selector,
        Func<TSource, TNext, TResult> resultSelector)

        => state0 =>
        {
            var (s, state1) = source(state0);
            var (n, state2) = selector(s)(state1);
            return (resultSelector(s, n), state2);
        };

    /// <summary>
    /// Traverse sequence of elements with a state transition function.
    /// </summary>
    /// <typeparam name="TState">Type of transition state.</typeparam>
    /// <typeparam name="TSource">Type of source element.</typeparam>
    /// <typeparam name="TResult">Type of transition result.</typeparam>
    /// <param name="source">Sequence to traverse over.</param>
    /// <param name="func">Function mapping element to transition.</param>
    /// <returns>Transition yielding sequence of results.</returns>
    public static Transition<TState, IEnumerable<TResult>> Traverse<TState, TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, Transition<TState, TResult>> func)

        => state =>
        {
            var result = source.TryGetNonEnumeratedCount(out var count)
                ? new List<TResult>(count)
                : new List<TResult>();

            foreach (var s in source)
            {
                var (r, nextState) = func(s)(state);
                result.Add(r);
                state = nextState;
            }

            return (result, state);
        };
}

/// <summary>
/// Static helper extension to take advantage of type argument inference.
/// </summary>
/// <typeparam name="TState">Type of state.</typeparam>
public static class Transition<TState>
{
    /// <summary>
    /// Lift a value into a <see cref="Transition{TState, TValue}"/>. The computation does not alter state.
    /// </summary>
    /// <typeparam name="TValue">Type of value.</typeparam>
    /// <param name="value"></param>
    /// <returns>A <see cref="Transition{TState, TValue}"/> which yields the value and the given state.</returns>
    public static Transition<TState, TValue> Return<TValue>(TValue value) => state => (value, state);
}
