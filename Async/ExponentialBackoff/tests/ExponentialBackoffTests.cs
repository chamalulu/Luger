using System.Collections.Immutable;

namespace Luger.Async.ExponentialBackoff.Tests;

public class ExponentialBackoffTests
{
    static Task NoDelay(TimeSpan backoffDelay, CancellationToken cancellationToken) => Task.CompletedTask;

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public async void EBOverFaultingWithNRetriesYieldsNPlusOneCalls(uint retries, uint expectedCalls)
    {
        uint actualCalls = 0;

        Task<object> func()
        {
            actualCalls++;
            return Task.FromException<object>(new Exception());
        }

        async Task<object> testCode()

            => await ExponentialBackoff
                .Over(func)
                .WithRetries(retries)
                .WithCustomDelay(NoDelay);

        _ = await Assert.ThrowsAsync<Exception>(testCode);

        Assert.Equal(expectedCalls, actualCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task MeanDelayScaledByFactor(int factor)
    {
        var delaysTicks = ImmutableArray.Create<long>();

        Task delay(TimeSpan d, CancellationToken ct)
        {
            ImmutableInterlocked.Update(ref delaysTicks, (s, v) => s.Add(v), d.Ticks);

            return Task.CompletedTask;
        }

        async Task testCode()

            => await ExponentialBackoff
                .Over(() => Task.FromException(new Exception()))
                .WithBaseDelay((DelayTimeSpan)TimeSpan.FromTicks(1L))
                .WithCustomDelay(delay)
                .WithCustomRNG(() => 1d - Math.Exp(-1d))  // Always mean
                .WithFactor((TimeScaleFactor)factor)
                .WithRetries(2u);

        _ = await Assert.ThrowsAsync<Exception>(testCode);

        Assert.Equal(2, delaysTicks.Length);
        Assert.Equal(1L, delaysTicks[0]);
        Assert.Equal(factor, delaysTicks[1]);
    }
}
