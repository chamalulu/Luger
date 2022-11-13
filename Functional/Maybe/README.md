# Luger.Functional.Maybe

`Luger.Functional.Maybe<T>` is a composable version of `System.Nullable<T>` for
value types and non-nullable reference types.

The value of `Maybe<T>` can be in one of two main states; Some or None.

Some is analogous to a `Nullable<T>` or a nullable reference with a value.

None is analogous to a `Nullable<T>` or a nullable reference without a value,
i.e. the infamous `null`.

## Why?

The primary reason for using values of `Maybe<T>` instead of `Nullable<T>` or
nullable reference types is safety. C# is a relatively type safe language but
`null` is where it breaks.

`null` has no type and a function returning `null` instead of a value of it's
return type is essentially dishonest.

The problems with `null` are explained in, sometimes humourous and sometimes
painful, detail all around the interwebs. I'll not bother you with it here.

Instead, here are a couple of examples of usage together with the traditional C#
approach.

### Handling "null" return value

In a lot of C# code `null` is returned from a function when the happy path did
not work out. In such cases the author of calling code must remember to
implement null-check or the code may throw an exception.

```cs
Thing FindThing(ThingId id) {...}

void calling_code()
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

```cs
Maybe<Thing> FindThing(ThingId id) {...}

void calling_code()
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

Here, `thing` is only in scope within the happy path where a thing was found and
the calling code does not risk dereferencing a `null` value.

### Composing sequential computations of "null"

It is common to have logic which makes a series of transformations where each
one may result in an unhappy outcome.

```cs
Result Process(Input input)
{
    var subResult1 = Step1(input);

    if (subResult1 is null)
    {
        return null;
    }

    var subResult2 = Step2(subResult1);

    if (subResult2 is null)
    {
        return null;
    }

    return Step3(subResult2);
}
```

I've seen attempts to clean this up by letting the `Step#` functions handle
`null` inputs and just pass `null` on as their result.

```cs
Result Process(Input input) => Step3(Step2(Step1(input)));
```

The main problem with this, besides ugly code, is that now all the `Step#`
functions have dishonest signatures not only with regard to return value but
also their parameter.

Instead, if the steps use `Maybe<T>` we can use its composability to implement
the process as a quite simple and elegant expression.

```cs
Maybe<Result> Process(Input input)

    => from r1 in Step1(input)
       from r2 in Step2(r1)
       from r3 in Step3(r2)
       select r3;
```

Which is precompiled to something like the following.

```cs
Maybe<Result> Process(Input input)

    => Step1(input)
        .SelectMany(r1 => Step2(r1), (r1, r2) => new { r1, r2 })
        .SelectMany(r1r2 => Step3(r1r2.r2), (r1r2, r3) => r3);
```

Or, if one dislikes LINQ query syntax, one can bind this, admittedly simple,
process explicitly.

```cs
Maybe<Result> Process(Input input) => Step1(Input).Bind(Step2).Bind(Step3);
```

### Composing parallell computations of "null"

&lt;TODO&gt;


## Pattern matching

