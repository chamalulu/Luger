namespace Luger.Configuration.CommandLine.Specifications
{
    /// <summary>
    /// Argument specification.
    /// </summary>
    /// <remarks>
    /// Successful parsing yields an <see cref="ArgumentNode"/>
    /// </remarks>
    public record ArgumentSpecification(SpecificationName Name) : NamedSpecification(Name);

    /// <summary>
    /// Multi-argument specification.
    /// </summary>
    /// <remarks>
    /// Successful parsing yields zero or more indexed <see cref="MultiArgumentNode"/>s
    /// </remarks>
    public record MultiArgumentSpecification(SpecificationName Name) : NamedSpecification(Name);
}
