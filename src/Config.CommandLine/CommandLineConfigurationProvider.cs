using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

using Luger.Configuration.CommandLine.Specifications;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    public class ParseFailuresException : Exception
    {
        private readonly ImmutableList<(string message, ParseState state)> _failures;

        public ParseFailuresException(ImmutableList<(string message, ParseState state)> failures) : base(failures[0].message)

            => _failures = failures;

        public IEnumerable<(string message, TokenBase? token)> Failures

            => from failure in _failures
               select (failure.message, failure.state.Tokens.FirstOrDefault());
    }

    /// <summary>
    /// Configuration provider parsing command line arguments according to given specification.
    /// </summary>
    public class CommandLineConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// Create new command line configuration provider.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <param name="specification">Command line specification. If null, an empty specification is used.</param>
        /// <param name="failureCallback">Callback used to report parsing errors. If null, errors are not reported.</param>
        /// <param name="commandLineSection">
        /// Configuration section to root configuration items in. If null, configuration root is used.
        /// </param>
        public CommandLineConfigurationProvider(
            string[] args,
            CommandLineSpecification? specification = null,
            string? commandLineSection = null)
        {
            Args = ImmutableList.CreateRange(args);
            Specification = specification ?? CommandLineSpecification.Empty;
            CommandLineSection = commandLineSection;
        }

        /// <summary>
        /// Command line arguments used by configuration provider.
        /// </summary>
        public IReadOnlyList<string> Args { get; }

        /// <summary>
        /// Command line specification used by configuration provider.
        /// </summary>
        public CommandLineSpecification Specification { get; }

        /// <summary>
        /// Configuration section to root configuration items in. If null, configuration root is used.
        /// </summary>
        protected string? CommandLineSection { get; }

        /// <summary>
        /// Set configuration items in <see cref="ConfigurationProvider.Data"/>.
        /// </summary>
        /// <param name="successes">List of successes to set.</param>
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

        /// <summary>
        /// Loads the data for this provider.
        /// </summary>
        /// <remarks>
        /// (re)Load is idempotent since <see cref="Args"/> and <see cref="Specification"/> are immutable properties.
        /// </remarks>
        public override void Load()
        {
            // Create parser from specification
            var parser = CommandLineParser.Create(Specification);

            // Tokenize arguments
            var tokens = ImmutableQueue.CreateRange(Args.SelectMany(CommandLineTokenizer.Tokenize));

            // Parse tokens
            var state = new ParseState(tokens);

            var (successes, failures) = parser.Parse(state);

            if (failures.Any())
            {
                // Failed to parse command line given these command line arguments and specification
                throw new ParseFailuresException(failures);
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
