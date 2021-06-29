using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Luger.Configuration;
using Luger.Configuration.CommandLine;
using Luger.Configuration.CommandLine.Specifications;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Config.CommandLine.Example
{
    internal class Program
    {
        private static IHostBuilder CreateHostBuilder(string[] args)

            => Host.CreateDefaultBuilder()  // N.B. Don't use the overload with args parameter.
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddCommandLineConfiguration(source =>
                    {
                        // Set args. Not really needed. Default is taken from Environment.GetCommandLineArgs().
                        source.Args = args;

                        // Configure command line specification.
                        // I.e. the specification of how to parse and interpret the command line arguments.
                        source.ConfigureSpecification(specification
                            => specification
                                .AddStandardFlags()
                                .AddMultiArgument("Arguments"));

                        // Equivalent specification setup with lots of news.
                        //source.Specification = new CommandLineSpecification()
                        //    .AddStandardFlags()
                        //    .Add(new MultiArgumentSpecification(new SpecificationName("Arguments")));

                        // Set configuration section for command line configuration.
                        // If unset (null), configuration items are rooted in configuration root.
                        source.CommandLineSection = "CommandLine";
                    });
                });

        private static async Task<int> Main(string[] args)
        {
            try
            {
                using var host = CreateHostBuilder(args).Build();

                var configurationRoot = host.Services.GetService(typeof(IConfiguration)) as ConfigurationRoot
                    ?? throw new ApplicationException("Unable to get configuration service.");

                PrintProviderInfo(configurationRoot);

                var commandLineSection = configurationRoot.GetSection("CommandLine");   // As set in CreateHostBuilder

                foreach (var (key, value) in commandLineSection.AsEnumerable())
                {
                    Console.WriteLine($"{key}:\t{value}");
                }

                Console.WriteLine();
                Console.WriteLine("Press Ctrl-C to exit.");

                await host.RunAsync();

                return Environment.ExitCode;
            }
            catch (ParseFailuresException pfex)
            {
                Console.WriteLine();
                Console.WriteLine("Command Line Configuration failed with the following error(s).");

                foreach (var (message, token) in pfex.Failures)
                {
                    Console.WriteLine(token is null ? $"Message: {message}" : $"Message: {message}, Token: {token}");
                }

                return 1;
            }
        }

        private static readonly Lazy<System.Reflection.FieldInfo?> ConfigFieldInfo = new(()

            => typeof(ChainedConfigurationProvider).GetField(
                "_config",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));

        private static void PrintProviderInfo(ConfigurationRoot configurationRoot)
        {
            static IEnumerable<TProvider> FindProvidersOfType<TProvider>(IConfigurationRoot root)

                => root.Providers.OfType<TProvider>().Concat<TProvider>(
                    second: root.Providers.OfType<ChainedConfigurationProvider>()
                        .SelectMany<ChainedConfigurationProvider, TProvider>(
                            selector: cp => ConfigFieldInfo.Value?.GetValue(cp) is IConfigurationRoot cr
                                ? FindProvidersOfType<TProvider>(cr)
                                : Enumerable.Empty<TProvider>()));

            var provider = FindProvidersOfType<CommandLineConfigurationProvider>(configurationRoot).FirstOrDefault();

            if (provider is not null)
            {
                Console.Write("Args: ");
                Console.WriteLine(provider.Args.Count == 0 ? "[]" : $"[ \"{string.Join("\", \"", provider.Args)}\" ]");

                Console.Write("Specification: ");
                Console.WriteLine(provider.Specification);
            }
        }
    }
}
