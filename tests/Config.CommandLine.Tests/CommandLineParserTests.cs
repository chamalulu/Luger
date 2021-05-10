using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Luger.Configuration.CommandLine.Specifications;

using Xunit;

namespace Luger.Configuration.CommandLine.Tests
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

        private static CommandLineParser<string> CharParser(char c)

            => new(state

                => state.Tokens.Any() && state.Tokens.PeekRef() is CharToken tt && tt.C == c
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
            var argumentSpecification = new ArgumentSpecification(new("Name"));
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

        [Theory]
        [InlineData("literal", true, "literal")]
        [InlineData("Literal", true, "Literal")]
        [InlineData("litter", true, null)]
        [InlineData("literal", false, "literal")]
        [InlineData("Literal", false, null)]
        [InlineData("litter", false, null)]
        public void LiteralArgumentParserTest(string arg, bool ignoreCase, string expectedValue)
        {
            // Arrange
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var tokens = ImmutableQueue.Create<TokenBase>(new ArgumentToken(arg, Index.Start));
            var state = new ParseState(tokens);
            var expected = expectedValue is string
                ? ParseResult.Success(expectedValue, ParseState.Empty)
                : ParseResult.Failure<string>("Expected Argument 'literal'", state);

            // Act
            var actual = CommandLineParser.LiteralArgumentParser("literal", comparison);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void MultiArgumentParserTest()
        {
            // Arrange
            var multiArgumentSpecification = new MultiArgumentSpecification(new("MultiArgument"));
            var tokens = ImmutableQueue.Create<TokenBase>(new ArgumentToken("banan", 0));
            var state = new ParseState(tokens);
            var item = new MultiArgumentNode("MultiArgument", 0, "banan");
            var expected = ParseResult.Success<ListNode<MultiArgumentNode>>(new(ImmutableList.Create(item)), ParseState.Empty);

            // Act
            var actual = CommandLineParser.MultiArgumentParser(multiArgumentSpecification);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void ArgumentsParserTest()
        {
            // Arrange
            var source = new[] { 1, 2, 3 }.Select(i => (name: $"Arg{i}", value: $"Value{i}")).ToArray();
            var argumentSpecifications = ImmutableList.CreateRange(source.Select(nvp => new ArgumentSpecification(new(nvp.name))));
            var tokens = ImmutableQueue.CreateRange<TokenBase>(source.Select(nvp => new ArgumentToken(nvp.value, 0)));
            var state = new ParseState(tokens);
            var expected = ParseResult.Success(new ListNode<ArgumentNode>(
                ImmutableList.CreateRange(source.Select(nvp => new ArgumentNode(nvp.name, nvp.value)))),
                ParseState.Empty);

            // Act
            var actual = CommandLineParser.ArgumentsParser(argumentSpecifications);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        public static IEnumerable<object[]> FlagParserTestData => new (FlagSpecification spec, TokenBase[] tokens, FlagNode[] nodes, TokenBase[] remaining)[]
        {
            (new FlagWithValueSpecification(new("Name"), new("flag"), new('f')),
             new[] { new ShortFlagToken("-f", 1) },
             new[] { new FlagNode("Name") },
             Array.Empty<TokenBase>()),
            (new FlagWithValueSpecification(new("Name"), new("flag"), new('f')),
             new[] { new LongFlagToken("--flag", 2..) },
             new[] { new FlagNode("Name") },
             Array.Empty<TokenBase>()),
            (new FlagWithArgumentSpecification(new("Name"), new("flag"), new('f')),
             new TokenBase[] { new ShortFlagToken("-f", 1), new ArgumentToken("arg", 0) },
             new[] { new FlagNode("Name", "arg") },
             Array.Empty<TokenBase>()),
            (new FlagWithArgumentSpecification(new("Name"), new("flag"), new('f')),
             new TokenBase[] { new LongFlagToken("--flag", 2..), new ArgumentToken("arg", 0) },
             new[] { new FlagNode("Name", "arg") },
             Array.Empty<TokenBase>()),
        }.Select(data => new object[]
        {
            data.spec,
            new ParseState(ImmutableQueue.CreateRange(data.tokens)),
            new ParseResult<FlagNode>(
                ImmutableList.CreateRange(data.nodes.Select(n => (n, new ParseState(ImmutableQueue.CreateRange(data.remaining))))),
                ImmutableList.Create<(string, ParseState)>())
        });

        [Theory]
        [MemberData(nameof(FlagParserTestData))]
        public void FlagParserTest(FlagSpecification flagSpecification, ParseState state, ParseResult<FlagNode> expected)
        {
            // Arrange

            // Act
            var actual = CommandLineParser.FlagParser(flagSpecification);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void FlagsParserTest()
        {
            // Arrange
            var source = "abddf";
            var flagSpecifications = "abcdef".Select(c => new FlagWithValueSpecification(new($"Name_{c}"), new($"name-{c}"), new(c)));
            var tokens = ImmutableQueue.CreateRange<TokenBase>(source.Select((c, i) => new ShortFlagToken(source, i)));
            var state = new ParseState(tokens);
            var expected = ParseResult.Success(new ListNode<FlagNode>(
                ImmutableList.CreateRange(source.Select(c => new FlagNode($"Name_{c}")))),
                ParseState.Empty);

            // Act
            var actual = CommandLineParser.FlagsParser(flagSpecifications);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void VerbParserTest()
        {
            // Arrange
            var verbSpecification = new VerbSpecification(new("verb"), CommandLineSpecification.Empty);

            var tokens = ImmutableQueue.Create<TokenBase>(new ArgumentToken("verb", Index.Start));
            var state = new ParseState(tokens);

            var value = new VerbNode("verb", ListNode<FlagNode>.Empty);

            var expected = ParseResult.Success(value, ParseState.Empty);

            // Act
            var actual = CommandLineParser.VerbParser(verbSpecification);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Theory]
        [InlineData(new string[0], null)]
        [InlineData(new[] { "verb" }, "verb")]
        public void VerbsParserTest(string[] args, string verb)
        {
            // Arrange
            var verbSpecifications = ImmutableList.Create(new VerbSpecification(new("verb"), CommandLineSpecification.Empty));

            var tokens = ImmutableQueue.CreateRange<TokenBase>(from arg in args select new ArgumentToken(arg, Index.Start));

            var state = new ParseState(tokens);

            var expected = verb is null
                ? ParseResult.Success(ListNode<VerbNode>.Empty, ParseState.Empty)
                : new ParseResult<ListNode<VerbNode>>(ImmutableList.Create(
                    (new ListNode<VerbNode>(ImmutableList.Create(new VerbNode(verb, ListNode<FlagNode>.Empty))), ParseState.Empty),
                    (ListNode<VerbNode>.Empty, state)), ImmutableList<(string, ParseState)>.Empty);

            // Act
            var actual = CommandLineParser.VerbsParser(verbSpecifications);

            // Assert
            var result = actual.Parse(state);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }

        [Fact]
        public void CommandLineParserTest()
        {
            // Arrange
            var expected = ParseResult.Success(new CommandLineNode(ListNode<FlagNode>.Empty), ParseState.Empty);

            // Act
            var actual = CommandLineParser.Create(CommandLineSpecification.Empty);

            // Assert
            var result = actual.Parse(ParseState.Empty);
            Assert.Equal(expected.Successes, result.Successes);
            Assert.Equal(expected.Failures, result.Failures);
        }
    }
}
