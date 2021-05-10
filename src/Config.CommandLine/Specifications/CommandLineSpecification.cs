using System;
using System.Collections.Immutable;

namespace Luger.Configuration.CommandLine.Specifications
{
    public sealed record CommandLineSpecification
    {
        public static readonly CommandLineSpecification Empty = new();

        public CommandLineSpecification(StringComparison nameComparison = StringComparison.OrdinalIgnoreCase)
        {
            Names = ImmutableHashSet.Create<SpecificationName>(new SpecificationNameEqualityComparer(nameComparison));
            Flags = NamedSpecificationList<FlagSpecification>.Empty;
            Verbs = NamedSpecificationList<VerbSpecification>.Empty;
            Arguments = NamedSpecificationList<ArgumentSpecification>.Empty;
            MultiArgument = null;
        }

        private ImmutableHashSet<SpecificationName> Names { get; init; }

        public NamedSpecificationList<FlagSpecification> Flags { get; private init; }
        public NamedSpecificationList<VerbSpecification> Verbs { get; private init; }
        public NamedSpecificationList<ArgumentSpecification> Arguments { get; private init; }
        public MultiArgumentSpecification? MultiArgument { get; private init; }

        public bool CanAddVerb => Arguments.Count == 0 && MultiArgument is null;

        public bool CanAddArgument => Verbs.Count == 0 && MultiArgument is null;

        private CommandLineSpecification Add(SpecificationName name)

            => Names.Contains(name)
                ? throw new InvalidOperationException()
                : this with { Names = Names.Add(name) };

        public CommandLineSpecification Add(FlagSpecification specification)

            => Add(specification.Name) with { Flags = Flags.Add(specification) };

        public CommandLineSpecification Add(VerbSpecification specification)

            => CanAddVerb
                ? Add(specification.Name) with { Verbs = Verbs.Add(specification) }
                : throw new InvalidOperationException();

        public CommandLineSpecification Add(ArgumentSpecification specification)

            => CanAddArgument
                ? Add(specification.Name) with { Arguments = Arguments.Add(specification) }
                : throw new InvalidOperationException();

        public CommandLineSpecification Add(MultiArgumentSpecification specification)

            => CanAddArgument
                ? Add(specification.Name) with { MultiArgument = specification }
                : throw new InvalidOperationException();

        
    }
}
