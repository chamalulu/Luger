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
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddCommandLineConfiguration(source =>
                    {
                        source.Args = args;
                        source.ErrorPath = "CLC_ERROR";
                        source.Specification = new CommandLineSpecification()
                            .AddFlag(new FlagSpecification("Help", "help", 'h'));
                    });
                });

        private static async Task<int> Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();

            var configurationRoot = (ConfigurationRoot)host.Services.GetService(typeof(IConfiguration));

            var errorSection = configurationRoot.GetSection("CLC_ERROR");

            if (errorSection.Exists())
            {
                Console.WriteLine("Command Line Configuration failed. Here's the error section.");
                Console.WriteLine();

                foreach (var (key, value) in errorSection.AsEnumerable(true))
                {
                    Console.WriteLine($"Key: '{key}', Value '{value}'.");
                }

                return 1;
            }

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
