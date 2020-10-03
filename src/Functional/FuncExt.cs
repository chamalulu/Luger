using System;
using System.Collections.Generic;

namespace Luger.Functional
{
    public static class FuncExt
    {
        #region Partial application

        public static Func<T2, TR> Apply<T1, T2, TR>(this Func<T1, T2, TR> f, T1 p)
            => p2 => f(p, p2);

        public static Func<T2, T3, TR> Apply<T1, T2, T3, TR>(this Func<T1, T2, T3, TR> f, T1 p)
            => (p2, p3) => f(p, p2, p3);

        #endregion

        #region Func type inference helpers

        // Usage: var f = AsFunc(<lambda expression>);
        public static Func<TR> AsFunc<TR>(Func<TR> target) => target;

        public static Func<T, TR> AsFunc<T, TR>(Func<T, TR> target) => target;

        #endregion

        #region Curry multi parameter functions into single parameter functions

        public static Func<T1, Func<T2, TR>> Curry<T1, T2, TR>(this Func<T1, T2, TR> f)
            => p1 => p2 => f(p1, p2);

        public static Func<T1, Func<T2, Func<T3, TR>>> Curry<T1, T2, T3, TR>(this Func<T1, T2, T3, TR> f)
            => p1 => p2 => p3 => f(p1, p2, p3);

        public static Func<T1, Func<T2, T3, TR>> CurryFirst<T1, T2, T3, TR>(this Func<T1, T2, T3, TR> f)
            => p1 => (p2, p3) => f(p1, p2, p3);

        #endregion

        public static Func<TR> Map<T, TR>(this Func<T> ft, Func<T, TR> f)
            => () => f(ft());

        public static Func<TR> Apply<T, TR>(this Func<Func<T, TR>> af, Func<T> ft)
            => () => af()(ft());

        public static Func<TR> Bind<T, TR>(this Func<T> ft, Func<T, Func<TR>> f)
            => () => f(ft())();

        public static Func<T, TR> Compose<T, TC, TR>(this Func<TC, TR> g, Func<T, TC> f) =>
            t => g(f(t));

        #region LINQ method implementations

        public static Func<TR> Select<T, TR>(this Func<T> ft, Func<T, TR> f)
            => Map(ft, f);

        public static Func<TR> SelectMany<T, TR>(this Func<T> ft, Func<T, Func<TR>> f)
            => Bind(ft, f);

        public static Func<TR> SelectMany<T, TC, TR>(this Func<T> ft, Func<T, Func<TC>> f, Func<T, TC, TR> p)
            => () =>
            {
                var t = ft();
                return p(t, f(t)());
            };

        #endregion

        public static IEnumerable<T> Repeat<T>(this Func<T> f)
        {
            f = f ?? throw new ArgumentNullException(nameof(f));

            while (true) yield return f();
        }
    }
}
