using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Luger.Functional
{
    public static class Traversable
    {
        /// <summary>
        /// (Functional) Monadic traversal of transition over sequence 
        /// </summary>
        /// <param name="ts">Sequence to traverse over</param>
        /// <param name="f">Function mapping element to transition</param>
        /// <typeparam name="S">Type of transition state</typeparam>
        /// <typeparam name="T">Type of source element</typeparam>
        /// <typeparam name="R">Type of transition result</typeparam>
        /// <returns>Transition yielding sequence results</returns>
        public static Transition<S, IEnumerable<R>> TraverseM<S, T, R>(this IEnumerable<T> ts, Func<T, Transition<S, R>> f)
            => ts.Aggregate(
                    new Transition<S, ImmutableList<R>>(s => (ImmutableList.Create<R>(), s)),
                    (trrs, t) => from rs in trrs from r in f(t) select rs.Add(r)
                ).Map(list => list.AsEnumerable());
        
        /// <summary>
        /// (Imperative) Monadic traversal of transition over sequence
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

                for(int i = 0; i < length; i++)
                    (result[i], state) = f(source[i])(state);
                
                return (result, state);
            };
    }
}