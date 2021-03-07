using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    public class CommandLineConfigurationSource : IConfigurationSource
    {
        public IEnumerable<string>? Args { get; set; }

        public CommandLineSpecification? Specification { get; set; }

        public string? ErrorPath { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)

            => new CommandLineConfigurationProvider(Args ?? throw new InvalidOperationException(), Specification, ErrorPath);
    }
}
