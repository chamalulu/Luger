using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    public class CommandLineConfigurationProvider : ConfigurationProvider
    {
        public CommandLineConfigurationProvider(
            IEnumerable<string> args,
            CommandLineSpecification? specification = null,
            string? errorPath = null)
        {
            Args = Array.AsReadOnly(args.ToArray());

            Specification = specification ?? CommandLineSpecification.Empty;

            ErrorPath = errorPath ?? nameof(CommandLineConfigurationProvider);
        }

        public IReadOnlyList<string> Args { get; }

        public CommandLineSpecification Specification { get; }

        protected string ErrorPath { get; }

        protected void Set(IEnumerable<string> path, string value) => Set(ConfigurationPath.Combine(path), value);

        protected virtual void SetErrorKeys(ImmutableList<(string message, ParseState state)> failures)
        {
            var path = ImmutableList.Create(ErrorPath);

            // Add argument list keys
            var argsPath = path.Add("Args");

            if (Args.Any())
            {
                foreach (var (arg, index) in Args.Select((a, i) => (a, i + 1)))
                {
                    Set(argsPath.Add(index.ToString()), arg);
                }
            }
            else
            {
                Set(argsPath, "Empty");
            }

            // Add failures
            var failuresPath = path.Add("Failures");

            foreach (var (failure, index) in failures.Select((f, i) => (f, i + 1)))
            {
                var failurePath = failuresPath.Add(index.ToString());

                Set(failurePath.Add("Message"), failure.message);

                var token = failure.state.Tokens.FirstOrDefault();

                if (token is TokenBase)
                {
                    Set(failurePath.Add("Token"), token.ToString());
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
                // Set some predefined keys in configuration the client can use in error handling.
                SetErrorKeys(failures);
            }
            else
            {
                // If successes.Count > 1 we have an ambiguous specification leading with multiple successes.
                // Collect them all, hope for no collisions and let the client deal with ambiguities.
                foreach (var success in successes)
                {
                    // Collect configuration items
                    foreach (var (key, value) in success.value.Collect())
                    {
                        Set(key, value);
                    }
                }
            }
        }
    }
}
