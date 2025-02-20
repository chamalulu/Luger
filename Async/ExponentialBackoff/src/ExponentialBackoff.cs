using System.Runtime.CompilerServices;

using JetBrains.Annotations;

using Luger.Functional;

namespace Luger.Async.ExponentialBackoff;

/// <summary>
/// Delegate for function returning a uniformly distributed random number in the interval [0..1)
/// </summary>
[PublicAPI]
public delegate double RNG();

/// <summary>
/// Progress message reported before awaiting backoff delay
/// </summary>
/// <param name="Retries">Number of retries left</param>
/// <param name="BackoffDelay">Length of backoff delay</param>
/// <param name="Exception">Exception thrown by last try</param>
[PublicAPI]
public record struct ExponentialBackoffProgress(uint Retries, TimeSpan BackoffDelay, Exception Exception);

/// <summary>
/// Delegate for function providing non-blocking delay of executing task
/// </summary>
/// <param name="delay">Duration of delay</param>
/// <param name="cancellationToken">Token for cancellation of delay</param>
/// <returns>Task to await for delay</returns>
[PublicAPI]
public delegate Task Delay(TimeSpan delay, CancellationToken cancellationToken);

/// <summary>
/// A finite non-negative <see cref="double"/>
/// </summary>
[PublicAPI]
public readonly struct TimeScaleFactor
{
    readonly double _value;

    TimeScaleFactor(double value) => this._value = value;

    /// <summary>
    /// Implicit cast from <see cref="TimeScaleFactor"/> to <see cref="double"/>
    /// </summary>
    public static implicit operator double(TimeScaleFactor timeScaleFactor) => timeScaleFactor._value;

    /// <summary>
    /// Explicit cast from <see cref="double"/> to <see cref="TimeScaleFactor"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is not finite or negative.</exception>
    public static explicit operator TimeScaleFactor(double value)

        => double.IsFinite(value) && value >= 0d ? new(value) : throw new ArgumentOutOfRangeException(nameof(value));
}

/// <summary>
/// A non-negative <see cref="TimeSpan"/>
/// </summary>
[PublicAPI]
public readonly struct DelayTimeSpan
{
    readonly TimeSpan _value;

    DelayTimeSpan(TimeSpan value) => this._value = value;

    /// <summary>
    /// Scaling of <see cref="DelayTimeSpan"/> by <see cref="TimeScaleFactor"/>
    /// </summary>
    public static DelayTimeSpan operator *(DelayTimeSpan delayTimeSpan, TimeScaleFactor factor)

        => new(delayTimeSpan._value * factor);

    /// <summary>
    /// Implicit cast from <see cref="DelayTimeSpan"/> to <see cref="TimeSpan"/>
    /// </summary>
    public static implicit operator TimeSpan(DelayTimeSpan delayTimeSpan) => delayTimeSpan._value;

    /// <summary>
    /// Explicit cast from <see cref="TimeSpan"/> to <see cref="DelayTimeSpan"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative</exception>
    public static explicit operator DelayTimeSpan(TimeSpan value)

        => value >= TimeSpan.Zero ? new(value) : throw new ArgumentOutOfRangeException(nameof(value));
}

/// <summary>
/// Wrapper of asynchronous function and exponential backoff options providing awaitable exponential backoff
/// </summary>
/// <typeparam name="TResult">Type of result of asynchronous function</typeparam>
[PublicAPI]
public sealed class ExponentialBackoffAwaitable<TResult>
{
    readonly Func<Task<TResult>> _func;

    sealed record Options(
        uint Retries = 8,
        Maybe<DelayTimeSpan> BaseDelay = default,
        bool RetryOnCapturedContext = false,
        Maybe<RNG> RNG = default,
        Maybe<IProgress<ExponentialBackoffProgress>> Progress = default,
        Maybe<Delay> Delay = default,
        Maybe<TimeScaleFactor> Factor = default,
        CancellationToken CancellationToken = default);

    readonly Options _options;

    ExponentialBackoffAwaitable(Func<Task<TResult>> func, Options options)
    {
        this._func = func;
        this._options = options;
    }

    internal ExponentialBackoffAwaitable(Func<Task<TResult>> func) : this(func, new Options()) { }

