using System;
using System.Collections.Immutable;

namespace Luger.Configuration.CommandLine
{
    public abstract record SpecificationBase;

    public abstract record NamedSpecificationBase(string Name) : SpecificationBase
    {
        public virtual bool Equals(NamedSpecificationBase? other) =>

            other is not null && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    public record ArgumentSpecification(string? Name = null) : NamedSpecificationBase(Name ?? string.Empty);

    public abstract record OptionSpecificationBase(string Name, bool TakesArgument) : NamedSpecificationBase(Name);

    public record ShortOptionSpecification(string Name, char Flag, bool TakesArgument) : OptionSpecificationBase(Name, TakesArgument);

    public record LongOptionSpecification(string Name, string Key, bool TakesArgument) : OptionSpecificationBase(Name, TakesArgument);

    public record VerbSpecification(string Name,
        ImmutableHashSet<OptionSpecificationBase> Options,
        ImmutableHashSet<VerbSpecification> Verbs,
        ImmutableList<ArgumentSpecification> Arguments,
        StringComparison NameComparison = StringComparison.OrdinalIgnoreCase) : NamedSpecificationBase(Name);

    public record CommandLineSpecification(
        ImmutableHashSet<OptionSpecificationBase> Options,
        ImmutableHashSet<VerbSpecification> Verbs,
        ImmutableList<ArgumentSpecification> Arguments) : SpecificationBase
    {
        public static readonly CommandLineSpecification Empty = new(
            Options: ImmutableHashSet.Create<OptionSpecificationBase>(),
            Verbs: ImmutableHashSet.Create<VerbSpecification>(),
            Arguments: ImmutableList.Create<ArgumentSpecification>());
    }
}
