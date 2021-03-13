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
        /// Defaults to command line arguments provided by <see cref="Environment.GetCommandLineArgs"/>.
        /// </summary>
        public string[] Args { get; set; } = Environment.GetCommandLineArgs();

        /// <summary>
        /// The specification of the command line.
        /// </summary>
        public CommandLineSpecification? Specification { get; set; }

        /// <summary>
        /// Callback for reporting failures in parsing arguments.
        /// </summary>
        public FailureCallback? FailureCallback { get; set; }

        /// <summary>
        /// The root configuration section for command line configuration items.
        /// </summary>
        public string? CommandLineSection { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)

            => new CommandLineConfigurationProvider(Args, Specification, FailureCallback, CommandLineSection);
    }
}
