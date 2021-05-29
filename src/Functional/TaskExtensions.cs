using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Void = System.ValueTuple;

namespace Luger.Functional
{
    /// <summary>
    /// Encapsulate options for the <see cref="TaskExtensions.ExponentialBackoff"/> extension method.
    /// </summary>
    // TODO: Refactor as record.
    public class ExponentialBackoffOptions
    {
        /// <summary>
        /// Number of retries to attempt. Default to 8.
        /// </summary>
        public uint Retries { get; set; } = 8;

        private TimeSpan _baseDelay = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Mean delay to wait before first retry. Default to 100ms.
        /// </summary>
        public TimeSpan BaseDelay
        {
            get => _baseDelay;
            set => _baseDelay = value >= TimeSpan.Zero
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value));
        }

        private Random? _rng;

        /// <summary>
        /// Random number generator to use for randomizing delay. Default to parameterless construction of <see cref="Random"/>.
        /// </summary>
        public Random RNG
        {
            get => _rng ??= new Random();
            set => _rng = value;
        }

        /// <summary>
        /// Cancellation token to monitor for cancelling backoff. Not automatically monitored by f() task. Default to None.
        /// </summary>
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        /// <summary>
        /// Deconstruct <see cref="ExponentialBackoffOptions"/> into tuple of <paramref name="retries"/>,
        /// <paramref name="baseDelay"/>, <paramref name="rng"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        public void Deconstruct(
            out uint retries,
            out TimeSpan baseDelay,
            out Random rng,
            out CancellationToken cancellationToken)
        {
            retries = Retries;
            baseDelay = BaseDelay;
            rng = RNG;
            cancellationToken = CancellationToken;
        }
    }

    // TODO: Add overloads for ValueTask

    /// <summary>
    /// Functional extensions to Task related types.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Map <paramref name="f"/> within functor <see cref="Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="T">Type of source result</typeparam>
        /// <typeparam name="TR">Type of map result</typeparam>
        /// <param name="task">Source task</param>
        /// <param name="f">Map function</param>
        /// <param name="mapOnCapturedContext">
        /// <c>true</c> to attempt to marshal the mapping back to the original context captured; otherwise, <c>false</c>.
        /// </param>
        /// <remarks>
        /// Since C# does not support partial application but do support extension methods this functionally "backwards" parameter
        /// order is practical.
        /// </remarks>
        /// <returns>Task of mapped result</returns>
        public static async Task<TR> Map<T, TR>(this Task<T> task, Func<T, TR> f, bool mapOnCapturedContext = false)
        {
            var t = await task.ConfigureAwait(mapOnCapturedContext);

            return f(t);
        }

        /// <summary>
        /// Creates a task that will complete when both of the given <see cref="Task{TResult}"/> objects have completed.
        /// </summary>
        /// <typeparam name="T1">Type of <paramref name="task1"/> result</typeparam>
        /// <typeparam name="T2">Type of <paramref name="task2"/> result</typeparam>
        /// <param name="task1">First task</param>
        /// <param name="task2">Second task</param>
        /// <returns>Task of a tuple of the results</returns>
        public static async Task<(T1, T2)> WhenAll<T1, T2>(Task<T1> task1, Task<T2> task2)
        {
            task1 = task1 ?? throw new ArgumentNullException(nameof(task1));

            task2 = task2 ?? throw new ArgumentNullException(nameof(task2));

            await Task.WhenAll(task1, task2).ConfigureAwait(false);

            return (task1.Result, task2.Result);
        }

        /// <summary>
        /// Unary function application within applicative functor <see cref="Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="T">Type of parameter</typeparam>
        /// <typeparam name="TR">Type of result</typeparam>
        /// <param name="tf">Function task</param>
        /// <param name="tt">Parameter task</param>
        /// <param name="applyOnCapturedContext">
        /// <c>true</c> to attempt to marshal the application back to the original context captures; otherwise, <c>false</c>.
        /// </param>
        /// <returns>Task of application result</returns>
        public static async Task<TR> Apply<T, TR>(this Task<Func<T, TR>> tf, Task<T> tt, bool applyOnCapturedContext = false)
        {
            var (f, t) = await WhenAll(tf, tt).ConfigureAwait(applyOnCapturedContext);

            return f(t);
        }

        /// <summary>
        /// Partial binary function application within applicative functor <see cref="Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="T1">Type of first parameter</typeparam>
        /// <typeparam name="T2">Type of second parameter</typeparam>
        /// <typeparam name="TR">Type of result</typeparam>
        /// <param name="tf">Function task</param>
        /// <param name="tt">First parameter task</param>
        /// <param name="applyOnCapturedContext">
        /// <c>true</c> to attempt to marshal the application back to the original context captures; otherwise, <c>false</c>.
        /// </param>
        /// <returns>Task of partially applied binary function</returns>
        public static Task<Func<T2, TR>> Apply<T1, T2, TR>(
            this Task<Func<T1, T2, TR>> tf,
            Task<T1> tt,
            bool applyOnCapturedContext = false)

            => tf.Map(FuncExt.Curry).Apply(tt, applyOnCapturedContext);

        /// <summary>
        /// Partial ternary function application within applicative functor <see cref="Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="T1">Type of first parameter</typeparam>
        /// <typeparam name="T2">Type of second parameter</typeparam>
        /// <typeparam name="T3">Type of third parameter</typeparam>
        /// <typeparam name="TR">Type of result</typeparam>
        /// <param name="tf">Function task</param>
        /// <param name="tt">First parameter task</param>
        /// <param name="applyOnCapturedContext">
        /// <c>true</c> to attempt to marshal the application back to the original context captures; otherwise, <c>false</c>.
        /// </param>
        /// <returns>Task of partially applied ternary function</returns>
        public static Task<Func<T2, T3, TR>> Apply<T1, T2, T3, TR>(
            this Task<Func<T1, T2, T3, TR>> tf,
            Task<T1> tt,
            bool applyOnCapturedContext = false)

            => tf.Map(FuncExt.CurryFirst).Apply(tt, applyOnCapturedContext);

        /// <summary>
        /// Bind within monad <see cref="Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="T">Type of source result</typeparam>
        /// <typeparam name="TR">Type of bound result</typeparam>
        /// <param name="task">Source task</param>
        /// <param name="f">Bound function</param>
        /// <param name="bindOnCapturedContext">
        /// <c>true</c> to attempt to marshal the execution of bound function back to the original context captured; otherwise,
        /// <c>false</c>.
        /// </param>
        /// <returns>Task of bound function result</returns>
        public static async Task<TR> Bind<T, TR>(this Task<T> task, Func<T, Task<TR>> f, bool bindOnCapturedContext = false)
        {
            var t = await task.ConfigureAwait(bindOnCapturedContext);

            return await f(t).ConfigureAwait(false);
        }

        #region LINQ query syntax implementation

        /// <summary>
        /// Projects the result of a task into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the result of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">A task to invoke a transform function on</param>
        /// <param name="selector">A transform function to apply to the result of <paramref name="source"/></param>
        /// <returns>A <see cref="Task{TResult}"/> with the result of invoking the transform function on the result of <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is <c>null</c>.</exception>
        /// <remarks>
        /// If selector returns a value that is itself a task, it is up to the consumer to unwrap the subtask manually.
        /// In such a situation, it might be better for your query to return a single task.
        /// To achieve this, use the SelectMany method instead of Select.
        /// Although SelectMany works similarly to Select, it differs in that the transform function returns a task that is then awaited by SelectMany before its result is returned.
        /// In query expression syntax, a select (Visual C#) or Select (Visual Basic) clause translates to an invocation of Select.
        /// </remarks>
        /// <example>
        /// The following code example demonstrates how to use <see cref="Select{TSource, TResult}(Task{TSource}, Func{TSource, TResult})"/> to project over an asynchronous result.
        /// <code>
        /// Task&lt;int&gt; square = Task.FromResult(10).Select(x => x * x);
        ///
        /// Console.WriteLine(await square);    // Output 100
        /// </code>
        /// <code>
        /// Task&lt;int&gt; square = from x in Task.FromResult(10) select x * x;
        ///
        /// Console.WriteLine(await square);    // Output 100
        /// </code>
        /// </example>
        public static Task<TResult> Select<TSource, TResult>(this Task<TSource> source, Func<TSource, TResult> selector) =>
            source.Map(selector, false);

        /// <summary>
        /// Projects the result of a task into a new form, projects both results into a new task.
        /// </summary>
        /// <typeparam name="TSource">The type of the result of <paramref name="source"/></typeparam>
        /// <typeparam name="TNext">The type of the intermediate result produced by <paramref name="selector"/></typeparam>
        /// <typeparam name="TResult">The type of the result returned by <paramref name="projection"/></typeparam>
        /// <param name="source">A task to invoke a projection on.</param>
        /// <param name="selector">A transform function to apply to the result of <paramref name="source"/></param>
        /// <param name="projection">A transform function to apply to <paramref name="source"/> result and <paramref name="selector"/> result</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> with the result of invoking the transform function on the result of <paramref name="source"/> and
        /// then mapping both the result of <paramref name="source"/> and <paramref name="selector"/> through <paramref name="projection"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="selector"/> or <paramref name="projection"/> is <c>null</c>.</exception>
        /// <example>
        /// The following code example demonstrates how to use <see cref="SelectMany{TSource, TNext, TResult}(Task{TSource}, Func{TSource, Task{TNext}}, Func{TSource, TNext, TResult})"/>
        /// to monadically bind the results of multiple <see cref="Task{TResult}"/>s.
        /// T.B.D.
        /// </example>
        /// <remarks>
        /// Bla bla bla.
        /// <para>
        /// In query expression syntax, each <c>from</c> clause (Visual C#) or <c>From</c> clause (Visual Basic) after the initial one translates to an
        /// invocation of <see cref="SelectMany{TSource, TNext, TResult}(Task{TSource}, Func{TSource, Task{TNext}}, Func{TSource, TNext, TResult})"/>.
        /// </para>
        /// </remarks>
        public static async Task<TResult> SelectMany<TSource, TNext, TResult>(this Task<TSource> source, Func<TSource, Task<TNext>> selector, Func<TSource, TNext, TResult> projection)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));

            selector = selector ?? throw new ArgumentNullException(nameof(selector));

            projection = projection ?? throw new ArgumentNullException(nameof(projection));

            var s = await source.ConfigureAwait(false);
            var n = await selector(s).ConfigureAwait(false);

            return projection(s, n);
        }

        #endregion

        /// <summary>
        /// Add general exception handling continuation to <see cref="Task{TResult}"/>
        /// </summary>
        /// <typeparam name="T">Type of task result</typeparam>
        /// <param name="task">Source task</param>
        /// <param name="fallback">Function mapping faulted task to fallback task</param>
        /// <param name="fallbackOnCapturedContext"><c>true</c> to attempt to marshal the execution of fallback function to the original context captured; otherwise, <c>false</c>.</param>
        /// <returns>Task of source result or fallback</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exception handling.")]
        public static async Task<T> OrElse<T>(this Task<T> task, Func<Task<T>, Task<T>> fallback, bool fallbackOnCapturedContext = false)
        {
            task = task ?? throw new ArgumentNullException(nameof(task));

            fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));

            try
            {
                return await task.ConfigureAwait(fallbackOnCapturedContext);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return await fallback(task).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Helper to wrap a <see cref="Task"/> in a <see cref="Task{TResult}"/> of <see cref="Void"/> to make it composable.
        /// </summary>
        /// <param name="task">Source task</param>
        /// <returns>Source task wrapped in <see cref="Task{TResult}"/> of <see cref="Void"/></returns>
        public static async Task<Void> AsVoidTask(this Task task)
        {
            task = task ?? throw new ArgumentNullException(nameof(task));

            await task.ConfigureAwait(false);

            return default;
        }

        /// <summary>
        /// Retries an asynchronous function with exponential backoff if it faults.
        /// </summary>
        /// <typeparam name="T">Type of result</typeparam>
        /// <param name="f">Asynchronous function to retry</param>
        /// <param name="options">Configuration of exponential backoff</param>
        /// <returns>Asynchronous function result</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exception handling.")]
        public static async Task<T> ExponentialBackoff<T>(this Func<Task<T>> f, ExponentialBackoffOptions? options = null, bool retryOnCapturedContext = false)
        {
            f = f ?? throw new ArgumentNullException(nameof(f));

            var (retries, meanDelay, rng, cancellationToken) = options ?? new ExponentialBackoffOptions();

            while (retries > 0)
            {
                try
                {
                    return await f().ConfigureAwait(retryOnCapturedContext);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Backoff time is distributed exponentially with mean meanDelay
                    var delay = -Math.Log(1 - rng.NextDouble()) * meanDelay;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(retryOnCapturedContext);

                    retries -= 1;   // Decrease retries
                    meanDelay *= 2; // Double delay
                }
            }

            return await f().ConfigureAwait(false);
        }
    }
}
