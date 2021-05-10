using System;
using System.Collections.Generic;
using System.Linq;

namespace Luger.Configuration.CommandLine.Specifications
{
    public static class CommandLineSpecificationExtensions
    {
        /// <summary>
        /// Add flag.
        /// </summary>
        public static CommandLineSpecification AddFlagWithValue(
            this CommandLineSpecification receiver,
            string name,
            string longName,
            char shortName = default,
            string value = "True")

            => receiver.Add(new FlagWithValueSpecification(new(name), new(longName), new(shortName), value));

        /// <summary>
        /// Add flag with value argument.
        /// </summary>
        public static CommandLineSpecification AddFlagWithArgument(
            this CommandLineSpecification receiver,
            string name,
            string longName,
            char shortName = default)

            => receiver.Add(new FlagWithArgumentSpecification(new(name), new(longName), new(shortName)));

        /// <summary>
        /// Flags enum of standard flags.
        /// </summary>
        [Flags]
        public enum StandardFlagsFlags
        {
            DryRun = 1,
            Help = 2,
            Quiet = 4,
            Verbose = 8
        }

        private static readonly Dictionary<StandardFlagsFlags, FlagSpecification> StandardFlags = new()
        {
            [StandardFlagsFlags.DryRun] = new FlagWithValueSpecification(new("DryRun"), new("dry-run"), new('n')),
            [StandardFlagsFlags.Help] = new FlagWithValueSpecification(new("Help"), new("help"), new('h')),
            [StandardFlagsFlags.Quiet] = new FlagWithValueSpecification(new("Quiet"), new("quiet"), new('q')),
            [StandardFlagsFlags.Verbose] = new FlagWithValueSpecification(new("Verbose"), new("verbose"), new('v'))
        };

        /// <summary>
        /// Add standard flags.
        /// </summary>
        /// <param name="standardFlags">Flags to add.</param>
        public static CommandLineSpecification AddStandardFlags(
            this CommandLineSpecification receiver,
            StandardFlagsFlags standardFlags
                = StandardFlagsFlags.DryRun
                | StandardFlagsFlags.Help
                | StandardFlagsFlags.Quiet
                | StandardFlagsFlags.Verbose)

            => Enum.GetValues<StandardFlagsFlags>()
                .Where(f => standardFlags.HasFlag(f))
                .Aggregate(
                    seed: receiver,
                    func: (caf, sf) => caf.Add(StandardFlags[sf]));

        /// <summary>
        /// Add verb.
        /// </summary>
        public static CommandLineSpecification AddVerb(
            this CommandLineSpecification receiver,
            string name,
            Func<CommandLineSpecification, CommandLineSpecification> commandLineSpecificationBuilder,
            StringComparison verbNameComparison = StringComparison.OrdinalIgnoreCase,
            StringComparison childNameComparison = StringComparison.OrdinalIgnoreCase)
        {
            var commandLineSpecification = commandLineSpecificationBuilder(new CommandLineSpecification(childNameComparison));
            var verbSpecification = new VerbSpecification(new(name), commandLineSpecification, verbNameComparison);

            return receiver.Add(verbSpecification);
        }

        /// <summary>
        /// Add argument.
        /// </summary>
        public static CommandLineSpecification AddArgument(this CommandLineSpecification receiver, string name)

            => receiver.Add(new ArgumentSpecification(new(name)));

        /// <summary>
        /// Add multi-argument.
        /// </summary>
        public static CommandLineSpecification AddMultiArgument(this CommandLineSpecification receiver, string name)

            => receiver.Add(new MultiArgumentSpecification(new(name)));
    }
}
