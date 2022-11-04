using System;

namespace Luger.Functional;

public abstract class Result<TError, T>
{
    private protected Result() { }

    public class Error : Result<TError, T>
    {
        internal Error(TError value) => Value = value;

        public TError Value { get; }

        internal Result<TError, TResult>.Error Cast<TResult>() => new(Value);
    }

    public class OK : Result<TError, T>
    {
        internal OK(T value) => Value = value;

        public T Value { get; }
    }

    public static implicit operator Result<TError, T>(TError value) => new Error(value);

    public static implicit operator Result<TError, T>(T value) => new OK(value);
}

public static class Result
{
    public static Result<TError, TResult> Select<TError, TSource, TResult>(
        this Result<TError, TSource> source,
        Func<TSource, TResult> selector)

        => source switch
        {
            Result<TError, TSource>.OK okSource => selector(okSource.Value),
            Result<TError, TSource>.Error sourceError => sourceError.Cast<TResult>(),
            _ => throw new InvalidOperationException(),
        };

    public static Result<TError, TResult> SelectMany<TError, TSource, TNext, TResult>(
        this Result<TError, TSource> source,
        Func<TSource, Result<TError, TNext>> selector,
        Func<TSource, TNext, TResult> resultSelector)

        => source switch
        {
            Result<TError, TSource>.OK okSource => selector(okSource.Value) switch
            {
                Result<TError, TNext>.OK okNext => resultSelector(okSource.Value, okNext.Value),
                Result<TError, TNext>.Error nextError => nextError.Cast<TResult>(),
                _ => throw new InvalidOperationException()
            },
            Result<TError, TSource>.Error sourceError => sourceError.Cast<TResult>(),
            _ => throw new InvalidOperationException()
        };
}
