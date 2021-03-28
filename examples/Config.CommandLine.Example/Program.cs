using System;
using System.Threading.Tasks;

using Luger.Configuration;
using Luger.Configuration.CommandLine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Config.CommandLine.Example
{
    internal class Program
    {
        private static bool FailureSignalled;

        private static void FailureCallback(string message, TokenBase token)
        {
            FailureSignalled = true;

            Console.WriteLine();
            Console.WriteLine("Command Line Configuration failed with the following error.");
            Console.WriteLine(token is null ? $"Message: {message}" : $"Message: {message}, Token: {token}");
        }

        private static IHostBuilder CreateHostBuilder(string[] args)

            => Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddCommandLineConfiguration(source =>
                    {
                        // Set args. Not really needed. Default is taken from Environment.GetCommandLineArgs().
                        source.Args = args;

                        // Set failure callback where configuration can report configuration errors.
                        // Is there a standard way to report configuration errors (not exceptions) during host configuration?
                        source.FailureCallback = FailureCallback;

                        // Set command line specification.
                        // I.e. the specification of how to parse and interpret the command line arguments.
                        source.Specification = new CommandLineSpecification()
                            .AddStandardFlags()
                            .AddArgument(new MultiArgumentSpecification("Arguments"));

                        // Set configuration section for command line configuration.
                        // If unset (null), configuration items are rooted in configuration root.
                        source.CommandLineSection = "CommandLine";
                    });
                });

        private static async Task<int> Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();

            if (FailureSignalled)
            {
                return 1;
            }

            var configurationRoot = (ConfigurationRoot)host.Services.GetService(typeof(IConfiguration));

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
    }
}
