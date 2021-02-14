using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    public class CommandLineConfigurationSource : IConfigurationSource
    {
        public CommandLineSpecification? Specification { get; set; }

        public IEnumerable<string>? Args { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)

            => new CommandLineConfigurationProvider(Args, Specification);
    }
}
