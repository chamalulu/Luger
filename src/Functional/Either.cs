using System;
using System.Diagnostics;

namespace Luger.Functional
{
    /// <summary>
    /// A discriminated union of <typeparamref name="TLeft"/> and <typeparamref name="TRight"/> with
    /// Railway-oriented, functional map and bind methods.
    /// </summary>
    /// <typeparam name="TLeft">Type of left (derailing) value</typeparam>
    /// <typeparam name="TRight">Type of right (on track) value</typeparam>
    [DebuggerStepThrough]
    public struct Either<TLeft, TRight>
    {
        private readonly TLeft _left;
        private readonly TRight _right;
        private readonly bool _isRight;

        private Either(TLeft left)
        {
            _left = left;
            _right = default!;
            _isRight = false;
        }

        private Either(TRight right)
        {
            _left = default!;
            _right = right;
            _isRight = true;
        }

        /// <summary>
        /// Pattern matching against left or right case of discriminated union
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="left">Left case function</param>
        /// <param name="right">Right case function</param>
        /// <returns>Result of either left or right case function depending on value</returns>
        public TResult Match<TResult>(Func<TLeft, TResult> left, Func<TRight, TResult> right)
        {
            left = left ?? throw new ArgumentNullException(nameof(left));
            right = right ?? throw new ArgumentNullException(nameof(right));

            return _isRight
                ? right(_right)
                : left(_left);
        }

        /// <summary>
        /// Map value within discriminated union functor
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="func">Right case map function</param>
        /// <returns>Mapped value in right case. Casted original value in left case.</returns>
        public Either<TLeft, TResult> Map<TResult>(Func<TRight, TResult> func)
            => Match<Either<TLeft, TResult>>(
                left: l => l,
                right: r => func(r));

        /// <summary>
        /// Bind value within discriminated union monad
        /// </summary>
        /// <typeparam name="TResult">Type of result</typeparam>
        /// <param name="func">Right case bind function</param>
        /// <returns>Bound result in right case. Casted original value in left case.</returns>
        public Either<TLeft, TResult> Bind<TResult>(Func<TRight, Either<TLeft, TResult>> func)
            => Match(
                left: l => l,
                right: func);

        public static implicit operator Either<TLeft, TRight>(TLeft value)
            => new Either<TLeft, TRight>(value);

        public static implicit operator Either<TLeft, TRight>(TRight value)
            => new Either<TLeft, TRight>(value);
    }

    public static class EitherExtensions
    {
        /// <summary>
        /// LINQ query extension method for mapping a <paramref name="selector"/> function over the functor <see cref="Either{TLeft, TRight}"/>
        /// </summary>
        /// <remarks>
        /// The expression
        /// <code>
        /// from r in e<br/>
        /// select f(r)
        /// </code>
        /// is (roughly) translated into
        /// <code>
        /// e.Select(r => f(r))
        /// </code>
        /// </remarks>
        public static Either<TLeft, TResult> Select<TLeft, TSource, TResult>(
            this Either<TLeft, TSource> source,
            Func<TSource, TResult> selector)

            => source.Map(selector);

        /// <summary>
        /// LINQ query extension method for binding a <paramref name="selector"/> function over the monad <see cref="Either{TLeft, TRight}"/>
        /// </summary>
        /// <remarks>
        /// The expression
        /// <code>
        /// from r in e<br/>
        /// from n in f(r)<br/>
        /// select p(r, n)
        /// </code>
        /// is (roughly) translated into
        /// <code>
        /// e.SelectMany(r => f(r), (r, n) => p(r, n))
        /// </code>
        /// </remarks>
        public static Either<TLeft, TResult> SelectMany<TLeft, TSource, TNext, TResult>(
            this Either<TLeft, TSource> source,
            Func<TSource, Either<TLeft, TNext>> selector,
            Func<TSource, TNext, TResult> projection)

            => source.Bind(s => selector(s).Map(n => projection(s, n)));
    }
}
