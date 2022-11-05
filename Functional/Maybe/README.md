# Luger.Functional.Maybe

`Luger.Functional.Maybe<T>` is a composable version of `System.Nullable<T>` for
value types and non-nullable reference types.

The value of `Maybe<T>` can be in one of two main states; Some or None.

Some is analogous to a `Nullable<T>` or nullable reference with a value.

None is analogous to a `Nullable<T>` or nullable reference without a value, i.e.
the infamous `null`.

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

Some is represented as `"[<value>]"`.

`IFormattable.ToString(string?, IFormatProvider?)` delegates to the same method
on the value if `T` is `IFormattable`; otherwise `object.ToString()` is used to
produce the value representation.

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

`maybeX | maybeY | z` evaluates to the leftmost operand which is some; otherwise `z`.

`Maybe<T>` implements implicit cast operator from `T`.
Thus, returning some value from a `Maybe<T>`-returning function is no effort.
Returning none from a `Maybe<T>`-returning function is equally simple as it is
the default state; `return default;`.

## Factories

The static class `Maybe` implements these factory methods.

`None<T>()` gives the none value.

`Some<T>(T)` gives a some value for the given value of `T`.

`FromNullable<T>(T?) where T : struct` converts a `Nullable<T>` value to a
`Maybe<T>` value.

`FromReference<T>(T?) where T : class` converts a nullable reference to a
`Maybe<T>` value.

## Extensions

`Apply` applies a `Maybe<Func<..., TResult>>` to a parameter `Maybe<T>`. The
lifted function can be unary, binary or ternary. In the unary case a value of
type `Maybe<TResult>` is returned. In the binary and ternary cases a partially
applied lifted function will be returned.

`Bind` &lt;TODO&gt;

`Map` &lt;TODO&gt;

`Select` &lt;TODO&gt;

`SelectMany` &lt;TODO&gt;

`Try` &lt;TODO&gt;

`Where` &lt;TODO&gt;

`ToNullable() where T : struct` converts the `Maybe<T>` value to a `Nullable<T>`
value.

`ToReference() where T : class` converts the `Maybe<T>` value to a nullable
reference.
