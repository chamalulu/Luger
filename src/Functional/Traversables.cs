using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Luger.Functional
{
    /* Traversal is a high order function to traverse a traversable structure with a lifting
     *  function which returns an (applicative) functor of the traversable result.
     * It's similar to Map but reverse the stack of functors in the result.
     *
     * Mapping Func<T, F<R>> over Tr<T> yields a result of type Tr<F<R>>.
     * Traversing Tr<T> with Func<T, F<R>> yields a result of type F<Tr<R>>.
     *
     * Traversing can be done in a monadic or applicative fashion which often translates to
     *  sequential or parallell invocation of the function.
     * Since C# is based on an imperative execution model most implementations below will be
     *  imperative for simplicity and performance.
     * The traverse functions are postfixed with 'M', 'A', 'S' or 'P' for functional (M)onadic,
     *  functional (A)pplicative, imperative (S)equential and imperative (P)arallell respectively
     *  to indicate type of implementation.
     * Not all variants are, and cannot correctly be, implemented.
     */

    /// <summary>
    /// Traverse extension methods for <see cref="Transition{TState, T}"/> over <see cref="IEnumerable{T}"/>
    /// </summary>
    /// <remarks>
    /// <see cref="Transition{TState, T}"/> is not an applicative functor so only Monadic and Sequential traverse is implemented.
    /// (An implementation of Apply for <see cref="Transition{TState, T}"/> wouldn't know which order to thread the state.)
    /// </remarks>
    public static class EnumerableTransitionTraversal
    {
        /// <summary>
        /// Monadic traversal of transition over sequence 
        /// </summary>
        /// <param name="ts">Sequence to traverse over</param>
        /// <param name="f">Function mapping element to transition</param>
        /// <typeparam name="TState">Type of transition state</typeparam>
        /// <typeparam name="T">Type of source element</typeparam>
        /// <typeparam name="TR">Type of transition result</typeparam>
        /// <returns>Transition yielding sequence results</returns>
        public static Transition<TState, IEnumerable<TR>> TraverseM<TState, T, TR>(this IEnumerable<T> ts, Func<T, Transition<TState, TR>> f)
        {
            var seed = Transition.Return<TState, ImmutableList<TR>>(ImmutableList<TR>.Empty);

            Transition<TState, ImmutableList<TR>> reduce(Transition<TState, ImmutableList<TR>> trrs, T t)
                => from rs in trrs
                   from r in f(t)
                   select rs.Add(r);

            return ts.Aggregate(seed, reduce).Map(Enumerable.AsEnumerable);
        }

        /// <summary>
        /// Sequential traversal of transition over sequence
        /// </summary>
        /// <param name="ts">Sequence to traverse over</param>
        /// <param name="f">Function mapping element to transition</param>
        /// <typeparam name="TState">Type of transition state</typeparam>
        /// <typeparam name="T">Type of source element</typeparam>
        /// <typeparam name="TR">Type of transition result</typeparam>
        /// <returns>Transition yielding sequence results</returns>
        public static Transition<TState, IEnumerable<TR>> TraverseS<TState, T, TR>(this IEnumerable<T> ts, Func<T, Transition<TState, TR>> f)
            => state =>
            {
                var source = ts.ToArray();
                var length = source.Length;
                var result = new TR[length];

                for (int i = 0; i < length; i++)
                    (result[i], state) = f(source[i])(state);

                return (result, state);
            };
    }

    /// <summary>
    /// Traverse extension methods for <see cref="Task{TResult}"/> over <see cref="IEnumerable{T}"/>
    /// </summary>
    /// <remarks>
    /// I see no point in running independent tasks sequentially so only Applicative and Parallell traverse is implemented.
    /// </remarks>
    public static class EnumerableTaskTraversal
    {
        /// <summary>
        /// Applicative traversal of task over sequence
        /// </summary>
        /// <param name="ts">Sequence to traverse over</param>
        /// <param name="f">Function mapping element to task</param>
        /// <typeparam name="T">Type of source element</typeparam>
        /// <typeparam name="TR">Type of task result</typeparam>
        /// <returns>Task yeilding sequence results</returns>
        public static Task<IEnumerable<TR>> TraverseA<T, TR>(this IEnumerable<T> ts, Func<T, Task<TR>> f)
        {
            var seed = Task.FromResult(ImmutableList<TR>.Empty);
            var appendTask = Task.FromResult<Func<ImmutableList<TR>, TR, ImmutableList<TR>>>((list, t) => list.Add(t));

            Task<ImmutableList<TR>> reduce(Task<ImmutableList<TR>> trs, T t)
                => appendTask.Apply(trs).Apply(f(t));

            return ts.Aggregate(seed, reduce).Map(Enumerable.AsEnumerable);
        }

        /// <summary>
        /// Parallell traversal of task over sequence
        /// </summary>
        /// <param name="ts">Sequence to traverse over</param>
        /// <param name="f">Function mapping element to task</param>
        /// <typeparam name="T">Type of source element</typeparam>
        /// <typeparam name="TR">Type of task result</typeparam>
        /// <returns>Task yeilding sequence results</returns>
        public static Task<IEnumerable<TR>> TraverseP<T, TR>(this IEnumerable<T> ts, Func<T, Task<TR>> f)
            => Task.WhenAll(ts.Map(f)).Map(Enumerable.AsEnumerable);
    }
}
