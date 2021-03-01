using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Luger.Configuration.CommandLine.Tests
{
    public class CommandLineConfigurationTests
    {
        public static IEnumerable<object[]> AddCommandLineConfigurationSpecTestData => new object[][]
        {
            new object[]    // Empty case
            {
                Array.Empty<string>(),
                CommandLineSpecification.Empty,
                new Dictionary<string, string>()
            },
            new object[]    // Flags and argument case
            {
                new []{ "-ab", "--cflag", "val", "arg" },
                new CommandLineSpecification()
                    .AddFlag(new("Aflag", "aflag", 'a', false))
                    .AddFlag(new("Bflag", "bflag", 'b', false))
                    .AddFlag(new("Cflag", "cflag", 'c', true))
                    .AddArgument(new("Argument")),
                new Dictionary<string, string>
                {
                    ["Aflag"] = "True",
                    ["Bflag"] = "True",
                    ["Cflag"] = "val",
                    ["Argument"] = "arg"
                }
            },
            new object[]    // Nested verbs case
            {
                new []{ "noun", "verb", "arg" },
                new CommandLineSpecification()
                    .AddVerb(new("Noun", new CommandLineSpecification()
                        .AddVerb(new("Verb", new CommandLineSpecification()
                            .AddArgument(new("Argument")))))),
                new Dictionary<string, string>
                {
                    ["Noun:Verb:Argument"] = "arg"
                }
            }
        };

        [Theory]
        [MemberData(nameof(AddCommandLineConfigurationSpecTestData))]
        public void AddCommandLineConfigurationSpecTest(
            string[] args,
            CommandLineSpecification commandLineSpecification,
            Dictionary<string, string> expected)
        {
            // Arrange
            var target = new ConfigurationBuilder();

            // Act
            var actual = target.AddCommandLineConfiguration(args, commandLineSpecification).Build();

            // Assert
            Assert.All(expected.Keys, key => Assert.Equal(expected[key], actual[key]));
        }
    }
}
