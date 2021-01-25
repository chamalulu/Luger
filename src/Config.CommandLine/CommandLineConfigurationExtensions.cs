using System;

using Luger.Extensions.Configuration.CommandLine;

using Microsoft.Extensions.Configuration;

namespace Luger.Extensions.Configuration
{
    public static class CommandLineConfigurationExtensions
    {
        public static IConfigurationBuilder AddCommandLineConfiguration(
            this IConfigurationBuilder configurationBuilder,
            string[] args,
            CommandLineSpecification? specification = null)
            
            => configurationBuilder.Add(new CommandLineConfigurationSource { Args = args, Specification = specification });

        public static IConfigurationBuilder AddCommandLineConfiguration(
            this IConfigurationBuilder configurationBuilder,
            Action<CommandLineConfigurationSource> configureSource)
            
            => configurationBuilder.Add(configureSource);
    }
}
