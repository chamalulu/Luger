using System.Collections.Immutable;

namespace Luger.Async.ExponentialBackoff.Tests;

public class ExponentialBackoffTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public async Task NRetriesYieldsNPlusOneCalls(uint retries, uint expectedCalls)
    {
        uint actualCalls = 0;

        Task func()
        {
            Interlocked.Increment(ref actualCalls);
            return Task.FromException(new Exception());
        }

        async Task testCode()

            => await ExponentialBackoff
                .Over(func)
                .WithCustomDelay((d, ct) => Task.CompletedTask)
                .WithRetries(retries);

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
