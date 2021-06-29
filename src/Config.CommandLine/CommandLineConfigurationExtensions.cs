using System;

using Luger.Configuration.CommandLine;
using Luger.Configuration.CommandLine.Specifications;

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
        /// <param name="commandLineSection">Configuration section to root configuration items in.</param>
        /// <returns>The configuration builder.</returns>
        public static IConfigurationBuilder AddCommandLineConfiguration(
            this IConfigurationBuilder configurationBuilder,
            string[]? args = null,
            CommandLineSpecification? specification = null,
            string? commandLineSection = null)
        {
            var source = new CommandLineConfigurationSource
            {
                Args = args,
                Specification = specification,
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
    }
}
