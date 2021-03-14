using System;

using Luger.Configuration.CommandLine;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration
{
    public static class CommandLineConfigurationExtensions
    {
        public static IConfigurationBuilder AddCommandLineConfiguration(
            this IConfigurationBuilder configurationBuilder,
            string[] args,
            CommandLineSpecification? specification = null,
            FailureCallback? failureCallback = null,
            string? commandLineSection = null)
            
            => configurationBuilder.Add(new CommandLineConfigurationSource
            {
                Args = args,
                Specification = specification,
                FailureCallback = failureCallback,
                CommandLineSection = commandLineSection
            });

        public static IConfigurationBuilder AddCommandLineConfiguration(
            this IConfigurationBuilder configurationBuilder,
            Action<CommandLineConfigurationSource> configureSource)
            
            => configurationBuilder.Add(configureSource);


        [Flags]
        public enum StandardFlagsFlags
        {
            DryRun = 1,
            Help = 2,
            Quiet = 4,
            Verbose = 8
        }

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
