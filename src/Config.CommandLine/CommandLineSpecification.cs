using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Luger.Configuration.CommandLine
{
    public abstract record NamedSpecification
    {
        private static readonly Regex NameRex = new Regex(@"^\p{L}\w*$");

        public string Name { get; init; }

        protected NamedSpecification(string name)

            => Name = NameRex.IsMatch(name)
                ? name
                : throw new ArgumentException("Invalid name", nameof(name));
    }

    public record ArgumentSpecification(string Name) : NamedSpecification(Name);

    public record MultiArgumentSpecification(string Name) : ArgumentSpecification(Name);

    public record FlagSpecification(string Name, string LongName, char ShortName = default, bool TakesValue = false)
        : NamedSpecification(Name);

    public record VerbSpecification(string Name,
        CommandLineSpecification CommandLineSpecification,
        StringComparison NameComparison = StringComparison.OrdinalIgnoreCase) : NamedSpecification(Name)
    {
        public VerbSpecification(string Name, StringComparison NameComparison = StringComparison.OrdinalIgnoreCase)
            : this(Name, CommandLineSpecification.Empty, NameComparison) { }

        public ImmutableList<FlagSpecification> Flags => CommandLineSpecification.Flags;

        public ImmutableList<VerbSpecification> Verbs => CommandLineSpecification.Verbs;

        public ImmutableList<ArgumentSpecification> Arguments => CommandLineSpecification.Arguments;

        public VerbSpecification AddFlag(FlagSpecification flagSpecification)

            => this with { CommandLineSpecification = CommandLineSpecification.AddFlag(flagSpecification) };

        public VerbSpecification AddVerb(VerbSpecification verbSpecification)

            => this with { CommandLineSpecification = CommandLineSpecification.AddVerb(verbSpecification) };

        public VerbSpecification AddArgument(ArgumentSpecification argumentSpecification)

            => this with { CommandLineSpecification = CommandLineSpecification.AddArgument(argumentSpecification) };
    }

    public record CommandLineSpecification
    {
        public static CommandLineSpecification Empty => new();

        public ImmutableList<FlagSpecification> Flags { get; private init; } = ImmutableList<FlagSpecification>.Empty;

        public ImmutableList<VerbSpecification> Verbs { get; private init; } = ImmutableList<VerbSpecification>.Empty;

        public ImmutableList<ArgumentSpecification> Arguments { get; private init; } = ImmutableList<ArgumentSpecification>.Empty;

        private IEnumerable<string> Names => from specs in new IEnumerable<NamedSpecification>[] { Flags, Verbs, Arguments }
                                             from spec in specs
                                             select spec.Name;

        private TSpec CheckName<TSpec>(TSpec spec, string paramName) where TSpec : NamedSpecification

            => Names.Contains(spec.Name, StringComparer.OrdinalIgnoreCase)
                ? throw new ArgumentException("Duplicate name.", paramName)
                : spec;

        public CommandLineSpecification AddFlag(FlagSpecification flagSpecification)

            => this with { Flags = Flags.Add(CheckName(flagSpecification, nameof(flagSpecification))) };

        public CommandLineSpecification AddVerb(VerbSpecification verbSpecification)

            => this with { Verbs = Verbs.Add(CheckName(verbSpecification, nameof(verbSpecification))) };

        public CommandLineSpecification AddArgument(ArgumentSpecification argumentSpecification)

            => Arguments.Count > 0 && Arguments[^1] is MultiArgumentSpecification
                ? throw new InvalidOperationException("No more argument specifications allowed after multi argument specification.")
                : this with { Arguments = Arguments.Add(CheckName(argumentSpecification, nameof(argumentSpecification))) };
    }
}
