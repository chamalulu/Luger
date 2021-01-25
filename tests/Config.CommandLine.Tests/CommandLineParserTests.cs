using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Xunit;

namespace Luger.Extensions.Configuration.CommandLine.Tests
{
    public class ParseStateTests
    {
        private record TestTokenA() : TokenBase("A", ..);
        private record TestTokenB() : TokenBase("B", ..);

        [Fact]
        public void AcceptEmptyTest()
        {
            var target = new ParseState(ImmutableQueue.Create<TokenBase>());
            var (state, token) = target.Accept<TokenBase>();

            Assert.Same(target, state);
            Assert.Null(token);
        }

        [Fact]
        public void AcceptTypeMismatchTest()
        {
            var target = new ParseState(ImmutableQueue.Create<TokenBase>(new TestTokenB()));
            var (state, token) = target.Accept<TestTokenA>();

            Assert.Same(target, state);
            Assert.Null(token);
        }

        [Fact]
        public void AcceptPredicateFalseTest()
        {
            var target = new ParseState(ImmutableQueue.Create<TokenBase>(new TestTokenB()));
            var (state, token) = target.Accept<TestTokenB>(t => t.Arg == "A");

            Assert.Same(target, state);
            Assert.Null(token);
        }

        [Fact]
        public void AcceptMatchTest()
        {
            var target = new ParseState(ImmutableQueue.Create<TokenBase>(new TestTokenB()));
            var (state, token) = target.Accept<TestTokenB>(t => t.Arg == "B");

            Assert.Empty(state.Tokens);
            Assert.NotNull(token);
        }
    }

    public class CommandLineParserTests
    {
        [Fact]
        public void AlternationOperatorTest()
        {
            // Arrange
            var p1 = CommandLineParser.True(1);
            var p2 = CommandLineParser.True(2);
            var expectedSuccesses = new[] { (1, ParseState.Empty), (2, ParseState.Empty) };
            var expectedFailures = Enumerable.Empty<(string, ParseState)>();

            // Act
            var actual = p1 | p2;

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expectedSuccesses, result.Successes);
            Assert.Equal(expectedFailures, result.Failures);
        }

