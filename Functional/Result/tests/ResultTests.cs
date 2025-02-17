using System;

using Xunit;

using static Luger.Functional.Result;

namespace Luger.Functional.Tests;

public class ResultTests
{
    [Fact]
    public void GetHashCodeOkNull() => Assert.Equal(0, Ok<string, object?>(null).GetHashCode());

    [Fact]
    public void GetHashCodeOkNotNull() => Assert.Equal(42.GetHashCode(), Ok<string, int>(42).GetHashCode());

    [Fact]
    public void GetHashCodeError() => Assert.Equal("banan".GetHashCode(), Error<string, int>("banan").GetHashCode());

    [Fact]
    public void MatchOk() => Assert.Equal(43, Ok<string, int>(42).Match(int.Parse, i => i + 1));

    [Fact]
    public void MatchError() => Assert.Equal(42, Error<string, int>("42").Match(int.Parse, i => i + 1));

    [Fact]
    public void MapOk() => Assert.Equal(Ok<string, int>(43), Ok<string, int>(42).Map(i => i + 1));

    [Fact]
    public void MapError() => Assert.Equal(Error<string, int>("banan"), Error<string, int>("banan").Map(i => i + 1));

    [Fact]
    public void MapErrorOk()

        => Assert.Equal(Ok<string, int>(42), Ok<string, int>(42).MapError(e => e.ToUpperInvariant()));

    [Fact]
    public void MapErrorError()

        => Assert.Equal(Error<string, int>("BANAN"), Error<string, int>("banan").MapError(e => e.ToUpperInvariant()));

    static Result<string, int> ParseInt(string s) => int.TryParse(s, out var i) ? i : "banan";

    [Fact]
    public void BindOkOk() => Assert.Equal(Ok<string, int>(42), Ok<string, string>("42").Bind(ParseInt));

    [Fact]
    public void BindOkError() => Assert.Equal(Error<string, int>("banan"), Ok<string, string>("citron").Bind(ParseInt));

    [Fact]
    public void BindErrorError()

        => Assert.Equal(Error<string, int>("citron"), Error<string, string>("citron").Bind(ParseInt));

    [Fact]
    public void OpImplicitOk()
    {
        Result<string, int> expected = Ok<string, int>(42), actual = 42;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OpImplicitError()
    {
        Result<string, int> expected = Error<string, int>("banan"), actual = "banan";
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EqualsOkNullOkNullTrue() => Assert.True(Ok<string, object?>(null).Equals(Ok<string, object?>(null)));

    [Fact]
    public void EqualsOkNullOkObjFalse()

        => Assert.False(Ok<string, object?>(null).Equals(Ok<string, object?>(new object())));

    [Fact]
    public void EqualsOkObjOkObjFalse()

        => Assert.False(Ok<string, object?>(new object()).Equals(Ok<string, object?>(new object())));

    [Fact]
    public void EqualsOkBananErrorBananFalse()

        => Assert.False(Ok<string, string>("banan").Equals(Error<string, string>("banan")));

    [Fact]
    public void EqualsOk42Ok42True()

        => Assert.True(Ok<string, int>(42).Equals(Ok<string, int>(42)));

    [Fact]
    public void EqualsOk42Ok43False()

        => Assert.False(Ok<string, int>(42).Equals(Ok<string, int>(43)));

    [Fact]
    public void EqualsErrorBananErrorBananTrue()

        => Assert.True(Error<string, int>("banan").Equals(Error<string, int>("banan")));

    [Fact]
    public void EqualsErrorBananErrorCitronFalse()

        => Assert.False(Error<string, int>("banan").Equals(Error<string, int>("citron")));

    class FormattableBox<T>(T value) : IFormattable where T : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider)

            => value.ToString(format, formatProvider);

        public override string? ToString() => value.ToString();
    }

    [Theory]
    [InlineData(null, null, null, "Ok")]
    [InlineData(1000, "D", null, "Ok\u00a01000")]
    [InlineData(1000, "N2", "en-US", "Ok\u00a01,000.00")]
    [InlineData(1000, "N2", "sv-SE", "Ok\u00a01\u00a0000,00")]
    public void ToStringOkTheory(int? value, string? format, string? cultureName, string expected)
    {
        var result = value.HasValue
            ? Ok<string, FormattableBox<int>?>(new FormattableBox<int>(value.Value))
            : Ok<string, FormattableBox<int>?>(null);

        var formatProvider = cultureName is not null
            ? System.Globalization.CultureInfo.GetCultureInfo(cultureName)
            : null;

        var actual = result.ToString(format, formatProvider);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1000, "D", null, "Error\u00a01000")]
    [InlineData(1000, "N2", "en-US", "Error\u00a01,000.00")]
    [InlineData(1000, "N2", "sv-SE", "Error\u00a01\u00a0000,00")]
    public void ToStringErrorTheory(int value, string? format, string? cultureName, string expected)
    {
        var result = Error<FormattableBox<int>, int>(new FormattableBox<int>(value));

        var formatProvider = cultureName is not null
            ? System.Globalization.CultureInfo.GetCultureInfo(cultureName)
            : null;

        var actual = result.ToString(format, formatProvider);

        Assert.Equal(expected, actual);
    }

    // TODO: Use property tests for Equals(object?)
}
