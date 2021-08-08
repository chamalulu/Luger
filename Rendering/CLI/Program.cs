using System.Threading.Tasks;

using Luger.Configuration;
using Luger.Configuration.CommandLine.Specifications;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RenderSandBox
{
    internal class Program
    {
        private static CommandLineSpecification SpecificationBuilder(CommandLineSpecification specification)

            => specification
                .AddStandardFlags()
                .AddFlagWithArgument("Width", "width", 'w')
                .AddFlagWithArgument("Height", "height", 'h')
                .AddFlagWithArgument("Size", "size", 's')
                .AddFlagWithArgument("OutFile", "out-file", 'o');

        private static Task Main()

            => Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddCommandLineConfiguration(source =>
                    {
                        source.ConfigureSpecification(SpecificationBuilder);
                    });
                })
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddOptions<RenderOptions>()
                        .Bind(context.Configuration)
                        .Validate(renderOptions => renderOptions.Width > 0 && renderOptions.Height > 0);

                    services.AddHostedService<RenderService>();
                })
                .RunConsoleAsync();
    }
}
