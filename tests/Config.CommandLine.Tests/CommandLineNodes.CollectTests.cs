using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Luger.Configuration.CommandLine.Tests
{
    public class CommandLineNodesCollectTests
    {
        private record TestNode(string Name) : NamedNode(Name);

        [Fact]
        public void CollectListNodeTest()
        {
            // Arrange
            var names = new[] { "Name1", "Name2" };
            var nodes = from name in names select new TestNode(name);
            var list = ImmutableList.CreateRange<NamedNode>(nodes);
            var target = new ListNode<NamedNode>(list);
            var path = ImmutableList<string>.Empty;
            var expected = ImmutableList.CreateRange(from name in names select (name, bool.TrueString));

            // Act
            var actual = target.Collect(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectSetNodeTest()
        {
            // Arrange
            var names = new[] { "Name1", "Name2" };
            var nodes = from name in names select new TestNode(name);
            var set = ImmutableHashSet.CreateRange<NamedNode>(nodes);
            var target = new SetNode<NamedNode>(set);
            var path = ImmutableList<string>.Empty;
            var expected = ImmutableHashSet.CreateRange(from name in names select (name, bool.TrueString));

            // Act
            var actual = target.Collect(path);

            // Assert
            Assert.True(expected.SetEquals(actual));
        }

        [Theory]
        [InlineData(new string[0], "name")]
        [InlineData(new[] { "path" }, "path:name")]
        [InlineData(new[] { "s1", "s2" }, "s1:s2:name")]
        public void CollectNamedNodeTest(string[] pathSegments, string expected)
        {
            // Arrange
            var target = new TestNode("name");
            var path = ImmutableList.Create(pathSegments);

            // Act
            var actual = target.Collect(path);

            // Assert
            Assert.Equal(new[] { (expected, bool.TrueString) }, actual);
        }

        [Fact]
        public void CollectFlagNodeTest()
        {
            // Arrange
            var target = new FlagNode("name");
            var path = ImmutableList<string>.Empty;
            var expected = new[] { ("name", bool.TrueString) };

            // Act
            var actual = target.Collect(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectFlagWithValueTest()
        {
            // Arrange
            var target = new FlagNodeWithValue("name", "value");
            var path = ImmutableList<string>.Empty;
            var expected = new[] { ("name", "value") };

            // Act
            var actual = target.Collect(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollectArgumentNodeTest()
        {
            // Arrange
            var target = new ArgumentNode("name", "value");
            var path = ImmutableList<string>.Empty;
            var expected = new[] { ("name", "value") };

            // Act
            var actual = target.Collect(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> CollectVerbNodeTestData => new object[][]
        {
            new object[]
            {
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { ("name", bool.TrueString)  }
            },
            new object[]
            {
                Array.Empty<string>(),
                new[] { "path" },
                new[] { ("path:name", bool.TrueString)  }
            },
            new object[]
            {
                new[] { "flag" },
                Array.Empty<string>(),
                new[] { ("name:flag", bool.TrueString)  }
            },
            new object[]
            {
                new[] { "flag" },
                new[] { "path" },
                new[] { ("path:name:flag", bool.TrueString)  }
            },
        };

        [Theory]
        [MemberData(nameof(CollectVerbNodeTestData))]
        public void CollectVerbNodeTest(string[] flagNames, string[] pathSegments, (string, string)[] expected)
        {
            // Arrange
            var flagSeq = from name in flagNames select new FlagNode(name);
            var flagList = ImmutableList.CreateRange(flagSeq);
            var flagListNode = new ListNode<FlagNode>(flagList);
            var target = new VerbNode("name", flagListNode);
            var path = ImmutableList.Create(pathSegments);

            // Act
            var actual = target.Collect(path);

            // Assert
            Assert.Equal(expected, actual);
        }

        // TODO: Tests for VerbNodeWithVerb
        // TODO: Tests for VerbNodeWithArguments
        // TODO: Tests for CommandLineNode
        // TODO: Tests for CommandLineNodeWithVerb
        // TODO: Tests for CommandLineNodeWithArguments
    }
}
