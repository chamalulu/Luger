using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    public delegate void FailureCallback(string message, TokenBase? token);

    public class CommandLineConfigurationProvider : ConfigurationProvider
    {
        public CommandLineConfigurationProvider(
            string[] args,
            CommandLineSpecification? specification = null,
            FailureCallback? failureCallback = null,
            string? commandLineSection = null)
        {
            Args = ImmutableList.CreateRange(args);
            Specification = specification ?? CommandLineSpecification.Empty;
            FailureCallback = failureCallback;
            CommandLineSection = commandLineSection;
        }

        public IReadOnlyList<string> Args { get; }

        public CommandLineSpecification Specification { get; }

        protected FailureCallback? FailureCallback { get; }

        protected string? CommandLineSection { get; }

        protected virtual void ReportFailures(ImmutableList<(string message, ParseState state)> failures)
        {
            foreach (var (message, state) in failures)
            {
                FailureCallback?.Invoke(message, state.Tokens.FirstOrDefault());
            }
        }

        protected virtual void SetConfigurationItems(ImmutableList<(CommandLineNode value, ParseState state)> successes)
        {
            var path = CommandLineSection is null ? ImmutableList<string>.Empty : ImmutableList.Create(CommandLineSection);

            foreach (var success in successes)
            {
                // Collect and set configuration items
                foreach (var (key, value) in success.value.Collect(path))
                {
                    Set(key, value);
                }
            }
        }

        public override void Load()
        {
            // Create parser from specification
            var parser = CommandLineParser.Create(Specification);

            // Tokenize arguments
            var tokens = ImmutableQueue.CreateRange(Args.SelectMany(CommandLineTokenizer.Tokenize));

            var state = new ParseState(tokens);

            // Parse tokens
            var (successes, failures) = parser.Parse(state);

            if (failures.Count > 0)
            {
                // Failed to parse command line given these command line arguments and specification
                // Report failures to client provided callback.
                ReportFailures(failures);
            }
            else
            {
                // If successes.Count > 1 we have an ambiguous specification leading with multiple successes.
                // Collect them all, hope for no collisions and let the client deal with ambiguities.
                SetConfigurationItems(successes);
            }
        }
    }
}
