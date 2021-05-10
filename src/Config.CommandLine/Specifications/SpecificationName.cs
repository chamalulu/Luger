using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Luger.Configuration.CommandLine.Specifications
{
    public sealed record SpecificationName
    {
        private static readonly Regex ValueRex = new(@"^\p{L}\w*$");

        private string Value { get; init; }

        public SpecificationName(string value)

            => Value = ValueRex.IsMatch(value)
                ? value
                : throw new ArgumentException(null, nameof(value));

        public static implicit operator string(SpecificationName specificationName) => specificationName.Value;

        public bool Equals(SpecificationName other, StringComparison comparisonType)

            => string.Equals(Value, other.Value, comparisonType);

        public int GetHashCode(StringComparison comparisonType) => Value.GetHashCode(comparisonType);

        public override string ToString() => $"\"{Value}\"";
    }

    public class SpecificationNameEqualityComparer : EqualityComparer<SpecificationName>
    {
        private readonly StringComparison _comparisonType;

        public SpecificationNameEqualityComparer(StringComparison comparisonType) => _comparisonType = comparisonType;

        public override bool Equals(SpecificationName? x, SpecificationName? y)

            => x is null ? y is null : y is SpecificationName && x.Equals(y, _comparisonType);

        public override int GetHashCode([DisallowNull] SpecificationName obj) => obj.GetHashCode(_comparisonType);
    }
}
