namespace Luger.Functional.Tests;

public class TaskExtensionsTests
{
    static Task<T> TaskOfStatus<T>(TaskStatus status, T result)

        => status switch
        {
            TaskStatus.Canceled => Task.FromCanceled<T>(new CancellationToken(true)),
            TaskStatus.Faulted => Task.FromException<T>(new Exception()),
            TaskStatus.RanToCompletion => Task.FromResult<T>(result),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    public static IEnumerable<object[]> TaskStatusTestData { get; } = from triple in new[]
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
    [MemberData(nameof(TaskStatusTestData))]
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

    [Theory]
    [MemberData(nameof(TaskStatusTestData))]
    public async Task BindTaskStatusTest(
        TaskStatus taskStatus,
        TaskStatus funcResultTaskStatus,
        TaskStatus expectedTaskStatus)
    {
        var task = TaskOfStatus(taskStatus, new object());
        Task<object> func(object _) => TaskOfStatus(funcResultTaskStatus, new object());

        var resultTask = task.Bind(func);

        var actualTaskStatus = await resultTask.ContinueWith(t => t.Status);

        Assert.Equal(expectedTaskStatus, actualTaskStatus);
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
    [MemberData(nameof(TaskStatusTestData))]
    public async Task SelectManyTaskStatusTest(
        TaskStatus sourceTaskStatus,
        TaskStatus selectorResultTaskStatus,
        TaskStatus expectedTaskStatus)
    {
        var source = TaskOfStatus(sourceTaskStatus, 0);
        Task<object> selector(object s) => TaskOfStatus(selectorResultTaskStatus, new object());
        object projection(object s, object n) => new();

        var resultTask = from s in source
                         from n in selector(s)
                         select projection(s, n);

        var actualTaskStatus = await resultTask.ContinueWith(t => t.Status);

        Assert.Equal(expectedTaskStatus, actualTaskStatus);
    }

    [Fact]
    public async Task SelectManyProjectionThrows()
    {
        var exception = new Exception();

        object projection(object s, object n) => throw exception;

        var actual = await Assert.ThrowsAnyAsync<Exception>(()
            => from s in Task.FromResult(new object())
               from n in Task.FromResult(new object())
               select projection(s, n));

        Assert.Same(exception, actual);
    }

    [Fact]
    public async Task OrElseCanceledHandleWithExceptionHandlerEdgeCase()
    {
        var canceledTask = TaskOfStatus(TaskStatus.Canceled, new object());
        var exceptionHandlerCalled = false;

        Task<object> exceptionHandler(OperationCanceledException operationCanceledException)
        {
            exceptionHandlerCalled = true;
            return Task.FromResult(new object());
        };

        await canceledTask.OrElse<object, OperationCanceledException>(exceptionHandler);

        Assert.True(exceptionHandlerCalled);
    }
}
