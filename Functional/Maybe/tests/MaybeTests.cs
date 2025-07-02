using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Xunit;

using static Luger.Functional.Maybe;

namespace Luger.Functional.Tests;

public class MaybeTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData(42, 1)]
    public void CountTheory(int? maybe, int expected) => Assert.Equal(expected, FromNullable(maybe).Count);

    [Fact]
    public void IndexSomeFirst() => Assert.Equal(42, Some(42)[0]);

    [Theory]
    [InlineData(null, 0)]
    [InlineData(null, 1)]
    [InlineData(42, 1)]
    public void IndexThrowsTheory(int? maybe, int index)

        => Assert.Throws<InvalidOperationException>(() => FromNullable(maybe)[index]);

    [Fact]
    public void ListPatternNoneMatchEmpty() => Assert.True(None<int>() is []);

    [Fact]
    public void ListPatternNoneDoNotMatchSingleton() => Assert.False(None<int>() is [_]);

    [Fact]
    public void ListPatternSomeDoNotMatchEmpty() => Assert.False(Some(42) is []);

    [Fact]
    public void ListPatternSomeMatchSingleton() => Assert.True(Some(42) is [_]);

    [Theory]
    [InlineData(null, null)]
    [InlineData(42, 42)]
    public void EquatableEqualsTrue(int? maybeThis, int? maybeOther)

        => Assert.True(FromNullable(maybeThis).Equals(FromNullable(maybeOther)));

    [Theory]
    [InlineData(null, 42)]
    [InlineData(42, null)]
    [InlineData(42, 43)]
    public void EquatableEqualsFalse(int? maybeThis, int? maybeOther)

        => Assert.False(FromNullable(maybeThis).Equals(FromNullable(maybeOther)));

    [Theory]
    [InlineData(null, null)]
    [InlineData(42, 42)]
    public void ObjectEqualsTrue(int? maybeThis, object? obj) => Assert.True(FromNullable(maybeThis).Equals(obj));

    [Theory]
    [InlineData(null, "banan")]
    [InlineData(42, null)]
    [InlineData(42, "banan")]
    public void ObjectEqualsFalse(int? maybeThis, object? obj) => Assert.False(FromNullable(maybeThis).Equals(obj));

    [Fact]
    public void GetHashCodeNoneFact() => Assert.Equal(0, None<int>().GetHashCode());

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void GetHashCodeSomeTheory(int i) => Assert.Equal(i.GetHashCode(), ((Maybe<int>)i).GetHashCode());

    [Fact]
    public void GetEnumeratorNoneEmpty() => Assert.Empty(None<int>());

    [Fact]
    public void GetEnumeratorSomeSingleton() => Assert.Single(Some(42));

    [Theory]
    [InlineData(null, null, null, "[]")]
    [InlineData(1000, "D", null, "[1000]")]
    [InlineData(1000, "N2", "en-US", "[1,000.00]")]
    [InlineData(1000, "N2", "sv-SE", "[1\u00A0000,00]")]    // So, the Swedish thousand separator is a non-breaking space. Obviously.
    public void FormattableToStringTheory(int? maybe, string? format, string? cultureName, string expected)
    {
        var formatProvider = cultureName is not null
            ? System.Globalization.CultureInfo.GetCultureInfo(cultureName)
            : null;

        Assert.Equal(expected, FromNullable(maybe).ToString(format, formatProvider));
    }

    [Fact]
    public void OpTrueNoneFalse() => Assert.False(None<int>() ? true : false);

    [Fact]
    public void OpTrueSomeTrue() => Assert.True(Some(42) ? true : false);

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, 42, null)]
    [InlineData(42, null, null)]
    [InlineData(42, 43, 43)]
    public void OpAndTheory(int? maybeX, int? maybeY, int? expected)

        => Assert.Equal(FromNullable(expected), FromNullable(maybeX) & FromNullable(maybeY));

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(null, 42, 42)]
    [InlineData(42, null, 42)]
    [InlineData(42, 43, 42)]
    public void OpOrTheory(int? maybeX, int? maybeY, int? expected)

        => Assert.Equal(FromNullable(expected), FromNullable(maybeX) | FromNullable(maybeY));

    [Theory]
    [InlineData(null, 42, 42)]
    [InlineData(42, 43, 42)]
    public void OpOrTTheory(int? maybeX, int y, int? expected)

        => Assert.Equal(FromNullable(expected), FromNullable(maybeX) | y);

    [Theory]
    [InlineData(null, true)]
    [InlineData(42, false)]
    public void OpOrTConditionalInvokeTheory(int? maybeX, bool expected)
    {
        var invoked = false;

        _ = FromNullable(maybeX) | Factory;

        Assert.Equal(expected, invoked);
        return;

        int Factory()
        {
            invoked = true;
            return 0;
        }
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(42, 42)]
    public void OpEqualTrueTheory(int? maybeX, int? maybeY)

        => Assert.True(FromNullable(maybeX) == FromNullable(maybeY));

    [Theory]
    [InlineData(null, 42)]
    [InlineData(42, null)]
    [InlineData(42, 43)]
    public void OpEqualFalseTheory(int? maybeX, int? maybeY)

        => Assert.False(FromNullable(maybeX) == FromNullable(maybeY));

    [Theory]
    [InlineData(null, 42)]
    [InlineData(42, null)]
    [InlineData(42, 43)]
    public void OpNotEqualTrueTheory(int? maybeX, int? maybeY)

        => Assert.True(FromNullable(maybeX) != FromNullable(maybeY));

    [Theory]
    [InlineData(null, null)]
    [InlineData(42, 42)]
    public void OpNotEqualFalseTheory(int? maybeX, int? maybeY)

        => Assert.False(FromNullable(maybeX) != FromNullable(maybeY));

    [Fact]
    public void OpImplicitValue() => Assert.True(((Maybe<int>)42) is [42]);

    [Fact]
    public void OpImplicitReference() => Assert.True(((Maybe<string>)"banan") is ["banan"]);

    [Fact]
    public void OpImplicitReferenceNullThrows() => Assert.Throws<ArgumentNullException>(() => (Maybe<string>)null!);

    [Fact]
    public void SomeNullThrows() => Assert.Throws<ArgumentNullException>(() => Some<object>(null!));

    public static IEnumerable<object[]> ApplyTheoryArguments

        => from f in new Func<int, Func<int, Func<int, int>>>?[] { null, k => m => x => k * x + m }
           from k in new int?[] { null, 2 }
           from m in new int?[] { null, 1 }
           from x in new int?[] { null, 42 }
           let expected = f is not null && k.HasValue && m.HasValue && x.HasValue ? 85 : default(int?)
           select new object[] { f, k, m, x, expected };

    /* I was a bit surprised that the arguments created above gets passed correctly to the test method below.
     * But since they are held in an object array the arguments runtime types are just Func<int, int, int, int> and int.
     * If I understand correctly non-nullable reference types are not distinct from ordinary reference types at runtime
     * and Nullable<T> are boxed as simply T wrapped in an object reference, or null.
     * When the arguments are passed to the parameters of the test method the implicit cast from T to Maybe<T> is
     * invoked for non-null arguments.
     * I guess xUnit do something to handle passing null arguments to value type parameters. They seem to be passed as
     * default values which in this case is the right thing.
     */

    [Theory]
    [MemberData(nameof(ApplyTheoryArguments))]
    public void ApplyTheory(
        Maybe<Func<int, Func<int, Func<int, int>>>> maybeCurriedFunc,
        Maybe<int> maybeK,
        Maybe<int> maybeM,
        Maybe<int> maybeX,
        Maybe<int> expected)

        => Assert.Equal(expected, maybeCurriedFunc.Apply(maybeK).Apply(maybeM).Apply(maybeX));

    static Maybe<int> ParseInt(string s)

        => int.TryParse(s, out var i)
            ? Some(i)
            : default;

    [Theory]
    [InlineData(null, null)]
    [InlineData("banan", null)]
    [InlineData("42", 42)]
    public void BindTheory(string? maybeS, int? expected)

        => Assert.Equal(FromNullable(expected), FromReference(maybeS).Bind(ParseInt));

    [Theory]
    [InlineData(null, null)]
    [InlineData(42, "42")]
    public void MapTheory(int? maybeI, string? expected)

        => Assert.Equal(FromReference(expected), FromNullable(maybeI).Map(i => i.ToString()));

    // Since Select directly calls Map this is not much of a test.
    // It's more just a demonstration of using LINQ query syntax with Maybe<T>.
    [Theory]
    [InlineData(null, null)]
    [InlineData(42, "42")]
    public void SelectTheory(int? maybeI, string? expected)

        => Assert.Equal(
            expected: FromReference(expected),
            actual: from i in FromNullable(maybeI)
                    select i.ToString());

    [Theory]
    [InlineData(null, null)]
    [InlineData("banan", null)]
    [InlineData("42", "42 + 1 = 43")]
    public void SelectManyTheory(string? maybeS, string? expected)

        => Assert.Equal(
            expected: FromReference(expected),
            actual: from s in FromReference(maybeS)
                    from i in ParseInt(s)
                    select $"{s} + 1 = {i + 1}");

    [Theory]
    [InlineData(null, null)]
    [InlineData(42, "42")]
    public async Task TraverseTheory(int? maybeI, string? expected)

        => Assert.Equal(
            expected: FromReference(expected),
            actual: await FromNullable(maybeI).Traverse(i => Task.FromResult(i.ToString())));

    [Theory]
    [InlineData(null, 0)]
    [InlineData(42, 42)]
    public void TryTheory(int? maybe, int expected)

        => Assert.Equal(expected, FromNullable(maybe).Try(out var value) ? value : 0);

    [Theory]
    [InlineData(null, null)]
    [InlineData(42, 42)]
    [InlineData(43, null)]
    public void WhereTheory(int? maybe, int? expected)

        => Assert.Equal(FromNullable(expected), FromNullable(maybe).Where(i => i % 2 == 0));

    [Fact]
    public void FromNullableNullNone() => Assert.Equal(None<int>(), FromNullable<int>(null));

    [Fact]
    public void FromNullableNotNullSome() => Assert.Equal(Some(42), FromNullable<int>(42));

    [Fact]
    public void FromReferenceNullNone() => Assert.Equal(None<string>(), FromReference<string>(null));

    [Fact]
    public void FromReferenceNotNullSome() => Assert.Equal(Some("banan"), FromReference("banan"));

    [Fact]
    public void ToNullableNoneNull() => Assert.Null(None<int>().ToNullable());

    [Fact]
    public void ToNullableSomeNotNull() => Assert.NotNull(Some(42).ToNullable());

    [Fact]
    public void ToReferenceNoneNull() => Assert.Null(None<string>().ToReference());

    [Fact]
    public void ToReferenceSomeNotNull() => Assert.NotNull(Some("banan").ToReference());

    [Fact]
    public void MaybeSingleEmptyNone() => Assert.Equal(None<int>(), Array.Empty<int>().MaybeSingle());

    [Fact]
    public void MaybeSingleSingletonSome() => Assert.Equal(Some(42), new[] { 42 }.MaybeSingle());

    [Fact]
    public void MaybeSingleManyThrows()

        => Assert.Throws<InvalidOperationException>(() => new[] { 41, 42 }.MaybeSingle());

    [Fact]
    public void MaybeFirstEmptyNone() => Assert.Equal(None<int>(), Array.Empty<int>().MaybeFirst());

    [Fact]
    public void MaybeFirstSingletonSome() => Assert.Equal(Some(42), new[] { 42 }.MaybeFirst());

    [Fact]
    public void MaybeFirstManySome() => Assert.Equal(Some(41), new[] { 41, 42 }.MaybeFirst());

    [Fact]
    public void JsonSerializeNoneNull() => Assert.Equal("null", JsonSerializer.Serialize(None<int>()));

    [Fact]
    public void JsonSerializeSomeNumber() => Assert.Equal("42", JsonSerializer.Serialize(Some(42)));

    [Fact]
    public void JsonSerializeNonePropertyNull() =>
        Assert.Equal("{\"Id\":null}", JsonSerializer.Serialize(new { Id = None<int>() }));

    [Fact]
    public void JsonSerializeSomePropertyNumber() =>
        Assert.Equal("{\"Id\":42}", JsonSerializer.Serialize(new { Id = Some(42) }));

    static readonly JsonSerializerOptions OptionsWithDefaultIgnoreCondition =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

    [Fact]
    public void JsonSerializeNoneIgnoreWhenWritingDefault() =>
        Assert.Equal("{}", JsonSerializer.Serialize(new { Id = None<int>() }, OptionsWithDefaultIgnoreCondition));

    [Fact]
    public void JsonSerializeArrayOfMaybe() =>
        Assert.Equal("[42,null]", JsonSerializer.Serialize(new[] { Some(42), None<int>() }));

    [Fact]
    public void JsonSerializeMaybeArray() =>
        Assert.Equal("[41,42]", JsonSerializer.Serialize(Some<int[]>([41, 42])));

    [Fact]
    public void JsonDeserializeNullNone() =>
        Assert.Equal(None<int>(), JsonSerializer.Deserialize<Maybe<int>>("null"));

    [Fact]
    public void JsonDeserializeNumberSome() =>
        Assert.Equal(Some(42), JsonSerializer.Deserialize<Maybe<int>>("42"));

    class DsTarget
    {
        public Maybe<int> Id { get; init; } = None<int>();
    }

    [Fact]
    public void JsonDeserializeNullPropertyNone() =>
        Assert.True(JsonSerializer.Deserialize<DsTarget>("{\"Id\":null}") is { Id: [] });

    [Fact]
    public void JsonDeserializeNumberPropertySome() =>
        Assert.True(JsonSerializer.Deserialize<DsTarget>("{\"Id\":42}") is { Id: [42] });

    [Fact]
    public void JsonDeserializeMissingPropertyNone() =>
        Assert.True(JsonSerializer.Deserialize<DsTarget>("{}") is { Id: [] });

    [Fact]
    public void JsonDeserializeArrayOfMaybe() =>
        Assert.Equal(new[] { Some(42), None<int>() }, JsonSerializer.Deserialize<Maybe<int>[]>("[42,null]"));

    [Fact]
    public void JsonDeserializeMaybeArray() =>
        Assert.True(JsonSerializer.Deserialize<Maybe<int[]>>("[41,42]") is [[41, 42]]);
}
