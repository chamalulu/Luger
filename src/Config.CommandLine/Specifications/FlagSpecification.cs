namespace Luger.Configuration.CommandLine.Specifications
{
    public abstract record FlagSpecification(SpecificationName Name, LongFlagName LongName, ShortFlagName ShortName = default)
        : NamedSpecification(Name);

    public record FlagWithValueSpecification(
        SpecificationName Name,
        LongFlagName LongName,
        ShortFlagName ShortName = default,
        string Value = "True")
        : FlagSpecification(Name, LongName, ShortName);

    public record FlagWithArgumentSpecification(
        SpecificationName Name,
        LongFlagName LongName,
        ShortFlagName ShortName = default)
        : FlagSpecification(Name, LongName, ShortName);
}
