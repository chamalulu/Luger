# Luger.Async.ExponentialBackoff

Utility for configuring and awaiting exponential backoff over asynchronous
functions.

> _Exponential backoff is an algorithm that uses feedback to multiplicatively
decrease the rate of some process, in order to gradually find an acceptable
rate._ <sup>Wikipedia page **Exponential Backoff**
([this version](https://en.wikipedia.org/w/index.php?title=Exponential_backoff&oldid=1106690929))</sup>

Luger.Async.ExponentialBackoff provides an implementation that retries an
asynchronous function (_process_) after an increasingly longer delay
(_multiplicatively decrease the rate_) if it fails (_feedback_) in order to
mitigate transient failures.

## Usage

Wrap an asynchronous function `func` in exponential backoff with default
options and await it.

```csharp
await ExponentialBackoff.Over(func);
```

The return type will be `T` for `func` of type `Func<Task<T>>`.
The return type will be `System.ValueTuple` for `func` of type `Func<Task>`.

Exponential backoff can be configured before await by calling methods making up
its fluent configuration API.

```csharp
await ExponentialBackoff.Over(func).WithRetries(2);
```

## Configuration

### Limit number of retries

Number of retries before giving up and throwing the last encountered exception.

```csharp
WithRetries(uint retries)
```

Default limit number of retries is 8.

### Base delay

The mean duration of the first delay before retrying.

```csharp
WithBaseDelay(DelayTimeSpan baseDelay)
```

Default base delay is 100 ms.

`DelayTimeSpan` is a non-negative `System.TimeSpan`.

### Synchronization context marshalling

Configure exponential backoff to attempt to marshal the delays and retries back
to the original context captured.

```csharp
ConfigureRetryAwait(bool retryOnCapturedContext)
```

Default is to _not_ attempt to marshal back to the original context captured.

### Custom random number generation

Inject a custom random number generator.

```csharp
WithCustomRNG(RNGDelegate rng)
```

Default random number generator is `System.Random.NextDouble` with a fresh
instance of `System.Random` shared by all iterations of one single exponential
backoff.

The random number generator is expected to produce values in the range $[0..1)$.
Values outside this range will cause an exception to be thrown.

`RNGDelegate` has the following declaration;

```csharp
delegate double RNGDelegate();
```

### Progress reporting

Inject a progress reporting sink.

```csharp
WithProgress(IProgress<ExponentialBackoffProgress> progress)
```

Default is to not report progress.

If you need to have progress reporting performed according to a specific
synchronization context (e.g. for updating WinForm GUI elements on the GUI
thread) it's advisable to use `System.Progress<T>` as it uses the
synchronization context captured at construction.

`ExponentialBackoffProgress` has the following declaration;

```csharp
record struct ExponentialBackoffProgress(
    uint Retries,
    TimeSpan BackoffDelay,
    Exception Exception);
```

If configured, progress is reported before each delay.
`Retries` is the number of retries left.
`BackoffDelay` is the duration of this delay.
`Exception` is the exception caught from the recent await of the asynchronous
function.

### Custom delay implementation

Inject a custom delay implementation.

```csharp
WithCustomDelay(DelayDelegate delay)
```

Default delay implementation is `System.Threading.Task.Delay`.

This configuration is only useful for testing purposes when you want
deterministic control over execution and therefore want to isolate from
environmental concerns like time.

Another possible use of custom delay would be to use spin wait in high
performance scenarios, but in that case it would be better to implement a more
suitable exponential backoff to avoid the overhead of wrapping the function and
delay in `Task`. This implementation is better suited for mitigating I/O with
transient failures.

`DelayDelegate` has the following declaration;

```csharp
delegate Task DelayDelegate(TimeSpan delay, CancellationToken cancellationToken);
```

### Delay scale factor

Set scale factor to multiply mean delay duration with each iteration.

```csharp
WithFactor(TimeScaleFactor factor)
```

Default is 2.0 .

`TimeScaleFactor` is a finite, non-negative `double`.

### Cancellation

Set cancellation token with which to cancel exponential backoff.

```csharp
WithCancellation(CancellationToken cancellationToken)
```

Default is `CancellationToken.None`.

If configured, this cancellation token is used by the delay. It is not
automatically monitored by the asynchronous function. If you need the
asynchronous function to monitor the same cancellation token you have to
configure its cancellation separately.

## Details

The exponential scaling of backoff duration affects the mean durations of the
delays.

The effective durations of the delays are the mean durations multiplied by a
random jitter factor.
The jitter factor is exponentially distributed with mean 1.0 . The reason for
this is, I don't know, it's more interesting than uniform distribution.

In theory this can result in very long delays. If this becomes a practical
problem, or if someone constructively points out how stupid this is, it will be
changed.

If you want the jitter factor to be constant, inject a constant random number
generator.

If you want the jitter factor to be constant 1.0, inject a constant random
number generator producing $1-e^{-1}$.
