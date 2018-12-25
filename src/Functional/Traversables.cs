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
    /// Traverse extension methods for Transition<S, R> over IEnumerable<T>
    /// </summary>
    /// <remarks>
    /// Transition<S, R> is not an applicative functor so only Monadic and Sequential traverse is implemented.
    /// (An implementation of Apply for Transition<S, R> wouldn't know which order to thread the state.)
    /// </remarks>
    public static class EnumerableTransitionTraversal
    {
        /// <summary>
        /// Monadic traversal of transition over sequence 
        /// </summary>
        /// <param name="ts">Sequence to traverse over</param>
        /// <param name="f">Function mapping element to transition</param>
        /// <typeparam name="S">Type of transition state</typeparam>
        /// <typeparam name="T">Type of source element</typeparam>
        /// <typeparam name="R">Type of transition result</typeparam>
        /// <returns>Transition yielding sequence results</returns>
        public static Transition<S, IEnumerable<R>> TraverseM<S, T, R>(this IEnumerable<T> ts, Func<T, Transition<S, R>> f)
        {
            var seed = Transition<S>.Return(ImmutableList<R>.Empty);

            Transition<S, ImmutableList<R>> reduce(Transition<S, ImmutableList<R>> trrs, T t)
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
        /// <typeparam name="S">Type of transition state</typeparam>
        /// <typeparam name="T">Type of source element</typeparam>
        /// <typeparam name="R">Type of transition result</typeparam>
        /// <returns>Transition yielding sequence results</returns>
        public static Transition<S, IEnumerable<R>> TraverseS<S, T, R>(this IEnumerable<T> ts, Func<T, Transition<S, R>> f)
            => state =>
            {
                var source = ts.ToArray();
                var length = source.Length;
                var result = new R[length];

                for (int i = 0; i < length; i++)
                    (result[i], state) = f(source[i])(state);

                return (result, state);
            };
    }

    /// <summary>
    /// Traverse extension methods for Task<R> over IEnumerable<T>
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
        /// <typeparam name="R">Type of task result</typeparam>
        /// <returns>Task yeilding sequence results</returns>
        public static Task<IEnumerable<R>> TraverseA<T, R>(this IEnumerable<T> ts, Func<T, Task<R>> f)
        {
            var seed = Task.FromResult(ImmutableList<R>.Empty);
            var appendTask = Task.FromResult<Func<ImmutableList<R>, R, ImmutableList<R>>>((list, t) => list.Add(t));

            Task<ImmutableList<R>> reduce(Task<ImmutableList<R>> trs, T t)
                => appendTask.Apply(trs).Apply(f(t));

            return ts.Aggregate(seed, reduce).Map(Enumerable.AsEnumerable);
        }

        /// <summary>
        /// Parallell traversal of task over sequence
        /// </summary>
        /// <param name="ts">Sequence to traverse over</param>
        /// <param name="f">Function mapping element to task</param>
        /// <typeparam name="T">Type of source element</typeparam>
        /// <typeparam name="R">Type of task result</typeparam>
        /// <returns>Task yeilding sequence results</returns>
        public static Task<IEnumerable<R>> TraverseP<T, R>(this IEnumerable<T> ts, Func<T, Task<R>> f)
            => Task.WhenAll(ts.Map(f)).Map(Enumerable.AsEnumerable);
    }
}