using System;

namespace Luger.Functional
{
    public delegate (T Value, TState State) Transition<TState, T>(TState state);

    public static class Transition
    {
        public static Transition<TState, T> Return<TState, T>(T value) => s => (value, s);

        /// <summary>
        /// Run a transition with given state (discarding end state)
        /// </summary>
        /// <typeparam name="TState">Type of state</typeparam>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="transition">The transition to run</param>
        /// <param name="state">The state to run over</param>
        /// <returns>Return value of transition run over state</returns>
        public static T Run<TState, T>(this Transition<TState, T> transition, TState state)
        {
            if (transition is null)
                throw new ArgumentNullException(nameof(transition));

            return transition(state).Value;
        }

        /// <summary>
        /// Map function over Transition
        /// </summary>
        /// <typeparam name="TState">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="TR">Type of destination value</typeparam>
        /// <param name="transition">The transition to map over</param>
        /// <param name="f">The function to be mapped</param>
        /// <returns>Transition of state S with value of type R</returns>
        public static Transition<TState, TR> Map<TState, T, TR>(this Transition<TState, T> transition, Func<T, TR> f) =>
            s0 =>
            {
                var (t, s1) = transition(s0);
                return (f(t), s1);
            };

        /// <summary>
        /// Bind function over Transition
        /// </summary>
        /// <typeparam name="TState">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="TR">Type of destination value</typeparam>
        /// <param name="transition">The transition to bind over</param>
        /// <param name="f">The function to be bound</param>
        /// <returns>Transition of state S with value of type R</returns>
        public static Transition<TState, TR> Bind<TState, T, TR>(this Transition<TState, T> transition, Func<T, Transition<TState, TR>> f) =>
            s0 =>
            {
                var (t, s1) = transition(s0);
                return f(t)(s1);
            };

        /// <summary>
        /// LINQ Select extension over Transition
        /// </summary>
        /// <typeparam name="TState">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="TR">Type of destination value</typeparam>
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
        public static Transition<TState, TR> Select<TState, T, TR>(this Transition<TState, T> transition, Func<T, TR> f) =>
            transition.Map(f);

        /// <summary>
        /// LINQ binary SelectMany extension over Transition
        /// </summary>
        /// <typeparam name="TState">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="TR">Type of destination value</typeparam>
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
        public static Transition<TState, TR> SelectMany<TState, T, TR>(this Transition<TState, T> transition, Func<T, Transition<TState, TR>> f) =>
            transition.Bind(f);

        /// <summary>
        /// LINQ ternary SelectMany extension over Transition
        /// </summary>
        /// <typeparam name="TState">Type of state</typeparam>
        /// <typeparam name="T">Type of source value</typeparam>
        /// <typeparam name="TC">Type of intermediary value</typeparam>
        /// <typeparam name="TR">Type of destination value</typeparam>
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
        public static Transition<TState, TR> SelectMany<TState, T, TC, TR>(this Transition<TState, T> transition, Func<T, Transition<TState, TC>> f, Func<T, TC, TR> p) =>
            s0 =>
            {
                var (t, s1) = transition(s0);
                var (c, s2) = f(t)(s1);
                return (p(t, c), s2);
            };
    }
}
