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
            string? errorPath = null)
            
            => configurationBuilder.Add(new CommandLineConfigurationSource
            {
                Args = args,
                Specification = specification,
                ErrorPath = errorPath
            });

        public static IConfigurationBuilder AddCommandLineConfiguration(
            this IConfigurationBuilder configurationBuilder,
            Action<CommandLineConfigurationSource> configureSource)
            
            => configurationBuilder.Add(configureSource);
    }
}