        [Fact]
        public void ConsOperatorTest()
        {
            // Arrange
            var left = CommandLineParser.True(1);
            var right = CommandLineParser.True(2);
            var expected = ParseResult.Success(ImmutableList.Create(1, 2), ParseState.Empty);

            // Act
            var actual = left & right;

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void AppendOperatorTest()
        {
            // Arrange
            var acc = CommandLineParser.True(ImmutableList.Create<int>());
            var p = CommandLineParser.True(1);
            var expected = ParseResult.Success(ImmutableList.Create(1), ParseState.Empty);

            // Act
            var actual = acc & p;

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void TrueTest()
        {
            // Arrange
            var expected = ParseResult.Success(0, ParseState.Empty);

            // Act
            var actual = CommandLineParser.True(0);

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void FalseTest()
        {
            // Arrange
            var expected = ParseResult.Failure<int>("message", ParseState.Empty);

            // Act
            var actual = CommandLineParser.False<int>("message");

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void OrTest()
        {
            // Arrange
            var target = CommandLineParser.True(1);
            var alternative = CommandLineParser.True(2);
            var expectedSuccesses = new[] { (1, ParseState.Empty), (2, ParseState.Empty) };
            var expectedFailures = Enumerable.Empty<(string, ParseState)>();

            // Act
            var actual = target.Or(alternative);

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expectedSuccesses, result.Successes);
            Assert.Equal(expectedFailures, result.Failures);
        }

        [Fact]
        public void AndTest()
        {
            // Arrange
            var left = CommandLineParser.True(1);
            var right = CommandLineParser.True(2);
            var expected = ParseResult.Success((1, 2), ParseState.Empty);

            // Act
            var actual = left.And(right);

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void AnyTest()
        {
            // Assert
            var source = new[] { 1, 2, 3 };
            var target = from s in source select CommandLineParser.True(s);
            var expectedSuccesses = from s in source select (s, ParseState.Empty);
            var expectedFailures = Enumerable.Empty<(string, ParseState)>();

            // Act
            var actual = target.Any();

            // Arrange
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expectedSuccesses, result.Successes);
            Assert.Equal(expectedFailures, result.Failures);
        }

        [Fact]
        public void AllTest()
        {
            // Arrange
            var source = new[] { 1, 2, 3 };
            var target = from s in source select CommandLineParser.True(s);
            var expected = ParseResult.Success(ImmutableList.Create(source), ParseState.Empty);

            // Act
            var actual = target.All();

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void SelectTest()
        {
            // Arrange
            var target = CommandLineParser.True(1);
            var expected = ParseResult.Success("1", ParseState.Empty);

            // Act
            var actual = from s in target select s.ToString();

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        // Next test became a bit convoluted but I really wanted to exercise more than one successful parsing.

        private record CharToken(char C) : TokenBase(string.Empty, ..);

        private static CommandLineParser<string> CharParser(char c) =>

            new CommandLineParser<string>(state =>
                state.Tokens.Any() && state.Tokens.PeekRef() is CharToken tt && tt.C == c
                    ? ParseResult.Success(c.ToString(), state with { Tokens = state.Tokens.Dequeue() })
                    : ParseResult.Failure<string>($"Expected '{c}'", state));

        [Fact]
        public void SelectManyTest()
        {
            // Arrange
            var a_Parser = CharParser('a');
            var b_Parser = CharParser('b');
            var c_Parser = CharParser('c');
            var ab_Parser = from a in a_Parser from b in b_Parser select a + b;
            var bc_Parser = from b in b_Parser from c in c_Parser select b + c;
            var a_Or_ab_Parser = a_Parser | ab_Parser;
            var bc_Or_c_Parser = bc_Parser | c_Parser;

            var tokens = ImmutableQueue.CreateRange<TokenBase>("abc".Select(c => new CharToken(c)));
            var state = new ParseState(tokens);
            var expectedSuccesses = new[] { (("a", "bc"), ParseState.Empty), (("ab", "c"), ParseState.Empty) };
            var expectedFailures = Enumerable.Empty<(string, ParseState)>();

            // Act
            var actual = from a_Or_ab in a_Or_ab_Parser
                         from bc_Or_c in bc_Or_c_Parser
                         select (a_Or_ab, bc_Or_c);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expectedSuccesses, result.Successes);
            Assert.Equal(expectedFailures, result.Failures);
        }

        [Fact]
        public void ZeroOrMoreTest()
        {
            // Arrange
            var a_Parser = CharParser('a');
            var tokens = ImmutableQueue.CreateRange<TokenBase>("aaa".Select(c => new CharToken(c)));
            var state = new ParseState(tokens);
            var expected = ParseResult.Success(ImmutableList.Create("a", "a", "a"), ParseState.Empty);

            // Act
            var actual = a_Parser.ZeroOrMore();

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void ArgumentParserTest()
        {
            // Arrange
            var argumentSpecification = new ArgumentSpecification("Name");
            var tokens = ImmutableQueue.Create<TokenBase>(new ArgumentToken("Option=Value", 7));
            var state = new ParseState(tokens);
            var expected = ParseResult.Success(new ArgumentNode("Name", "Value"), ParseState.Empty);

            // Act
            var actual = CommandLineParser.ArgumentParser(argumentSpecification);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void AnonymousArgumentParserTest()
        {
            // Arrange
            var tokens = ImmutableQueue.Create<TokenBase>(new ArgumentToken("Value", 0));
            var state = new ParseState(tokens);
            var expected = ParseResult.Success("Value", ParseState.Empty);

            // Act
            var actual = CommandLineParser.AnonymousArgumentParser;

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void ArgumentListParserTest()
        {
            // Arrange
            var source = new[] { 1, 2, 3 }.Select(i => (name: $"Arg{i}", value: $"Value{i}")).ToArray();
            var argumentSpecifications = source.Select(nvp => new ArgumentSpecification(nvp.name));
            var tokens = ImmutableQueue.CreateRange<TokenBase>(source.Select(nvp => new ArgumentToken(nvp.value, 0)));
            var state = new ParseState(tokens);
            var expected = ParseResult.Success(
                ImmutableList.CreateRange(source.Select(nvp => new ArgumentNode(nvp.name, nvp.value))),
                ParseState.Empty);

            // Act
            var actual = CommandLineParser.ArgumentListParser(argumentSpecifications);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        public static IEnumerable<object[]> OptionParserTestData => new (OptionSpecificationBase spec, TokenBase[] tokens, OptionNode[] nodes, TokenBase[] remaining)[]
        {
            (new ShortOptionSpecification("Name", 'o', false),
             new[] { new ShortOptionToken("-o", 1) },
             new[] { new OptionNode("Name") },
             Array.Empty<TokenBase>()),
            (new LongOptionSpecification("Name", "option", false),
             new[] { new LongOptionToken("--option", 2..) },
             new[] { new OptionNode("Name") },
             Array.Empty<TokenBase>()),
            (new ShortOptionSpecification("Name", 'o', true),
             new TokenBase[] { new ShortOptionToken("-o", 1), new ArgumentToken("Argument", 0) },
             new[] { new OptionNode("Name", "Argument") },
             Array.Empty<TokenBase>()),
            (new LongOptionSpecification("Name", "option", true),
             new TokenBase[] { new LongOptionToken("--option", 2..), new ArgumentToken("Argument", 0) },
             new[] { new OptionNode("Name", "Argument") },
             Array.Empty<TokenBase>()),
        }.Select(data => new object[]
        {
            data.spec,
            new ParseState(ImmutableQueue.CreateRange(data.tokens)),
            new ParseResult<OptionNode>(
                ImmutableList.CreateRange(data.nodes.Select(n => (n, new ParseState(ImmutableQueue.CreateRange(data.remaining))))),
                ImmutableList.Create<(string, ParseState)>())
        });

        [Theory]
        [MemberData(nameof(OptionParserTestData))]
        public void OptionParserTest(OptionSpecificationBase optionSpecification, ParseState state, ParseResult<OptionNode> expected)
        {
            // Arrange

            // Act
            var actual = CommandLineParser.OptionParser(optionSpecification);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void OptionSetParserTest()
        {
            // Arrange
            var source = "abddf";
            var optionSpecifications = "abcdef".Select(c => new ShortOptionSpecification($"Name_{c}", c, false));
            var tokens = ImmutableQueue.CreateRange<TokenBase>(source.Select((c, i) => new ShortOptionToken(source, i)));
            var state = new ParseState(tokens);
            var expected = ParseResult.Success(
                ImmutableList.CreateRange(source.Select(c => new OptionNode($"Name_{c}"))),
                ParseState.Empty);

            // Act
            var actual = CommandLineParser.OptionSetParser(optionSpecifications);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }
    }
}