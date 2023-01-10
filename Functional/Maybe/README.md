# Luger.Functional.Maybe

`Luger.Functional.Maybe<T>` is a composable version of `System.Nullable<T>` for
value types and non-nullable reference types.

The value of `Maybe<T>` can be in one of two main states; some or none.

Some is analogous to a `Nullable<T>` or a nullable reference with a value.

None is analogous to a `Nullable<T>` or a nullable reference without a value,
i.e. the infamous `null`.

## Why?

The primary reason for using values of `Maybe<T>` instead of `Nullable<T>` or
nullable reference types is safety. C# is a relatively type safe language but
`null` is one piece of the language design where it breaks.

`null` has no type. A function returning `null` instead of a value of its return
type is essentially dishonest.

The problems with `null` are explained in, sometimes humorous and sometimes
painful, detail all around the interwebs. I'll not bother you with it here.

Instead, here are a couple of examples of usage together with the traditional C#
approach.

### Handling "null" return value

In a lot of C# code `null` is returned from a function to signal when the happy
path did not work out. In such cases the author of calling code must remember to
implement null-check or their code may throw an exception.

```csharp
Thing FindThing(ThingId id) {...}

void consuming_code()
{
    ThingId id = ...;

    var thing = FindThing(id);

    if (thing is null)
    {
        // Handle thing not found
    }
    else
    {
        // Handle found thing
    }
}
```

The possibility of `FindThing` returning `null` is not expressed by its
signature. The author of consuming code must read its implementation or
documentation, neither of which may be available, to find out.

In contrast, using `Maybe<T>` informs the author of consuming code that there is
a possibility of not finding a thing and to handle the thing found the return
value must be matched against.

```csharp
Maybe<Thing> FindThing(ThingId id) {...}

void consuming_code()
{
    ThingId id = ...;

    if (FindThing(id) is [var thing])
    {
        // Handle found thing
    }
    else
    {
        // Handle thing not found
    }
}
```

~~Here, `thing` is only in scope within the happy path where a thing was found and
the calling code does not risk dereferencing a `null` value.~~

Here, even though `thing` is in scope in the `else` block, the compiler should
give you an error
([CS0165](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0165))
about it being unassigned if you try to access it in the `else` block.

### Composing computations of "null"

It is common to have computations composed from a series of steps where each
step depend on results from previous steps and may result in an unhappy outcome.

```csharp
Result Computation(Input input)
{
    var result1 = Step1(input);

    if (result1 is null)
    {
        return null;
    }

    var result2 = Step2(result1);

    if (result2 is null)
    {
        return null;
    }

    return Step3(result2);
}
```

I've seen attempts to clean this up by letting the `Step#` functions handle
`null` inputs and just pass `null` on as their result.

```csharp
Result Computation(Input input) => Step3(Step2(Step1(input)));
```

The main problem with this, besides being ugly, is that now all the `Step#`
functions have dishonest signatures not only with regard to return value but
also their parameter.

Instead, if the steps return `Maybe<T>` we can use its composability to
implement the computation as a quite simple and elegant expression.

```csharp
Maybe<Result> Computation(Input input)

    => from r1 in Step1(input)
       from r2 in Step2(r1)
       from r3 in Step3(r2)
       select r3;
```

Which is precompiled to something like the following.

```csharp
Maybe<Result> Computation(Input input)

    => Step1(input)
        .SelectMany(r1 => Step2(r1), (r1, r2) => new { r1, r2 })
        .SelectMany(r1r2 => Step3(r1r2.r2), (r1r2, r3) => r3);
```

Or, if one dislikes LINQ query syntax, one can bind this, admittedly simple,
process explicitly.

```csharp
Maybe<Result> Computation(Input input) => Step1(Input).Bind(Step2).Bind(Step3);
```

This style of composition of sequentially dependent computation is called
monadic and is possible to implement for monadic types like `Maybe<T>` as they
implement `Bind` (and `SelectMany` for the LINQ query syntax support).

