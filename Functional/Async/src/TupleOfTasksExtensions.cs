using System.Runtime.CompilerServices;

namespace Luger.Functional;

/// <summary>
/// Extensions to <see cref="ValueTuple{T1, T2}"/> of <see cref="Task{TResult}"/>
/// </summary>
public static class TupleOfTasksExtensions
{
    static async Task<(T1, T2)> CombineTasks<T1, T2>(Task<T1> t1, Task<T2> t2)

        => (await t1.ConfigureAwait(false), await t2.ConfigureAwait(false));

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

        => CombineTasks(tasks.Item1, tasks.Item2).ConfigureAwait(continueOnCapturedContext);

    /// <summary>
    /// Gets an awaiter used to await this tuple of tasks.
    /// </summary>
    /// <typeparam name="T1">Type of first of <paramref name="tasks"/> result</typeparam>
    /// <typeparam name="T2">Type of second of <paramref name="tasks"/> result</typeparam>
    /// <param name="tasks">Tuple of tasks</param>
    /// <returns>An awaiter instance.</returns>
    public static TaskAwaiter<(T1, T2)> GetAwaiter<T1, T2>(this (Task<T1>, Task<T2>) tasks)

        => CombineTasks(tasks.Item1, tasks.Item2).GetAwaiter();
}
