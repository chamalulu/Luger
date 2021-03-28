using System;

using Luger.Configuration.CommandLine;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration
{
    public static class CommandLineConfigurationExtensions
    {
        /// <summary>
        /// Add new <see cref="CommandLineConfigurationSource"/> to <paramref name="configurationBuilder"/> with given settings.
        /// </summary>
        /// <param name="configurationBuilder">Configuration builder provided by host builder.</param>
        /// <param name="args">Command line arguments to parse.</param>
        /// <param name="specification">Specification for parsing and interpreting command line arguments.</param>
        /// <param name="failureCallback">Callback for provider to report parsing errors.</param>
        /// <param name="commandLineSection">Configuration section to root configuration items in.</param>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddCommandLineConfiguration(
            this IConfigurationBuilder configurationBuilder,
            string[]? args = null,
            CommandLineSpecification? specification = null,
            FailureCallback? failureCallback = null,
            string? commandLineSection = null)
        {
            var source = new CommandLineConfigurationSource
            {
                Args = args,
                Specification = specification,
                FailureCallback = failureCallback,
                CommandLineSection = commandLineSection
            };

            return configurationBuilder.Add(source);
        }

        /// <summary>
        /// Add new <see cref="CommandLineConfigurationSource"/> to <paramref name="configurationBuilder"/> with settings set by
        /// <paramref name="configureSource"/> action.
        /// </summary>
        /// <param name="configurationBuilder">Configuration builder provided by host builder.</param>
        /// <param name="configureSource">Action providing settings for configuration source.</param>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddCommandLineConfiguration(
            this IConfigurationBuilder configurationBuilder,
            Action<CommandLineConfigurationSource> configureSource)
            
            => configurationBuilder.Add(configureSource);

        /// <summary>
        /// Flags enum of standard flags.
        /// </summary>
        [Flags]
        public enum StandardFlagsFlags
        {
            DryRun = 1,
            Help = 2,
            Quiet = 4,
            Verbose = 8
        }

        /// <summary>
        /// Add specification of standard flags to <paramref name="commandLineSpecification"/>
        /// </summary>
        /// <param name="commandLineSpecification">Command line specification to add flags to.</param>
        /// <param name="standardFlags">Flags to add.</param>
        /// <returns>The command line specification.</returns>
        public static CommandLineSpecification AddStandardFlags(
            this CommandLineSpecification commandLineSpecification,
            StandardFlagsFlags standardFlags
                = StandardFlagsFlags.DryRun
                | StandardFlagsFlags.Help
                | StandardFlagsFlags.Quiet
                | StandardFlagsFlags.Verbose)
        {
            commandLineSpecification = standardFlags.HasFlag(StandardFlagsFlags.DryRun)
                ? commandLineSpecification.AddFlag(new FlagSpecification("DryRun", "dry-run", 'n'))
                : commandLineSpecification;

            commandLineSpecification = standardFlags.HasFlag(StandardFlagsFlags.Help)
                ? commandLineSpecification.AddFlag(new FlagSpecification("Help", "help", 'h'))
                : commandLineSpecification;

            commandLineSpecification = standardFlags.HasFlag(StandardFlagsFlags.Quiet)
                ? commandLineSpecification.AddFlag(new FlagSpecification("Quiet", "quiet", 'q'))
                : commandLineSpecification;

            commandLineSpecification = standardFlags.HasFlag(StandardFlagsFlags.Verbose)
                ? commandLineSpecification.AddFlag(new FlagSpecification("Verbose", "verbose", 'v'))
                : commandLineSpecification;

            return commandLineSpecification;
        }
    }
}
