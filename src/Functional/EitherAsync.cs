using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Luger.Functional
{
    /// <summary>
    /// A discriminated union of <typeparamref name="TLeft"/> and <typeparamref name="TRight"/> wrapped in a <see cref="Task{TResult}"/> with
    /// Railway-oriented, functional map and bind methods.
    /// </summary>
    /// <inheritdoc cref="Either{TLeft, TRight}"/>
    public readonly struct EitherAsync<TLeft, TRight>
    {
        private readonly Task<Either<TLeft, TRight>> _inner;

        private EitherAsync(Task<Either<TLeft, TRight>> inner)
            => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        /// <summary>
        /// Enable using <c>await</c> keyword on an expression of type <see cref="EitherAsync{TLeft, TRight}"/>
        /// </summary>
        public TaskAwaiter<Either<TLeft, TRight>> GetAwaiter() => _inner.GetAwaiter();

        /// <inheritdoc cref="Either{TLeft, TRight}.Match"/>
        public Task<TResult> MatchAsync<TResult>(Func<TLeft, TResult> left, Func<TRight, TResult> right)
            => TaskExtensions.Select(this._inner, e => e.Match(left, right));

        /// <inheritdoc cref="Either{TLeft, TRight}.Map"/>
        public EitherAsync<TLeft, TResult> Map<TResult>(Func<TRight, TResult> func)
            => TaskExtensions.Select(this._inner, e => e.Map(func));

        /// <inheritdoc cref="Either{TLeft, TRight}.Bind"/>
        public EitherAsync<TLeft, TResult> Bind<TResult>(Func<TRight, EitherAsync<TLeft, TResult>> func)
        {
            Task<Either<TLeft, TResult>> selector(Either<TLeft, TRight> source)
                => source.Match(
                    left: l => Task.FromResult<Either<TLeft, TResult>>(l),
                    right: r => func(r)._inner);

            return TaskExtensions.Bind(this._inner, selector);
        }

        public static implicit operator EitherAsync<TLeft, TRight>(Task<Either<TLeft, TRight>> taskEither)
            => new EitherAsync<TLeft, TRight>(taskEither);

        /// <summary>
        /// Access the value as a <see cref="Task{TResult}"/> of <see cref="Either{TLeft, TRight}"/>
        /// </summary>
        public Task<Either<TLeft, TRight>> AsTask() => _inner;  // TODO: Is there a potential problem with giving access to the wrapped Task? Should it be protected in some way?
    }

    public static class EitherAsyncExtensions
    {
        /// <summary>
        /// LINQ query extension method for mapping a <paramref name="selector"/> function over the functor <see cref="EitherAsync{TLeft, TRight}"/>
        /// </summary>
        /// <inheritdoc cref="EitherExtensions.Select"/>
        public static EitherAsync<TLeft, TResult> Select<TLeft, TSource, TResult>(
            this EitherAsync<TLeft, TSource> source,
            Func<TSource, TResult> selector)

            => source.Map(selector);

        /// <summary>
        /// LINQ query extension method for binding a <paramref name="selector"/> function over the monad <see cref="EitherAsync{TLeft, TRight}"/>
        /// </summary>
        /// <inheritdoc cref="EitherExtensions.SelectMany"/>
        public static EitherAsync<TLeft, TResult> SelectMany<TLeft, TSource, TNext, TResult>(
            this EitherAsync<TLeft, TSource> source,
            Func<TSource, EitherAsync<TLeft, TNext>> selector,
            Func<TSource, TNext, TResult> projection)

            => source.Bind(s => selector(s).Map(n => projection(s, n)));

        /// <summary>
        /// LINQ query extension method for binding a synchronous <paramref name="selector"/> function over the monad <see cref="EitherAsync{TLeft, TRight}"/>
        /// </summary>
        /// <inheritdoc cref="EitherExtensions.SelectMany"/>
        public static EitherAsync<TLeft, TResult> SelectMany<TLeft, TSource, TNext, TResult>(
            this EitherAsync<TLeft, TSource> source,
            Func<TSource, Either<TLeft, TNext>> selector,
            Func<TSource, TNext, TResult> projection)
        {
            EitherAsync<TLeft, TResult> func(TSource s)
                => Task.FromResult(selector(s).Map(n => projection(s, n)));

            return source.Bind(func);
        }

        /// <summary>
        /// LINQ query extension method for binding an asynchronous, simple-valued <paramref name="selector"/> function over the monad <see cref="EitherAsync{TLeft, TRight}"/>
        /// </summary>
        /// <inheritdoc cref="EitherExtensions.SelectMany"/>
        public static EitherAsync<TLeft, TResult> SelectMany<TLeft, TSource, TNext, TResult>(
            this EitherAsync<TLeft, TSource> source,
            Func<TSource, Task<TNext>> selector,
            Func<TSource, TNext, TResult> projection)
        {
            EitherAsync<TLeft, TResult> func(TSource s)
                => selector(s).Select(n => (Either<TLeft, TResult>)projection(s, n));

            return source.Bind(func);
        }
    }
}