You can pattern match against values of `Maybe<T>` by using C# 11
[List Patterns](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns#list-patterns)

```cs
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

```cs
var flattened = from x in xs from t in funcMaybe(x) select t; // some results from funcMaybe(x) are filtered.
```

## Operators

`Maybe<T>` implements truth (`true`, `false`) and logical conjunction (`&`) and
disjunction (`|`) operators.

The combination also provides conditional logical operators (`&&`, `||`). This
enables chaining of `Maybe<T>` values in logical expressions.

Using the conditional operators enables on-demand evaluation as expected.

The disjunction (`|`) operator is also implemented between `Maybe<T>` and `T`.
This is useful to provide a fallback value, much like
`Nullable<T>.GetValueOrDefault(T)`

Some examples;

`maybeX & maybeY & maybeZ` evaluates to the rightmost operand (`maybeZ`) if all
are some; otherwise none.

`maybeX | maybeY | maybeZ` evaluates to the leftmost operand which is some;
otherwise none.

`maybeX | maybeY | z` evaluates to the value of the leftmost operand which is
some; otherwise `z`.

`Maybe<T>` implements implicit cast operator from `T`.
Thus, returning some value from a `Maybe<T>`-returning function is no effort.
Returning none from a `Maybe<T>`-returning function is equally simple as it is
the default state; `return default;`.

## Factories

The static class `Maybe` implement these factory methods.

`Maybe<T> None<T>()` gives the none value.

`Maybe<T> Some<T>(T)` gives a some value for the given value of `T`.

`Maybe<T> FromNullable<T>(T?) where T : struct` converts a `Nullable<T>` value
to a `Maybe<T>` value.

`Maybe<T> FromReference<T>(T?) where T : class` converts a nullable reference to
a `Maybe<T>` value.

## Extensions

The static class `Maybe` also implement the following extension methods.

### Apply

`Maybe<TR> Apply(Maybe<Func<T, TR>>, Maybe<T>)`

`Maybe<Func<T2, TR>> Apply(Maybe<Func<T1, T2, TR>>, Maybe<T1>)`

`Maybe<Func<T2, T3, TR>> Apply(Maybe<Func<T1, T2, T3, TR>>, Maybe<T1>)`

Applies a lifted function to a lifted parameter. Overloads for lifted unary,
binary and ternary functions are implemented.

In the unary case a value of type `Maybe<TR>` is returned. In the binary and
ternary cases a partially applied lifted function will be returned (with a
smaller arity of course).

The unary case of `Apply` corresponds to the infix operator `<*>` of Applicative
in Haskell. The overloads are provided to simplify application of binary and
ternary functions since C# does not use partial application.

### Bind

`Maybe<TR> Bind(Maybe<T>, Func<T, Maybe<TR>>)`

Sequentially combines the computation of `Maybe<T>` over the application of a
`Maybe<TR>`-returning function.

`Bind` corresponds to the infix operator `>>=` of Monad in Haskell.

`Maybe<TR> SelectMany(Maybe<TSource>, Func<TSource, Maybe<TNext>>, Func<TSource, TNext, TResult>)`

Projects the value of `Maybe<TSource>` to a `Maybe<TNext>` and invokes a result
selector function on the pair to produce the result.  Provided for support of
LINQ query syntax. The expression

```cs
from s in source
from n in selector(s)
select resultSelector(s, n)
```

is precompiled into

```cs
source.SelectMany(selector, resultSelector)
```

The difference between `Bind` and `SelectMany` is that the latter takes a binary
projection function as a parameter and as such can chain calls instead of
encapsulating calls in nested closures.

### Filter

`Maybe<T> Filter(Maybe<T>, Func<T, bool>)`

Filters a lifted value based on a predicate function.

`Maybe<TSource> Where(Maybe<TSource>, Func<TSource, bool>)`

Does exactly the same as `Filter` but is provided for support of LINQ query
syntax. The expression

```cs
from s in source
where predicate(s)
select s
```

is precompiled into

```cs
source.Where(predicate)
```

### Map

`Maybe<TR> Map(Maybe<T>, Func<T, TR>)`

Maps a lifted value of `T` by given function to a lifted value of `TR`.

`Map` corresponds to the infix operator `<$>` of Functor in Haskell.

`Maybe<TResult> Select(Maybe<TSource>, Func<TSource, TResult>)`

Projects the value of `Maybe<T>` into a new form. This is exactly the same
functionality as `Map` above but is provided for support of LINQ query syntax.
The expression

```cs
from s in source
select selector(s)
```

is precompiled into

```cs
source.Select(selector)
```

### Try

`bool Try(Maybe<T>, out T value)`

Provides `Try`-style method syntax to extract the value of `Maybe<T>` if
consuming code is not able to use C# 11 list pattern matching.

Instead of the expression `maybeT is [var t]` such code can use
`maybeT.Try(out var t)`.

### Nullable interop

`T? ToNullable(Maybe<T>) where T : struct`

Converts the `Maybe<T>` value to a `Nullable<T>` value.

`T? ToReference() where T : class`

Converts the `Maybe<T>` value to a nullable reference.
