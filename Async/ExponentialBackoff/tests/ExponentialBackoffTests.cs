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
}
