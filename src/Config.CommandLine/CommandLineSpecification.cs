using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Luger.Configuration.CommandLine
{
    /// <summary>
    /// Base class for named specifications.
    /// </summary>
    /// <remarks>
    /// A named specification has a <see cref="Name"/> starting with a latin letter followed by zero or more word characters.
    /// </remarks>
    public abstract record NamedSpecification
    {
        private static readonly Regex NameRex = new(@"^\p{L}\w*$");

        /// <summary>
        /// Name of specification. Used in parsing to yield named nodes.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Create a named specification.
        /// </summary>
        /// <param name="name">Name of specification.</param>
        protected NamedSpecification(string name)

            => Name = NameRex.IsMatch(name)
                ? name
                : throw new ArgumentException("Invalid name", nameof(name));
    }

    /// <summary>
    /// Argument specification.
    /// </summary>
    /// <remarks>
    /// An argument specification is a <see cref="NamedSpecification"/>.
    /// Successful parsing yields an <see cref="ArgumentNode"/>
    /// </remarks>
    public record ArgumentSpecification(string Name) : NamedSpecification(Name);

    /// <summary>
    /// Multi-argument specification.
    /// </summary>
    /// <remarks>
    /// A multi-argument specification is an <see cref="ArgumentSpecification"/>.
    /// Successful parsing yields zero or more indexed <see cref="MultiArgumentNode"/>s
    /// </remarks>
    public record MultiArgumentSpecification(string Name) : ArgumentSpecification(Name);

    /// <summary>
    /// Base class for flag specifications.
    /// </summary>
    /// <remarks>
    /// A flag specification is a <see cref="NamedSpecification"/>.
    /// It also has a <see cref="LongName"/> and an optional <see cref="ShortName"/>.
    /// </remarks>
    public abstract record FlagSpecificationBase(string Name, string LongName, char ShortName = default)
        : NamedSpecification(Name);

    /// <summary>
    /// Flag specification. 
    /// </summary>
    /// <remarks>
    /// A flag specification is a <see cref="FlagSpecificationBase"/>.
    /// It has a prespecified <see cref="Value"/> wich default to "True".
    /// Successful parsing yields a <see cref="FlagNode"/> with the prespecified value.
    /// </remarks>
    public record FlagSpecification(string Name, string LongName, char ShortName = default, string Value = "True")
        : FlagSpecificationBase(Name, LongName, ShortName);

    /// <summary>
    /// Flag with value specification.
    /// </summary>
    /// <remarks>
    /// A flag with value specification is a <see cref="FlagSpecificationBase"/>.
    /// Successful parsing yields a <see cref="FlageNode"/> with the value of the anonymous argument following the flag.
    /// </remarks>
    public record FlagWithValueSpecification(string Name, string LongName, char ShortName = default)
        : FlagSpecificationBase(Name, LongName, ShortName);

    /// <summary>
    /// Verb specification.
    /// </summary>
    /// <remarks>
    /// A verb specification is a <see cref="NamedSpecification"/>.
    /// It has a <see cref="CommandLineSpecification"/> and a <see cref="NameComparison"/> which default to
    /// <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// Successful parsing yields a <see cref="VerbNode"/>, a <see cref="VerbNodeWithVerb"/> or a
    /// <see cref="VerbNodeWithArguments"/>.
    /// </remarks>
    public record VerbSpecification(string Name,
        CommandLineSpecification CommandLineSpecification,
        StringComparison NameComparison = StringComparison.OrdinalIgnoreCase) : NamedSpecification(Name)
    {
        /// <summary>
        /// Create a verb specification.
        /// </summary>
        /// <param name="Name">Name of specification.</param>
        /// <param name="NameComparison">Comparison of name used when parsing.</param>
        public VerbSpecification(string Name, StringComparison NameComparison = StringComparison.OrdinalIgnoreCase)
            : this(Name, CommandLineSpecification.Empty, NameComparison) { }

        /// <summary>
        /// Flag specifications of verb.
        /// </summary>
        public ImmutableList<FlagSpecificationBase> Flags => CommandLineSpecification.Flags;

        /// <summary>
        /// Sub-verb specifications of verb.
        /// </summary>
        public ImmutableList<VerbSpecification> Verbs => CommandLineSpecification.Verbs;

        /// <summary>
        /// Argument specifications of verb.
        /// </summary>
        public ImmutableList<ArgumentSpecification> Arguments => CommandLineSpecification.Arguments;

        /// <summary>
        /// Adds a flag specification to this verb specification.
        /// </summary>
        /// <param name="flagSpecification">Flag specification to add.</param>
        /// <returns>Verb specification with added flag specification.</returns>
        public VerbSpecification AddFlag(FlagSpecificationBase flagSpecification)

            => this with { CommandLineSpecification = CommandLineSpecification.AddFlag(flagSpecification) };

        /// <summary>
        /// Adds a sub-verb specification to this verb specification.
        /// </summary>
        /// <param name="verbSpecification">Sub-verb specification to add.</param>
        /// <returns>Verb specification with added sub-verb specification.</returns>
        public VerbSpecification AddVerb(VerbSpecification verbSpecification)

            => this with { CommandLineSpecification = CommandLineSpecification.AddVerb(verbSpecification) };

        /// <summary>
        /// Adds an argument specification to this verb specification.
        /// </summary>
        /// <param name="argumentSpecification">Argument specification to add.</param>
        /// <returns>Verb specification with added argument specification.</returns>
        public VerbSpecification AddArgument(ArgumentSpecification argumentSpecification)

            => this with { CommandLineSpecification = CommandLineSpecification.AddArgument(argumentSpecification) };
    }

    /// <summary>
    /// Command line specification.
    /// </summary>
    /// <remarks>
    /// A command line specification is the root specification. It has no name.
    /// Successful parsing yields a <see cref="CommandLineNode"/>, a <see cref="CommandLineNodeWithVerb"/> or a
    /// <see cref="CommandLineNodeWithArguments"/>.
    /// </remarks>
    public record CommandLineSpecification
    {
        /// <summary>
        /// The empty command line specification.
        /// </summary>
        public static CommandLineSpecification Empty = new();

        /// <summary>
        /// Flag specifications of command line.
        /// </summary>
        public ImmutableList<FlagSpecificationBase> Flags { get; private init; } = ImmutableList<FlagSpecificationBase>.Empty;

        /// <summary>
        /// Verb specifications of command line.
        /// </summary>
        public ImmutableList<VerbSpecification> Verbs { get; private init; } = ImmutableList<VerbSpecification>.Empty;

        /// <summary>
        /// Argument specifications of command line.
        /// </summary>
        public ImmutableList<ArgumentSpecification> Arguments { get; private init; } = ImmutableList<ArgumentSpecification>.Empty;

        private IEnumerable<string> Names => from specs in new IEnumerable<NamedSpecification>[] { Flags, Verbs, Arguments }
                                             from spec in specs
                                             select spec.Name;

        private TSpec CheckName<TSpec>(TSpec spec, string paramName) where TSpec : NamedSpecification

            => Names.Contains(spec.Name, StringComparer.OrdinalIgnoreCase)
                ? throw new ArgumentException("Duplicate name.", paramName)
                : spec;

        /// <summary>
        /// Adds a flag specification to this command line specification.
        /// </summary>
        /// <param name="flagSpecification">Flag specification to add.</param>
        /// <returns>Command line specification with added flag specification.</returns>
        public CommandLineSpecification AddFlag(FlagSpecificationBase flagSpecification)

            => this with { Flags = Flags.Add(CheckName(flagSpecification, nameof(flagSpecification))) };

        /// <summary>
        /// Adds a verb specification to this command line specification.
        /// </summary>
        /// <param name="verbSpecification">Verb specification to add.</param>
        /// <returns>Command line specification with added sub-verb specification.</returns>
        public CommandLineSpecification AddVerb(VerbSpecification verbSpecification)

            => this with { Verbs = Verbs.Add(CheckName(verbSpecification, nameof(verbSpecification))) };

        /// <summary>
        /// Adds an argument specification to this command line specification.
        /// </summary>
        /// <param name="argumentSpecification">Argument specification to add.</param>
        /// <returns>Command line specification with added argument specification.</returns>
        public CommandLineSpecification AddArgument(ArgumentSpecification argumentSpecification)

            => Arguments.Count > 0 && Arguments[^1] is MultiArgumentSpecification
                ? throw new InvalidOperationException("No more argument specifications allowed after multi argument specification.")
                : this with { Arguments = Arguments.Add(CheckName(argumentSpecification, nameof(argumentSpecification))) };
    }
}