    async Task<TResult> Run()
    {
        var retries = _options.Retries;
        var meanDelay = _options.BaseDelay | (DelayTimeSpan)TimeSpan.FromMilliseconds(100);
        var rng = _options.RNG | (() => new Random().NextDouble);
        var delay = _options.Delay | Task.Delay;
        var factor = _options.Factor | (TimeScaleFactor)2d;

        while (retries > 0)
        {
            try
            {
                return await _func().ConfigureAwait(_options.RetryOnCapturedContext);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Backoff delay is distributed exponentially with mean meanDelay
                var jitter = (TimeScaleFactor)(-Math.Log(1d - rng()));
                var backoffDelay = meanDelay * jitter;

                if (_options.Progress is [var progress])
                {
                    progress.Report(new ExponentialBackoffProgress(retries, backoffDelay, exception));
                }

                await delay(backoffDelay, _options.CancellationToken).ConfigureAwait(_options.RetryOnCapturedContext);

                retries -= 1;   // Decrease retries
                meanDelay *= factor; // Scale mean delay
            }
        }

        return await _func().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an awaiter used to await this exponential backoff
    /// </summary>
    public TaskAwaiter<TResult> GetAwaiter() => Run().GetAwaiter();

    /// <summary>
    /// Set number of retries of exponential backoff before giving up.<br/>
    /// If not set, 8 retries are attempted.
    /// </summary>
    public ExponentialBackoffAwaitable<TResult> WithRetries(uint retries)

        => new(_func, _options with { Retries = retries });

    /// <summary>
    /// Set base delay of exponential backoff.<br/>
    /// If not set, 100ms is used as base delay.
    /// </summary>
    public ExponentialBackoffAwaitable<TResult> WithBaseDelay(DelayTimeSpan baseDelay)

        => new(_func, _options with { BaseDelay = baseDelay });

    /// <summary>
    /// Configure exponential backoff attempts to marshal the delays and retries back to the original context
    /// captured.<br/>
    /// If not set, or set to <see langword="false"/>, marshalling is not attempted.
    /// </summary>
    /// <param name="retryOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the delays and retries back to the original context captured;
    /// otherwise, <see langword="false"/>.
    /// </param>
    public ExponentialBackoffAwaitable<TResult> ConfigureRetryAwait(bool retryOnCapturedContext)

        => new(_func, _options with { RetryOnCapturedContext = retryOnCapturedContext });

    /// <summary>
    /// Set a custom random number generator.<br/>
    /// If not set, <see cref="Random.NextDouble()"/> is used.
    /// </summary>
    /// <param name="rng">
    /// A function returning a uniformly distributed random <see langword="double"/> in the interval [0..1).
    /// </param>
    /// <remarks>
    /// The random number generator is used to calculate an exponentially distributed random backoff delay.
    /// </remarks>
    public ExponentialBackoffAwaitable<TResult> WithCustomRNG(RNG rng)

        => new(_func, _options with { RNG = rng });

    /// <summary>
    /// Set a progress reporting sink called before each backoff delay.<br/>
    /// If not set, no progress is reported.
    /// </summary>
    public ExponentialBackoffAwaitable<TResult> WithProgress(IProgress<ExponentialBackoffProgress> progress)

        => new(_func, _options with { Progress = Maybe.Some(progress) });

    /// <summary>
    /// Set a custom delay function.<br/>
    /// If not set, <see cref="Task.Delay(TimeSpan, CancellationToken)"/> is used.
    /// </summary>
    public ExponentialBackoffAwaitable<TResult> WithCustomDelay(Delay delay)

        => new(_func, _options with { Delay = delay });

    /// <summary>
    /// Set scale factor of successive backoff delays.<br/>
    /// If not set, mean backoff delay is doubled each iteration.
    /// </summary>
    public ExponentialBackoffAwaitable<TResult> WithFactor(TimeScaleFactor factor)

        => new(_func, _options with { Factor = factor });

    /// <summary>
    /// Set cancellation token for cancellation of delay.<br/>
    /// If not set, <see cref="CancellationToken.None"/> is used.
    /// </summary>
    /// <remarks>
    /// This cancellation token is only used by the delay. The retried function need to have cancellation configured
    /// separately. Typically with the same cancellation token.
    /// </remarks>
    public ExponentialBackoffAwaitable<TResult> WithCancellation(CancellationToken cancellationToken)

        => new(_func, _options with { CancellationToken = cancellationToken });
}

/// <summary>
/// Static class providing factory functions for <see cref="ExponentialBackoffAwaitable{TResult}"/>
/// </summary>
[PublicAPI]
public static class ExponentialBackoff
{
    /// <summary>
    /// Create exponential backoff over an asynchronous function
    /// </summary>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <param name="func">Function to retry if faulting</param>
    /// <returns>
    /// Further configurable and ultimately awaitable exponential backoff over <paramref name="func"/>
    /// </returns>
    public static ExponentialBackoffAwaitable<TResult> Over<TResult>(Func<Task<TResult>> func) => new(func);

    /// <summary>
    /// Create exponential backoff over an asynchronous function
    /// </summary>
    /// <param name="func">Function to retry if faulting</param>
    /// <returns>
    /// Further configurable and ultimately awaitable exponential backoff over <paramref name="func"/>
    /// </returns>
    public static ExponentialBackoffAwaitable<ValueTuple> Over(Func<Task> func)

        => new(async () =>
        {
            await func();
            return default;
        });
}
