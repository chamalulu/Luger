using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Luger.Configuration.CommandLine.Tests
{
    public class CommandLineConfigurationTests
    {
        public static IEnumerable<object[]> TestData => new object[][]
        {
            new object[]    // Empty case
            {
                Array.Empty<string>(),
                CommandLineSpecification.Empty,
                new Dictionary<string, string>()
            },
            new object[]    // Options and argument case
            {
                new []{ "-ab", "--copt", "val", "arg" },
                new CommandLineSpecification(
                    Options: ImmutableHashSet.Create<OptionSpecificationBase>(
                        new ShortOptionSpecification("Aopt", 'a', false),
                        new ShortOptionSpecification("Bopt", 'b', false),
                        new LongOptionSpecification("Copt", "copt", true)),
                    Verbs: ImmutableHashSet.Create<VerbSpecification>(),
                    Arguments: ImmutableList.Create(new ArgumentSpecification("Argument"))),
                new Dictionary<string, string>
                {
                    ["Aopt"] = "True",
                    ["Bopt"] = "True",
                    ["Copt"] = "val",
                    ["Argument"] = "arg"
                }
            },
            new object[]    // Nested verbs case
            {
                new []{ "verb1", "verb1.1", "arg1.1", "arg1", "arg" },
                new CommandLineSpecification(
                    Options: ImmutableHashSet.Create<OptionSpecificationBase>(),
                    Verbs: ImmutableHashSet.Create(
                        new VerbSpecification("Verb1",
                            Options: ImmutableHashSet.Create<OptionSpecificationBase>(),
                            Verbs: ImmutableHashSet.Create(
                                new VerbSpecification("Verb1.1",
                                    Options: ImmutableHashSet.Create<OptionSpecificationBase>(),
                                    Verbs: ImmutableHashSet.Create<VerbSpecification>(),
                                    Arguments: ImmutableList.Create(new ArgumentSpecification("Argument1.1")))),
                            Arguments: ImmutableList.Create(new ArgumentSpecification("Argument1")))),
                    Arguments: ImmutableList.Create(new ArgumentSpecification("Argument"))),
                new Dictionary<string, string>
                {
                    ["Verb1:Verb1.1:Argument1.1"] = "arg1.1",
                    ["Verb1:Argument1"] = "arg1",
                    ["Argument"] = "arg"
                }
            }
        };

        [Theory]
        [MemberData(nameof(TestData))]
        public void E2ETest(
            string[] args,
            CommandLineSpecification commandLineSpecification,
            Dictionary<string, string> expected)
        {
            // Arrange

            // Act
            var actual = new ConfigurationBuilder()
                .AddCommandLineConfiguration(args, commandLineSpecification)
                .Build();

            // Assert
            Assert.All(expected.Keys, key => Assert.Equal(expected[key], actual[key]));
        }
    }
}
