using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Luger.Extensions.Configuration.CommandLine
{
    public class CommandLineConfigurationSource : IConfigurationSource
    {
        public CommandLineSpecification? Specification { get; set; }

        public IEnumerable<string>? Args { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
            
            => new CommandLineConfigurationProvider(this.Args, this.Specification);
    }
}
