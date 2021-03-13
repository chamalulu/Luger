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

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddCommandLineConfiguration(source =>
                    {
                        source.Args = args; // Not really needed, default is taken from Environment.GetCommandLineArgs()
                        source.FailureCallback = FailureCallback;
                        source.Specification = new CommandLineSpecification()
                            .AddFlag(new FlagSpecification("Help", "help", 'h'));
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

            var helpFlag = configurationRoot["Help"];

            if (helpFlag is null)
            {
                Console.WriteLine("Help flag not set.");
            }
            else
            {
                Console.WriteLine($"Help flag set to '{helpFlag}'.");
            }

            Console.WriteLine();
            Console.WriteLine("Press Ctrl-C to exit.");

            await host.RunAsync();

            return Environment.ExitCode;
        }
    }
}
