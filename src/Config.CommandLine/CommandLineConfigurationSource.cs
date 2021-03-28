using System;

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
        /// The specification of the command line. If not set, an empty specification is used by provider.
        /// </summary>
        public CommandLineSpecification? Specification { get; set; }

        /// <summary>
        /// Callback for reporting failures in parsing arguments. If not set, failures are not reported.
        /// </summary>
        public FailureCallback? FailureCallback { get; set; }

        /// <summary>
        /// The root configuration section for command line configuration items. If not set, configuration root is used.
        /// </summary>
        public string? CommandLineSection { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)

            => new CommandLineConfigurationProvider(
                args: Args ?? Environment.GetCommandLineArgs(),
                specification: Specification,
                failureCallback: FailureCallback,
                commandLineSection: CommandLineSection);
    }
}
