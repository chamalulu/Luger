using System;
using System.Text.RegularExpressions;

namespace Luger.Configuration.CommandLine.Specifications
{
    public record LongFlagName
    {
        private static readonly Regex ValueRex = new(@"^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$");

        private string Value { get; init; }

        public LongFlagName(string value)

            => Value = ValueRex.IsMatch(value)
                ? value
                : throw new ArgumentException(null, nameof(value));

        public static implicit operator string(LongFlagName longFlagName) => longFlagName.Value;

        public override string ToString() => $"\"{Value}\"";
    }
}
