using System;

using Luger.Configuration.CommandLine.Specifications;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    /// <summary>
    /// Represents a source of configuration key/values for <see cref="CommandLineConfigurationProvider"/>.
    /// </summary>
    public class CommandLineConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// The array of command line arguments to parse.
        /// If not set, <see cref="Environment.GetCommandLineArgs"/> is used when building provider.
        /// </summary>
        public string[]? Args { get; set; }

        /// <summary>
        /// The specification of the command line. If not configured, an empty specification is used by provider.
        /// </summary>
        public CommandLineSpecification? Specification { get; set; }

        /// <summary>
        /// The root configuration section for command line configuration items. If not set, configuration root is used.
        /// </summary>
        public string? CommandLineSection { get; set; }

        public void ConfigureSpecification(
            Func<CommandLineSpecification, CommandLineSpecification> specificationBuilder,
            StringComparison nameComparison = StringComparison.OrdinalIgnoreCase)

            => Specification = specificationBuilder(new CommandLineSpecification(nameComparison));

        public IConfigurationProvider Build(IConfigurationBuilder builder)

            => new CommandLineConfigurationProvider(
                args: Args ?? Environment.GetCommandLineArgs()[1..],
                specification: Specification,
                commandLineSection: CommandLineSection);
    }
}
