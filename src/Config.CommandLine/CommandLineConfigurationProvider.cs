using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Luger.Extensions.Configuration.CommandLine
{
    public class CommandLineConfigurationProvider : ConfigurationProvider
    {
        private readonly CommandLineSpecification _commandLineSpecification;

        public CommandLineConfigurationProvider(IEnumerable<string>? args, CommandLineSpecification? commandLineSpecification = null)
        {
            Args = args?.ToArray() ?? throw new ArgumentNullException(nameof(args));

            _commandLineSpecification = commandLineSpecification ?? CommandLineSpecification.Empty;
        }

        protected IEnumerable<string> Args { get; private init; }

        public override void Load()
        {
            // Create parser from specification
            var parser = CommandLineParser.Create(_commandLineSpecification);

            // Tokenize arguments
            var tokens = ImmutableQueue.CreateRange(Args.SelectMany(CommandLineTokenizer.Tokenize));

            var state = new ParseState(tokens);

            // Parse tokens
            // TODO: errorhandling
            var (successes, failures) = parser.Parse(state);
            var value = successes.Single().value; // Throws on ambiguous parsing

            // Collect configuration items
            value.Collect("", Set);
        }
    }
}
