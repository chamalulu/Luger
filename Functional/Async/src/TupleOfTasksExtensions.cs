using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace Luger.Functional;

/// <summary>
/// Extensions to <see cref="ValueTuple{T1, T2}"/> of <see cref="Task{TResult}"/>
/// </summary>
[PublicAPI]
public static class TupleOfTasksExtensions
{
    /// <summary>
    /// Combine a tuple of tasks into a task of tuple.
    /// </summary>
    /// <param name="tasks">Tuple of tasks</param>
    /// <typeparam name="T1">Return type of first task</typeparam>
    /// <typeparam name="T2">Return type of second task</typeparam>
    /// <returns>A task yielding a tuple of the returned values when they are completed.</returns>
    public static async Task<(T1, T2)> Combine<T1, T2>(this (Task<T1>, Task<T2>) tasks)
    {
        await Task.WhenAll(tasks.Item1, tasks.Item2).ConfigureAwait(false);
        return (tasks.Item1.Result, tasks.Item2.Result);
    }

    /// <summary>
    /// Configures an awaiter used to await this tuple of tasks
    /// </summary>
    /// <typeparam name="T1">Type of first of <paramref name="tasks"/> result</typeparam>
    /// <typeparam name="T2">Type of second of <paramref name="tasks"/> result</typeparam>
    /// <param name="tasks">Tuple of tasks</param>
    /// <param name="continueOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the continuation back to the original context captured; otherwise,
    /// <see langword="flase"/>.
    /// </param>
    /// <returns>An object used to await this tuple of tasks</returns>
    public static ConfiguredTaskAwaitable<(T1, T2)> ConfigureAwait<T1, T2>(
        this (Task<T1>, Task<T2>) tasks,
        bool continueOnCapturedContext)

        => tasks.Combine().ConfigureAwait(continueOnCapturedContext);

    /// <summary>
    /// Gets an awaiter used to await this tuple of tasks.
    /// </summary>
    /// <typeparam name="T1">Type of first of <paramref name="tasks"/> result</typeparam>
    /// <typeparam name="T2">Type of second of <paramref name="tasks"/> result</typeparam>
    /// <param name="tasks">Tuple of tasks</param>
    /// <returns>An awaiter instance.</returns>
    public static TaskAwaiter<(T1, T2)> GetAwaiter<T1, T2>(this (Task<T1>, Task<T2>) tasks)

        => tasks.Combine().GetAwaiter();
}
