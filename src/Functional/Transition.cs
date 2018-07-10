using System;

namespace Luger.Functional
{
    public delegate (T, S) Transition<S, T>(S state);

    public static class Transition
    {
        /// <summary>
        /// Map function over Transition
        /// </summary>
        /// <typeparam name="S">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="R">Type of destination value</typeparam>
        /// <param name="transition">The transition to map over</param>
        /// <param name="f">The function to be mapped</param>
        /// <returns>Transition of state S with value of type R</returns>
        public static Transition<S, R> Map<S, T, R>(
            this Transition<S, T> transition,
            Func<T, R> f
        )
        => s0 =>
        {
            var (t, s1) = transition(s0);
            return (f(t), s1);
        };

        /// <summary>
        /// Bind function over Transition
        /// </summary>
        /// <typeparam name="S">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="R">Type of destination value</typeparam>
        /// <param name="transition">The transition to bind over</param>
        /// <param name="f">The function to be bound</param>
        /// <returns>Transition of state S with value of type R</returns>
        public static Transition<S, R> Bind<S, T, R>(
            this Transition<S, T> transition,
            Func<T, Transition<S, R>> f
        )
        => s0 =>
        {
            var (t, s1) = transition(s0);
            return f(t)(s1);
        };

        /// <summary>
        /// LINQ Select extension over Transition
        /// </summary>
        /// <typeparam name="S">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="R">Type of destination value</typeparam>
        /// <param name="transition">The transition to map over</param>
        /// <param name="f">The function to be mapped</param>
        /// <returns>Transition of state S with value of type R</returns>
        /// <remark>
        /// Called by:
        /// <code>
        /// from t in transition
        /// select f(t)
        /// </code>
        /// </remark>
        public static Transition<S, R> Select<S, T, R>(
            this Transition<S, T> transition,
            Func<T, R> f
        )
            => transition.Map(f);

        /// <summary>
        /// LINQ binary SelectMany extension over Transition
        /// </summary>
        /// <typeparam name="S">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="R">Type of destination value</typeparam>
        /// <param name="transition">The transition to bind over</param>
        /// <param name="f">The function to be bound</param>
        /// <returns>Transition of state S with value of type R</returns>
        /// <remark>
        /// Called by:
        /// <code>
        /// from t in transition
        /// from r in f(t)
        /// select r
        /// </code>
        /// </remark>
        public static Transition<S, R> SelectMany<S, T, R>(
            this Transition<S, T> transition,
            Func<T, Transition<S, R>> f
        )
            => transition.Bind(f);

        /// <summary>
        /// LINQ ternary SelectMany extension over Transition
        /// </summary>
        /// <typeparam name="S">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="C">Type of intermediary value</typeparam>
        /// <typeparam name="R">Type of destination value</typeparam>
        /// <param name="transition">The transition to bind over</param>
        /// <param name="f">The function to be bound</param>
        /// <param name="p">Projection of T and C to R</param>
        /// <returns>Transition of state S with value of type R</returns>
        /// <remark>
        /// Called by:
        /// <code>
        /// from t in transition
        /// from c in f(t)
        /// select p(t, c)
        /// </code>
        /// </remark>
        public static Transition<S, R> SelectMany<S, T, C, R>(
            this Transition<S, T> transition,
            Func<T, Transition<S, C>> f,
            Func<T, C, R> p
        )
            => s0 =>
        {
            var (t, s1) = transition(s0);
            var (c, s2) = f(t)(s1);
            return (p(t, c), s2);
        };
    }
}