When inputs to a computation are sequentially independent their composition can
also be performed in an applicative style. This is possible for types which
are applicative functors (which `Maybe<T>` is) as they implement `Apply`.

A trivial example is the following computation of the sum of two `Maybe<int>`
inputs.

```csharp
Maybe<int> Sum(Maybe<int> maybeX, Maybe<int> maybeY)
{
    var maybeSum = Some((int x, int y) => x + y);

    return maybeSum.Apply(maybeX).Apply(maybeY);
}
```

## Pattern matching

You can pattern match against values of `Maybe<T>` by using C# 11
[List Patterns](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns#list-patterns)

```csharp
Console.WriteLine(maybeT is [var t] ? $"Got some {t}!" : "Got none.");
Console.WriteLine(maybeT is [] ? "Got none." : "Got some!");
```

## Equality

`Maybe<T>` implements `IEquatable<Maybe<T>>` and overrides
`object.Equals(object?)`.

The comparison works in the same way as equality comparison in `Nullable<T>`.

## Delegation of formatting to underlying type

`Maybe<T>` implements `IFormattable` which `Maybe<T>.ToString()` and
`Maybe<T>.ToString(string?)` delegates to.

`IFormattable.ToString(string?, IFormatProvider?)` delegates to the same method
on the value if `T` is `IFormattable`; otherwise `object.ToString()` is used to
produce the value representation.

Some is represented as `"[<value>]"`.

None is represented as `"[]"`.

## Playing nice with `System.Linq.Enumerable`

`Maybe<T>` implements `IEnumerable<T>`. The enumerator will yield zero or one
element for none or some respectively.

This enables `Maybe<T>` to be functionally bound (flattened) together with any
`IEnumerable<T>`.

```csharp
var flattened = from x in xs from t in funcMaybe(x) select t; // some results from funcMaybe(x) are filtered
```

## Operators

`Maybe<T>` implements truth (`true`, `false`) and logical conjunction (`&`) and
disjunction (`|`) operators.

The combination also provides conditional logical operators (`&&`, `||`). This
enables chaining of `Maybe<T>` values in logical expressions.

Using the conditional operators enables lazy evaluation as expected.

The disjunction (`|`) operator is also implemented between `Maybe<T>` and `T`.
This is useful to provide a fallback value, much like
`Nullable<T>.GetValueOrDefault(T)`

Since C# cannot handle conditional logical operators of operands of different
types, another overload of the disjunction operator is introduced in v1.1.0 to
help with lazy evaluation of fallback value. It provides functionality much like
`maybeX || getZ()` would, where `getZ` is a function providing the fallback
value.

Some illustrations;

`maybeX & maybeY` evaluates to `maybeY` if `maybeX` is some; otherwise none.

`maybeX | maybeY` evaluates to `maybeX` if it is some; otherwise `maybeY`.

`maybeX && getMaybeY()` evaluates to the result of `getMaybeY()` if `maybeX` is
some; otherwise `getMaybeY` is not invoked and the result is none.

`maybeX || getMaybeY()` evaluates to `maybeX` if it is some; otherwise
the result of `getMaybeY()`.

`maybeX | y` evaluates to the value of `maybeX` if it is some; otherwise `y`.

`maybeX | getY` where `getY` is a function returning a value of `T` evaluates to
the value of `maybeX` if it is some; otherwise the result of `getY()`.

`Maybe<T>` implements implicit cast operator from `T`.
Thus, returning some value from a `Maybe<T>`-returning function is no effort.
Returning none from a `Maybe<T>`-returning function is equally simple as it is
the default state; `return default;`.

## Factories

The static class `Maybe` implement these factory methods.

```csharp
Maybe<T> None<T>() where T : notnull
```

Produce the none value. Equivalent to `default(Maybe<T>)`.

```csharp
Maybe<T> Some<T>(T value) where T : notnull
```

Produce a some value for the given `value`. Equivalent to implicit cast
from `T` to `Maybe<T>`.

```csharp
Maybe<T> FromNullable<T>(T? value) where T : struct
```

Convert a `Nullable<T>` value to a `Maybe<T>` value.

```csharp
Maybe<T> FromReference<T>(T? value) where T : class
```

Convert a nullable reference to a `Maybe<T>` value.

## Extensions

The static class `Maybe` also implement the following extension methods.

### Apply

```csharp
Maybe<TResult> Apply<TArg, TResult>(
    this Maybe<Func<TArg, TResult>> maybeFunc,
    Maybe<TArg> maybeArg)
```
```csharp
Maybe<Func<TArg2, TResult>> Apply<TArg1, TArg2, TResult>(
    this Maybe<Func<TArg1, TArg2, TResult>> maybeFunc,
    Maybe<TArg1> maybeArg1)
```
```csharp
Maybe<Func<TArg2, TArg3, TResult>> Apply<TArg1, TArg2, TArg3, TResult>(
    this Maybe<Func<TArg1, TArg2, TArg3, TResult>> maybeFunc,
    Maybe<TArg1> maybeArg1)
```

Apply a lifted function to a lifted parameter. Overloads for lifted unary,
binary and ternary functions are implemented.

In the unary case a value of type `Maybe<TResult>` is returned. In the binary
and ternary cases a partially applied lifted function will be returned (with a
smaller arity of course).

The unary case of `Apply` corresponds to the infix operator `<*>` of Applicative
in Haskell. The overloads are provided to simplify application of binary and
ternary functions since C# does not use partial application. If you need to
apply higher arity functions you'll have to curry them yourself.

The unary overload is really the only one needed if you apply curried functions.

### Bind

```csharp
Maybe<TResult> Bind<TSource, TResult>(
    this Maybe<TSource> source,
    Func<TSource, Maybe<TResult>> func)
```

Monadic composition of the computation of `Maybe<TSource>` over the application
of a `Maybe<TResult>`-returning function.

`Bind` corresponds to the infix operator `>>=` of Monad in Haskell.

```csharp
Maybe<TResult> SelectMany<TSource, TNext, TResult>(
    this Maybe<TSource> source,
    Func<TSource, Maybe<TNext>> selector,
    Func<TSource, TNext, TResult> resultSelector)
```

Project the value of `Maybe<TSource>` to a `Maybe<TNext>` and invoke a result
selector function on the pair to produce the result.  Provided for support of
LINQ query syntax. The expression

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

### Filter

```csharp
Maybe<TSource> Filter<TSource>(
    this Maybe<TSource> source,
    Func<TSource, bool> predicate)
```

Filter a lifted value based on a predicate function.

```csharp
Maybe<TSource> Where<TSource>(
    this Maybe<TSource> source,
    Func<TSource, bool> predicate)
```

Do exactly the same as `Filter` but provided for support of LINQ query syntax.
The expression

```csharp
from s in source
where predicate(s)
select s
```

is precompiled into

```csharp
source.Where(predicate)
```

### Map

```csharp
Maybe<TResult> Map<TSource, TResult>(
    this Maybe<TSource> source,
    Func<TSource, TResult> func)
```

Map a lifted value of `TSource` by given function to a lifted value of
`TResult`.

`Map` corresponds to the infix operator `<$>` of Functor in Haskell.

```csharp
Maybe<TResult> Select<TSource, TResult>(
    this Maybe<TSource> source,
    Func<TSource, TResult> selector)
```

Project the value of `Maybe<TSource>` into a new form. This is exactly the same
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

### Try

```csharp
bool Try<TSource>(this Maybe<TSource> source, out TSource value)
```

Provides `Try`-style method syntax to extract the value of `Maybe<TSource>` for
consuming code which is not able to use C# 11 list pattern matching.

Instead of the expression `source is [var s]` such code can use
`source.Try(out var s)`.

### Nullable interop

```csharp
T? ToNullable<T>(Maybe<T> value) where T : struct
```

Convert the `Maybe<T>` value to a `Nullable<T>` value.

```csharp
T? ToReference<T>(Maybe<T> value) where T : class
```

Convert the `Maybe<T>` value to a nullable reference.
