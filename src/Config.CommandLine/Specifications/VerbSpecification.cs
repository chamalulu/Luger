using System;

namespace Luger.Configuration.CommandLine.Specifications
{
    public record VerbSpecification(
        SpecificationName Name,
        CommandLineSpecification CommandLineSpecification,
        StringComparison NameComparison = StringComparison.OrdinalIgnoreCase)
        : NamedSpecification(Name);
}
