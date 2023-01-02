using Void = System.ValueTuple;

namespace Luger.Functional;

/* NOTE: Overloads of the following extensions for ValueTask look simple enough to implement but so far I've decided
 * against it as I believe these extensions will mostly be used in quiet balmy high level code and therefore not safely
 * provide much of performance improvement. I have too little experience with advanced details of Task vs. ValueTask so
 * I'll leave it to some braver soul out there to make the call.
 * 
 * Stephen Toub wrote a good article about "Understanding the Whys, Whats, and Whens of ValueTask" in 2018.
 * https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/
 */

/// <summary>
/// Functional extensions to <see cref="Task{TResult}"/>
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Unary function application within applicative functor <see cref="Task{TResult}"/>.
    /// </summary>
    /// <typeparam name="TArg">Type of parameter</typeparam>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <param name="funcTask">Function task</param>
    /// <param name="argTask">Argument task</param>
    /// <param name="applyOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the application back to the original context captures; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <returns>Task of application result</returns>
    public static async Task<TResult> Apply<TArg, TResult>(
        this Task<Func<TArg, TResult>> funcTask,
        Task<TArg> argTask,
        bool applyOnCapturedContext = false)
    {
        var (func, arg) = await (funcTask, argTask).ConfigureAwait(applyOnCapturedContext);

        return func(arg);
    }

    /// <summary>
    /// Partial binary function application within applicative functor <see cref="Task{TResult}"/>.
    /// </summary>
    /// <typeparam name="TArg1">Type of first parameter</typeparam>
    /// <typeparam name="TArg2">Type of second parameter</typeparam>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <param name="funcTask">Function task</param>
    /// <param name="arg1Task">First argument task</param>
    /// <param name="applyOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the application back to the original context captures; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <returns>Task of partially applied binary function</returns>
    public static async Task<Func<TArg2, TResult>> Apply<TArg1, TArg2, TResult>(
        this Task<Func<TArg1, TArg2, TResult>> funcTask,
        Task<TArg1> arg1Task,
        bool applyOnCapturedContext = false)
    {
        var (func, arg1) = await (funcTask, arg1Task).ConfigureAwait(applyOnCapturedContext);

        return arg2 => func(arg1, arg2);
    }

    /// <summary>
    /// Partial ternary function application within applicative functor <see cref="Task{TResult}"/>.
    /// </summary>
    /// <typeparam name="TArg1">Type of first parameter</typeparam>
    /// <typeparam name="TArg2">Type of second parameter</typeparam>
    /// <typeparam name="TArg3">Type of third parameter</typeparam>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <param name="funcTask">Function task</param>
    /// <param name="arg1Task">First argument task</param>
    /// <param name="applyOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the application back to the original context captures; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <returns>Task of partially applied ternary function</returns>
    public static async Task<Func<TArg2, TArg3, TResult>> Apply<TArg1, TArg2, TArg3, TResult>(
        this Task<Func<TArg1, TArg2, TArg3, TResult>> funcTask,
        Task<TArg1> arg1Task,
        bool applyOnCapturedContext = false)
    {
        var (func, arg1) = await (funcTask, arg1Task).ConfigureAwait(applyOnCapturedContext);

        return (arg2, arg3) => func(arg1, arg2, arg3);
    }

    /// <summary>
    /// Helper to continue a <see cref="Task"/> with a <see cref="Task{TResult}"/> of <see cref="Void"/> to make it
    /// composable.
    /// </summary>
    /// <param name="task">Source task</param>
    /// <returns>Continuation task of type <see cref="Task{TResult}"/> of <see cref="Void"/></returns>
    public static async Task<Void> AsVoidTask(this Task task)
    {
        await task.ConfigureAwait(false);

        return default;
    }

    /// <summary>
    /// Bind within monad <see cref="Task{TResult}"/>.
    /// </summary>
    /// <typeparam name="TSource">Type of source result</typeparam>
    /// <typeparam name="TResult">Type of bound result</typeparam>
    /// <param name="source">Source task</param>
    /// <param name="func">Bound function</param>
    /// <param name="bindOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the execution of bound function back to the original context
    /// captured; otherwise, <see langword="false"/>.
    /// </param>
    /// <returns>Task of bound function result</returns>
    public static async Task<TResult> Bind<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, Task<TResult>> func,
        bool bindOnCapturedContext = false)
    {
        var t = await source.ConfigureAwait(bindOnCapturedContext);

        return await func(t).ConfigureAwait(false);
    }

    /// <summary>
    /// <see langword="true"/> to attempt to marshal the execution of LINQ query selectors back to the original context
    /// captured; otherwise, <see langword="false"/>.
    /// </summary>
    public static bool SelectOnCapturedContext { get; set; } = false;

    /// <summary>
    /// Projects the result of a task into a new task and projects both results into a new task.
    /// </summary>
    /// <typeparam name="TSource">The type of the result of <paramref name="source"/></typeparam>
    /// <typeparam name="TNext">The type of the intermediate result produced by <paramref name="selector"/></typeparam>
    /// <typeparam name="TResult">The type of the result returned by <paramref name="projection"/></typeparam>
    /// <param name="source">A task to invoke a projection on.</param>
    /// <param name="selector">A transform function to apply to the result of <paramref name="source"/></param>
    /// <param name="projection">
    /// A transform function to apply to <paramref name="source"/> result and <paramref name="selector"/> result
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> with the result of invoking the transform function on the result of
    /// <paramref name="source"/> and then mapping both the result of <paramref name="source"/> and
    /// <paramref name="selector"/> through <paramref name="projection"/>.
    /// </returns>
    /// <remarks>
    /// Provided for support of LINQ query syntax binding in the monad of <see cref="Task{TResult}"/>. The expression
    /// <code>
    /// from s in source
    /// from n in selector(s)
    /// select projection(s, n)
    /// </code>
    /// is precompiled into
    /// <code>
    /// source.SelectMany(selector, projection)
    /// </code>
    /// The difference between <c>Bind</c> and <c>SelectMany</c> is that <c>SelectMany</c> takes a binary projection
    /// function, <paramref name="projection"/>, as a parameter and as such can chain calls to <c>SelectMany</c>
    /// instead of encapsulating calls to <c>Bind</c> in nested closures.<br/>
    /// <c>SelectMany</c> can be implemented in terms of <c>Bind</c> and <c>Map</c> but type-specific implementations
    /// are probably more efficient.
    /// </remarks>
    public static async Task<TResult> SelectMany<TSource, TNext, TResult>(
        this Task<TSource> source,
        Func<TSource, Task<TNext>> selector,
        Func<TSource, TNext, TResult> projection)
    {
        var s = await source.ConfigureAwait(SelectOnCapturedContext);
        var n = await selector(s).ConfigureAwait(SelectOnCapturedContext);

        return projection(s, n);
    }

    /// <summary>
    /// Map <paramref name="func"/> within functor <see cref="Task{TResult}"/>.
    /// </summary>
    /// <typeparam name="TSource">Type of source result</typeparam>
    /// <typeparam name="TResult">Type of map result</typeparam>
    /// <param name="source">Source task</param>
    /// <param name="func">Map function</param>
    /// <param name="mapOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the mapping back to the original context captured; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <returns>Task of mapped result</returns>
    public static async Task<TResult> Map<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult> func,
        bool mapOnCapturedContext = false)
    {
        var s = await source.ConfigureAwait(mapOnCapturedContext);

        return func(s);
    }

    /// <summary>
    /// Projects the result of a task into a new form.
    /// </summary>
    /// <typeparam name="TSource">The type of the result of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="source">A task to invoke a transform function on</param>
    /// <param name="selector">A transform function to apply to the result of <paramref name="source"/></param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> with the result of invoking the transform function on the result of
    /// <paramref name="source"/>.
    /// </returns>
    /// <remarks>
    /// Provided for support of LINQ query syntax mapping in the functor of <see cref="Task{TResult}"/>. The expression
    /// <code>
    /// from s in source
    /// select selector(s)
    /// </code>
    /// is precompiled into
    /// <code>
    /// source.Select(selector)
    /// </code>
    /// This is exactly the same functionality as <see cref="Map{TSource, TResult}"/> (without context capture) and so
    /// <see cref="Select"/> delegates directly to it.
    /// </remarks>
    public static Task<TResult> Select<TSource, TResult>(this Task<TSource> source, Func<TSource, TResult> selector)

        => source.Map(selector, SelectOnCapturedContext);

    /// <summary>
    /// Add exception and, optionally, cancellation handling continuations to <see cref="Task{TResult}"/>
    /// </summary>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <typeparam name="TException">Type of exception to handle</typeparam>
    /// <param name="result">Task to handle exceptions and, optionally, cancellation for</param>
    /// <param name="exceptionHandler">Asynchronous exception handler</param>
    /// <param name="cancellationHandler">Asynchronous cancellation handler</param>
    /// <param name="handleOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the execution of handler to the original context captured;
    /// otherwise, <see langword="false"/>
    /// </param>
    /// <returns>Task of result or appropriate handler</returns>
    /// <remarks>
    /// If <paramref name="result"/> is successful, its result is returned.<br/>
    /// If <paramref name="result"/> is faulted and the exception thrown by the <see langword="await"/> is assignable to
    /// <typeparamref name="TException"/>, <paramref name="exceptionHandler"/> is invoked as continuation with the
    /// exception as argument.<br/>
    /// If <paramref name="result"/> is faulted and the exception thrown by the <see langword="await"/> is not
    /// assignable to <typeparamref name="TException"/>, the exception is not caught.
    /// If <paramref name="result"/> is canceled and <paramref name="cancellationHandler"/> is not
    /// <see langword="null"/>, <paramref name="cancellationHandler"/> is invoked as continuation with the
    /// <see cref="OperationCanceledException"/> thrown by the <see langword="await"/> as argument.<br/>
    /// If <paramref name="result"/> is canceled and <paramref name="cancellationHandler"/> is <see langword="null"/>,
    /// the <see cref="OperationCanceledException"/> is not caught.<br/>
    /// As an edge case, if <paramref name="result"/> is canceled, <paramref name="cancellationHandler"/> is
    /// <see langword="null"/> and <typeparamref name="TException"/> is assignable from
    /// <see cref="OperationCanceledException"/>, <paramref name="exceptionHandler"/> is invoked as continuation with
    /// the <see cref="OperationCanceledException"/> thrown by the <see langword="await"/> as argument.<br/>
    /// </remarks>
    public static async Task<TResult> OrElse<TResult, TException>(
        this Task<TResult> result,
        Func<TException, Task<TResult>> exceptionHandler,
        Func<OperationCanceledException, Task<TResult>>? cancellationHandler = null,
        bool handleOnCapturedContext = false)
        where TException : Exception
    {
        try
        {
            return await result.ConfigureAwait(handleOnCapturedContext);
        }
        catch (OperationCanceledException operationCanceledException) when (cancellationHandler is not null)
        {
            return await cancellationHandler(operationCanceledException).ConfigureAwait(false);
        }
        catch (TException exception)
        {
            return await exceptionHandler(exception).ConfigureAwait(false);
        }
    }
}
