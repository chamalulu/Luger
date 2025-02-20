namespace Luger.Functional.Tests;

public class TaskExtensionsTests
{
    static Task<T> TaskOfStatus<T>(TaskStatus status, T result)

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        => status switch
        {
            TaskStatus.Canceled => Task.FromCanceled<T>(new CancellationToken(true)),
            TaskStatus.Faulted => Task.FromException<T>(new Exception()),
            TaskStatus.RanToCompletion => Task.FromResult(result),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    public static IEnumerable<object[]> ApplyTaskStatusTestData { get; } = from triple in new[]
    {
        (TaskStatus.Canceled, TaskStatus.Canceled, TaskStatus.Canceled),
        (TaskStatus.Canceled, TaskStatus.Faulted, TaskStatus.Faulted),
        (TaskStatus.Canceled, TaskStatus.RanToCompletion, TaskStatus.Canceled),
        (TaskStatus.Faulted, TaskStatus.Canceled, TaskStatus.Faulted),
        (TaskStatus.Faulted, TaskStatus.Faulted, TaskStatus.Faulted),
        (TaskStatus.Faulted, TaskStatus.RanToCompletion, TaskStatus.Faulted),
        (TaskStatus.RanToCompletion, TaskStatus.Canceled, TaskStatus.Canceled),
        (TaskStatus.RanToCompletion, TaskStatus.Faulted, TaskStatus.Faulted),
        (TaskStatus.RanToCompletion, TaskStatus.RanToCompletion, TaskStatus.RanToCompletion)
    } select new object[]
    {
        triple.Item1,
        triple.Item2,
        triple.Item3
    };

    [Theory]
    [MemberData(nameof(ApplyTaskStatusTestData))]
    public async Task ApplyTaskStatusTest(
        TaskStatus funcTaskStatus,
        TaskStatus argTaskStatus,
        TaskStatus expectedTaskStatus)
    {
        var funcTask = TaskOfStatus<Func<object, object>>(funcTaskStatus, _ => new object());
        var argTask = TaskOfStatus(argTaskStatus, new object());

        var resultTask = funcTask.Apply(argTask);

        var actualTaskStatus = await resultTask.ContinueWith(t => t.Status);

        Assert.Equal(expectedTaskStatus, actualTaskStatus);
    }

    [Fact]
    public async Task ApplyTaskOfParseableIntToTaskOfIntParse()
    {
        var parseTask = Task.FromResult<Func<string, int>>(int.Parse);
        var argTask = Task.FromResult("42");

        var actual = await parseTask.Apply(argTask);

        Assert.Equal(42, actual);
    }

    [Fact]
    public async Task ApplyTaskOfUnparseableIntToTaskOfIntParse()
    {
        var parseTask = Task.FromResult<Func<string, int>>(int.Parse);
        var argTask = Task.FromResult("banan");

        await Assert.ThrowsAsync<FormatException>(() => parseTask.Apply(argTask));
    }

    public static IEnumerable<object[]> BindTaskStatusTestData { get; } = from triple in new[]
    {
        (TaskStatus.Canceled, TaskStatus.Canceled, TaskStatus.Canceled),
        (TaskStatus.Canceled, TaskStatus.Faulted, TaskStatus.Canceled),
        (TaskStatus.Canceled, TaskStatus.RanToCompletion, TaskStatus.Canceled),
        (TaskStatus.Faulted, TaskStatus.Canceled, TaskStatus.Faulted),
        (TaskStatus.Faulted, TaskStatus.Faulted, TaskStatus.Faulted),
        (TaskStatus.Faulted, TaskStatus.RanToCompletion, TaskStatus.Faulted),
        (TaskStatus.RanToCompletion, TaskStatus.Canceled, TaskStatus.Canceled),
        (TaskStatus.RanToCompletion, TaskStatus.Faulted, TaskStatus.Faulted),
        (TaskStatus.RanToCompletion, TaskStatus.RanToCompletion, TaskStatus.RanToCompletion)
    } select new object[]
    {
        triple.Item1,
        triple.Item2,
        triple.Item3
    };

    [Theory]
    [MemberData(nameof(BindTaskStatusTestData))]
    public async Task BindTaskStatusTest(
        TaskStatus taskStatus,
        TaskStatus funcResultTaskStatus,
        TaskStatus expectedTaskStatus)
    {
        var task = TaskOfStatus(taskStatus, new object());

        var resultTask = task.Bind(Func);

        var actualTaskStatus = await resultTask.ContinueWith(t => t.Status);

        Assert.Equal(expectedTaskStatus, actualTaskStatus);
        return;

        Task<object> Func(object _) => TaskOfStatus(funcResultTaskStatus, new object());
    }

    static Task<int> IntParseAsync(string s) => Task.FromResult(int.Parse(s));

    [Fact]
    public async Task BindAsyncIntParseToTaskOfParseableInt()
    {
        var task = Task.FromResult("42");

        var actual = await task.Bind(IntParseAsync);

        Assert.Equal(42, actual);
    }

    [Fact]
    public async Task BindAsyncIntParseToTaskOfUnparseableInt()
    {
        var task = Task.FromResult("banan");

        await Assert.ThrowsAsync<FormatException>(() => task.Bind(IntParseAsync));
    }

    [Theory]
    [MemberData(nameof(BindTaskStatusTestData))]
    public async Task SelectManyTaskStatusTest(
        TaskStatus sourceTaskStatus,
        TaskStatus selectorResultTaskStatus,
        TaskStatus expectedTaskStatus)
    {
        var source = TaskOfStatus(sourceTaskStatus, 0);

        var resultTask = from s in source
                         from n in Selector(s)
                         select ResultSelector(s, n);

        var actualTaskStatus = await resultTask.ContinueWith(t => t.Status);

        Assert.Equal(expectedTaskStatus, actualTaskStatus);
        return;

        // ReSharper disable once UnusedParameter.Local
        Task<object> Selector(object s) => TaskOfStatus(selectorResultTaskStatus, new object());

        // ReSharper disable twice UnusedParameter.Local
        object ResultSelector(object s, object n) => new();
    }

    [Fact]
    public async Task SelectManyProjectionThrows()
    {
        var exception = new Exception();

        var actual = await Assert.ThrowsAnyAsync<Exception>(()
            => from s in Task.FromResult(new object())
               from n in Task.FromResult(new object())
               select ResultSelector(s, n));

        Assert.Same(exception, actual);
        return;

        object ResultSelector(object s, object n) => throw exception;
    }

    [Fact]
    public async Task OrElseCanceledHandleWithExceptionHandlerEdgeCase()
    {
        var canceledTask = TaskOfStatus(TaskStatus.Canceled, new object());
        var exceptionHandlerCalled = false;

        await canceledTask.OrElse<object, OperationCanceledException>(ExceptionHandler);

        Assert.True(exceptionHandlerCalled);
        return;

        Task<object> ExceptionHandler(OperationCanceledException operationCanceledException)
        {
            exceptionHandlerCalled = true;
            return Task.FromResult(new object());
        }
    }
}
