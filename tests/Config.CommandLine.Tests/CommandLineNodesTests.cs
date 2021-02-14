using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Xunit;

namespace Luger.Configuration.CommandLine.Tests
{
    public class CommandLineNodesTests
    {
        private static CommandLineNodes.SetKeyValue SetKeyValue(IList<(string, string)> kvps) => (key, value) => kvps.Add((key, value));

        [Fact]
        public void CollectOptionNodeTest()
        {
            // Arrange
            var optionNode = new OptionNode("option", "value");

            var actual = new List<(string, string)>();
            var expected = new[] { ("prefix:option", "value") };

            // Act
            optionNode.Collect("prefix", SetKeyValue(actual));

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectOptionNodeNoArgumentTest()
        {
            // Arrange
            var optionNode = new OptionNode("option");

            var actual = new List<(string, string)>();
            var expected = new[] { ("prefix:option", bool.TrueString) };

            // Act
            optionNode.Collect("prefix", SetKeyValue(actual));

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectOptionsTest()
        {
            // Arrange
            var optionNames = new[] { "option1", "option2" };

            var actual = new List<(string, string)>();
            var setKeyValue = SetKeyValue(actual);
            var expected = from name in optionNames select ($"prefix:{name}", bool.TrueString);

            // Act
            optionNames
                .Select(name => new OptionNode(name))
                .Collect("prefix", setKeyValue);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectVerbNodeTest()
        {
            // Arrange
            var verbNode = new VerbNode(
                Name: "verbName",
                Options: ImmutableList.Create(new OptionNode("optionName")),
                Verbs: ImmutableList.Create<VerbNode>(),
                Arguments: ImmutableList.Create(new ArgumentNode("argumentName", "argumentValue")));

            var actual = new List<(string, string)>();
            var expected = new[]
            {
                ("prefix:verbName:optionName", bool.TrueString),
                ("prefix:verbName:argumentName", "argumentValue")
            };

            // Act
            verbNode.Collect("prefix", SetKeyValue(actual));

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectVerbNodeNoContent()
        {
            // Arrange
            var verbNode = new VerbNode(
                Name: "verbName",
                Options: ImmutableList.Create<OptionNode>(),
                Verbs: ImmutableList.Create<VerbNode>(),
                Arguments: ImmutableList.Create<ArgumentNode>());

            var actual = new List<(string, string)>();
            var expected = new[] { ("prefix:verbName", bool.TrueString) };

            // Act
            verbNode.Collect("prefix", SetKeyValue(actual));

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectVerbsTest()
        {
            // Arrange
            var verbNames = new[] { "verb1", "verb2" };

            var actual = new List<(string, string)>();
            var setKeyValue = SetKeyValue(actual);
            var expected = from name in verbNames select ($"prefix:{name}", bool.TrueString);

            // Act
            verbNames
                .Select(name => new VerbNode(
                    Name: name,
                    Options: ImmutableList.Create<OptionNode>(),
                    Verbs: ImmutableList.Create<VerbNode>(),
                    Arguments: ImmutableList.Create<ArgumentNode>()))
                .Collect("prefix", setKeyValue);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectArgumentTest()
        {
            // Arrange
            var argumentNode = new ArgumentNode("argumentName", "value");

            var actual = new List<(string, string)>();
            var expected = new[] { ("prefix:argumentName", "value") };

            // Act
            argumentNode.Collect("prefix", SetKeyValue(actual));

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectArgumentsTest()
        {
            // Arrange
            var argumentNodes = new ArgumentNode[]
            {
                new("argument1", "value1"),
                new("argument2", "value2")
            };

            var actual = new List<(string, string)>();
            var setKeyValue = SetKeyValue(actual);
            var expected = from node in argumentNodes select ($"prefix:{node.Name}", node.Value);

            // Act
            argumentNodes.Collect("prefix", setKeyValue);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectCommandLineTest()
        {
            // Arrange
            var commandLineNode = new CommandLineNode(
                Options: ImmutableList.Create(new OptionNode("optionName")),
                Verbs: ImmutableList.Create<VerbNode>(),
                Arguments: ImmutableList.Create(new ArgumentNode("argumentName", "argumentValue")));

            var actual = new List<(string, string)>();
            var expected = new[]
            {
                ("prefix:optionName", bool.TrueString),
                ("prefix:argumentName", "argumentValue")
            };

            // Act
            commandLineNode.Collect("prefix", SetKeyValue(actual));

            // Assert
            Assert.Equal(expected, actual);
        }

    }
}
