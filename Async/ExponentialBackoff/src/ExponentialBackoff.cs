using System.Runtime.CompilerServices;

namespace Luger.Async.ExponentialBackoff;

/// <summary>
/// Delegate for function returning a uniformly distributed random number in the interval [0..1)
/// </summary>
public delegate double RNGDelegate();

/// <summary>
/// Progress message reported before awaiting backoff delay
/// </summary>
/// <param name="Retries">Number of retries left</param>
/// <param name="BackoffDelay">Length of backoff delay</param>
/// <param name="Exception">Exception thrown by last try</param>
public record struct ExponentialBackoffProgress(uint Retries, TimeSpan BackoffDelay, Exception Exception);

/// <summary>
/// Delegate for function providing non-blocking delay of executing task
/// </summary>
/// <param name="delay">Duration of delay</param>
/// <param name="cancellationToken">Token for cancellation of delay</param>
/// <returns>Task to await for delay</returns>
public delegate Task DelayDelegate(TimeSpan delay, CancellationToken cancellationToken);

public class ExponentialBackoffAwaitable<TResult>
{
    readonly Func<Task<TResult>> func;

    record Options(
        uint Retries = 8,
        TimeSpan? BaseDelay = null,
        bool RetryOnCapturedContext = false,
        RNGDelegate? RNG = null,
        IProgress<ExponentialBackoffProgress>? Progress = null,
        DelayDelegate? Delay = null,
        double Factor = 2d,
        CancellationToken CancellationToken = default);

    readonly Options options;

    ExponentialBackoffAwaitable(Func<Task<TResult>> func, Options options)
    {
        this.func = func;
        this.options = options;
    }

    internal ExponentialBackoffAwaitable(Func<Task<TResult>> func) : this(func, new Options()) { }

    protected async Task<TResult> Run()
    {
        var (retries, baseDelay, retryOnCapturedContext, rng, progress, delay, factor, cancellationToken) = options;

        var meanDelay = baseDelay ?? TimeSpan.FromMilliseconds(100);

        rng ??= new Random().NextDouble;

        delay ??= Task.Delay;

        while (retries > 0)
        {
            try
            {
                return await func().ConfigureAwait(retryOnCapturedContext);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Backoff delay is distributed exponentially with mean meanDelay
                var backoffDelay = -Math.Log(1 - rng()) * meanDelay;

                progress?.Report(new ExponentialBackoffProgress(retries, backoffDelay, exception));

                await delay(backoffDelay, cancellationToken).ConfigureAwait(retryOnCapturedContext);

                retries -= 1;   // Decrease retries
                meanDelay *= factor; // Scale mean delay
            }
        }

        return await func().ConfigureAwait(false);
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

        => new(func, options with { Retries = retries });

    /// <summary>
    /// Set base delay of exponential backoff.<br/>
    /// If not set, 100ms is used as base delay.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="baseDelay"/> argument is negative.
    /// </exception>
    public ExponentialBackoffAwaitable<TResult> WithBaseDelay(TimeSpan baseDelay)

        => baseDelay >= TimeSpan.Zero
            ? new(func, options with { BaseDelay = baseDelay })
            : throw new ArgumentOutOfRangeException(nameof(baseDelay));

    /// <summary>
    /// Configure exponential backoff attempts to marshal the delays and retries back to the original context captured.<br/>
    /// If not set, or set to <see langword="false"/>, marshalling is not attempted.
    /// </summary>
    /// <param name="retryOnCapturedContext">
    /// <see langword="true"/> to attempt to marshal the delays and retries back to the original context captured;
    /// otherwise, <see langword="false"/>.
    /// </param>
    public ExponentialBackoffAwaitable<TResult> ConfigureRetryAwait(bool retryOnCapturedContext)

        => new(func, options with { RetryOnCapturedContext = retryOnCapturedContext });

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
    public ExponentialBackoffAwaitable<TResult> WithCustomRNG(RNGDelegate rng)

        => new(func, options with { RNG = rng });

    /// <summary>
    /// Set a progress reporting sink called before each backoff delay.<br/>
    /// If not set, no progress is reported.
    /// </summary>
    public ExponentialBackoffAwaitable<TResult> WithProgress(IProgress<ExponentialBackoffProgress> progress)

        => new(func, options with { Progress = progress });

    /// <summary>
    /// Set a custom delay function.<br/>
    /// If not set, <see cref="Task.Delay(TimeSpan, CancellationToken)"/> is used.
    /// </summary>
    public ExponentialBackoffAwaitable<TResult> WithCustomDelay(DelayDelegate delay)

        => new(func, options with { Delay = delay });

    /// <summary>
    /// Set scale factor of successive backoff delays.<br/>
    /// If not set, mean backoff delay is doubled each iteration.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="factor"/> argument is not finite or not positive.
    /// </exception>
    public ExponentialBackoffAwaitable<TResult> WithFactor(double factor)

        => double.IsFinite(factor) && factor > 0
            ? new(func, options with { Factor = factor })
            : throw new ArgumentOutOfRangeException(nameof(factor));

    /// <summary>
    /// Set cancellation token for cancellation of delay.<br/>
    /// If not set, <see cref="CancellationToken.None"/> is used.
    /// </summary>
    /// <remarks>
    /// This cancellation token is only used by the delay. The retried function need to have cancellation configured
    /// separately. Typically with the same cancellation token.
    /// </remarks>
    public ExponentialBackoffAwaitable<TResult> WithCancellation(CancellationToken cancellationToken)

        => new(func, options with { CancellationToken = cancellationToken });
}

public static class ExponentialBackoff
{
    /// <summary>
    /// Create exponential backoff over an asynchronous function
    /// </summary>
    /// <typeparam name="TResult">Type of result</typeparam>
    /// <param name="func">Function to retry if faulting</param>
    /// <returns>Further configurable and ultimately awaitable exponential backoff over <paramref name="func"/></returns>
    public static ExponentialBackoffAwaitable<TResult> Over<TResult>(Func<Task<TResult>> func) => new(func);

    /// <summary>
    /// Create exponential backoff over an asynchronous function
    /// </summary>
    /// <param name="func">Function to retry if faulting</param>
    /// <returns>Further configurable and ultimately awaitable exponential backoff over <paramref name="func"/></returns>
    public static ExponentialBackoffAwaitable<ValueTuple> Over(Func<Task> func)

        => new(async () =>
        {
            await func();
            return default;
        });
}
