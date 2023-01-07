# Luger.Functional.Async

`Luger.Functional.Async` contains a collection of extension methods for
`Task<TResult>` and, as it happens, also for `Task` and
`ValueTuple<Task<T1>, Task<T2>>`.

## Apply

```csharp
Task<TResult> Apply<TArg, TResult>(
    this Task<Func<TArg, TResult>> funcTask,
    Task<TArg> argTask,
    bool applyOnCapturedContext = false)
```
```csharp
Task<Func<TArg2, TResult>> Apply<TArg1, TArg2, TResult>(
    this Task<Func<TArg1, TArg2, TResult>> funcTask,
    Task<TArg1> arg1Task,
    bool applyOnCapturedContext = false)
```
```csharp
Task<Func<TArg2, TArg3, TResult>> Apply<TArg1, TArg2, TArg3, TResult>(
    this Task<Func<TArg1, TArg2, TArg3, TResult>> funcTask,
    Task<TArg1> arg1Task,
    bool applyOnCapturedContext = false)
```

Apply a lifted function to a lifted parameter. Overloads for lifted unary,
binary and ternary functions are implemented.

In the unary case an asynchronous result is returned. In the binary and ternary
cases a partially applied lifted function will be returned (with a smaller arity
of course).

The unary case of `Apply` corresponds to the infix operator `<*>` of Applicative
in Haskell. The overloads are provided to simplify application of binary and
ternary functions since C# does not use partial application. If you need to
apply higher arity functions you'll have to curry them yourself.

The unary overload is really the only one needed if you apply curried functions.

If the optional flag `applyOnCapturedContext` is `true`, `Apply` will try to
marshall the application of the function from `funcTask` back to the original
context captured.

## AsVoidTask

```csharp
Task<ValueTuple> AsVoidTask(this Task task)
```

Continue a `Task` as a `Task<ValueTuple>` to make it composable.

`void` in C# represents the absence of a return value. Void functions are not
composable as their application works more as statements than expressions.

Likewise, `Task` is not composable but `Task<T>` is.

`ValueTuple` is convenient as a stand-in for `void` as it is a type with a
singleton value space. Its value has a 0-bit state. Its value carries no
information. Whatever explains it best. `ValueTuple` is in this regard like
`unit` or `()` in F# or Haskell.

## Bind

```csharp
Task<TResult> Bind<TSource, TResult>(
    this Task<TSource> source,
    Func<TSource, Task<TResult>> func,
    bool bindOnCapturedContext = false)
```

Monadic composition of the asynchronous computation of `source` over the
application of the asynchronous function `func`.

`Bind` corresponds to the infix operator `>>=` of Monad in Haskell.

If the optional flag `bindOnCapturedContext` is `true`, `Bind` will try to
marshall the execution of the bound function back to the original context
captured.

```csharp
Task<TResult> SelectMany<TSource, TNext, TResult>(
    this Task<TSource> source,
    Func<TSource, Task<TNext>> selector,
    Func<TSource, TNext, TResult> resultSelector)
```

Project the value of `source` to a `Task<TNext>` and invoke a result selector
function on the pair to produce the result.  Provided for support of LINQ query
syntax. The expression

```csharp
from s in source
from n in selector(s)
select resultSelector(s, n)
```

is precompiled into

```csharp
source.SelectMany(selector, resultSelector)
```

The difference between `Bind` and `SelectMany` is that the latter takes a binary
projection function as a parameter and as such can chain calls instead of
encapsulating calls in nested closures.

Also, context capture can be affected by the static property
[`TaskExtensions.SelectOnCapturedContext`](#SelectOnCapturedContext).

## Map

```csharp
Task<TResult> Map<TSource, TResult>(
    this Task<TSource> source,
    Func<TSource, TResult> func,
    bool = mapOnCapturedContext = false)
```

Map the asynchronous computation of `TSource` by given function to an
asynchronous computation of `TResult`.

`Map` corresponds to the infix operator `<$>` of Functor in Haskell.

If the optional flag `mapOnCapturedContext` is `true`, `Map` will try to
marshall the execution of `func` back to the original context captured.

```csharp
Task<TResult> Select<TSource, TResult>(
    this Task<TSource> source,
    Func<TSource, TResult> selector)
```

Project the value of `Task<T>` into a new form. This is exactly the same
functionality as `Map` above but is provided for support of LINQ query syntax.
The expression

```csharp
from s in source
select selector(s)
```

is precompiled into

```csharp
source.Select(selector)
```

Context capture can be affected by the static property
[`TaskExtensions.SelectOnCapturedContext`](#SelectOnCapturedContext).

## OrElse

```csharp
Task<TResult> OrElse<TResult, TException>(
    this Task<TResult> result,
    Func<TException, Task<TResult>> exceptionHandler,
    Func<OperationCanceledException, Task<TResult>>? cancellationHandler = null,
    bool handleOnCapturedContext = false)
    where TException : Exception
```

Add exception and, optionally, cancellation handling continuations to
asynchronous computation `result`.

If `result` is successful, its result is returned.

If `result` is faulted and the exception thrown by the `await` is assignable to
`TException`, `exceptionHandler` is invoked as continuation with the exception
as argument.

If `result` is faulted and the exception thrown by the `await` is not assignable
to `TException`, the exception is not caught.

If `result` is canceled and `cancellationHandler` is not `null`,
`cancellationHandler` is invoked as continuation with the
`OperationCanceledException` thrown by the `await` as argument.

If `result` is canceled and `cancellationHandler` is `null`, the
`OperationCanceledException` is not caught.

As an edge case, if `result` is canceled, `cancellationHandler` is `null` and
`TException` is assignable from `OperationCanceledException`, `exceptionHandler`
is invoked as continuation with the `OperationCanceledException` thrown by the
`await` as argument.

## SelectOnCapturedContext

```csharp
static bool SelectOnCapturedContext { get; set; } = false
```

This static property is used by `Select` and `SelectMany` as configuration of
whether to try to marshall execution of selectors back to the original context
captured.

---

In v1.0.0-beta there was an asynchronous `Traverse` extension on
`IEnumerable<T>` mapping an asynchronous computation over a sequence of elements
to sequence of results.
It has been removed in favor of either using `Task.WhenAll<TResult>` or using
[Reactive Extensions](https://github.com/dotnet/reactive) for more robust
handling of both push-based asynchronous sequences (`IObservable<T>`) and
pull-based asynchronous sequences (`IAsyncEnumerable<T>`).

## Extensions of `(Task<T1>, Task<T2>)`

The following extensions came out of common use in the implementation of
`Luger.Functional.Async`. They may be useful by their own.

```csharp
ConfiguredTaskAwaitable<(T1, T2)> ConfigureAwait<T1, T2>(
    this (Task<T1>, Task<T2>) tasks,
    bool continueOnCapturedContext)
```

Configures an awaiter used to await this tuple of tasks.

```csharp
TaskAwaiter<(T1, T2)> GetAwaiter<T1, T2>(this (Task<T1>, Task<T2>) tasks)
```

Gets an awaiter used to await this tuple of tasks.
