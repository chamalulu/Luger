using System;
using System.Collections.Generic;

using Luger.Configuration.CommandLine.Specifications;

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
                new []{ "-ab", "--cflag", "Cval", "arg" },
                new CommandLineSpecification()
                    .AddFlagWithValue("Aflag", "aflag", 'a')
                    .AddFlagWithValue("Bflag", "bflag", 'b', "Bval")
                    .AddFlagWithArgument("Cflag", "cflag", 'c')
                    .AddArgument(new("Argument")),
                new Dictionary<string, string>
                {
                    ["Aflag"] = "True",
                    ["Bflag"] = "Bval",
                    ["Cflag"] = "Cval",
                    ["Argument"] = "arg"
                }
            },
            new object[]    // Nested verbs case
            {
                new []{ "noun", "verb", "arg" },
                new CommandLineSpecification().AddVerb("Noun", clsb => clsb.AddVerb("Verb", clsb => clsb.AddArgument("Argument"))),
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
