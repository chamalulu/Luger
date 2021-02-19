using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    public record OptionNode(string Name, string? Value = null);

    public record VerbNode(
        string Name,
        ImmutableList<OptionNode> Options,
        ImmutableList<VerbNode> Verbs,
        ImmutableList<ArgumentNode> Arguments)
    {
        public virtual bool Equals(VerbNode? other) =>

            other is not null &&
                Name.Equals(other.Name) &&
                Options.SequenceEqual(other.Options) &&
                Verbs.SequenceEqual(other.Verbs) &&
                Arguments.SequenceEqual(other.Arguments);

        public override int GetHashCode()
        {
            var hc = new HashCode();

            hc.Add(Name);
            Options.ForEach(hc.Add);
            Verbs.ForEach(hc.Add);
            Arguments.ForEach(hc.Add);

            return hc.ToHashCode();
        }

    }

    public record ArgumentNode(string Name, string Value);

    public record CommandLineNode(
        ImmutableList<OptionNode> Options,
        ImmutableList<VerbNode> Verbs,
        ImmutableList<ArgumentNode> Arguments)
    {
        public virtual bool Equals(CommandLineNode? other) =>

            other is not null &&
                Options.SequenceEqual(other.Options) &&
                Verbs.SequenceEqual(other.Verbs) &&
                Arguments.SequenceEqual(other.Arguments);

        public override int GetHashCode()
        {
            var hc = new HashCode();

            Options.ForEach(hc.Add);
            Verbs.ForEach(hc.Add);
            Arguments.ForEach(hc.Add);

            return hc.ToHashCode();
        }
    }

    public static class CommandLineNodes
    {
        public delegate void SetKeyValue(string key, string value);

        public static void Collect(this OptionNode optionNode, string prefix, SetKeyValue setKeyValue) =>

            setKeyValue(
                key: ConfigurationPath.Combine(prefix, optionNode.Name),
                value: optionNode.Value ?? bool.TrueString);

        public static void Collect(this IEnumerable<OptionNode> optionNodes, string prefix, SetKeyValue setKeyValue)
        {
            foreach (var node in optionNodes)
            {
                node.Collect(prefix, setKeyValue);
            }
        }

        public static void Collect(this VerbNode verbNode, string prefix, SetKeyValue setKeyValue)
        {
            prefix = ConfigurationPath.Combine(prefix, verbNode.Name);

            if (verbNode.Options.Any() || verbNode.Verbs.Any() || verbNode.Arguments.Any())
            {
                verbNode.Options.Collect(prefix, setKeyValue);
                verbNode.Verbs.Collect(prefix, setKeyValue);
                verbNode.Arguments.Collect(prefix, setKeyValue);
            }
            else
            {
                setKeyValue(prefix, bool.TrueString);
            }
        }

        public static void Collect(this IEnumerable<VerbNode> verbNodes, string prefix, SetKeyValue setKeyValue)
        {
            foreach (var node in verbNodes)
            {
                node.Collect(prefix, setKeyValue);
            }
        }

        public static void Collect(this ArgumentNode argumentNode, string prefix, SetKeyValue setKeyValue) =>

            setKeyValue(ConfigurationPath.Combine(prefix, argumentNode.Name), argumentNode.Value);

        public static void Collect(this IEnumerable<ArgumentNode> argumentNodes, string prefix, SetKeyValue setKeyValue)
        {
            foreach (var node in argumentNodes)
            {
                node.Collect(prefix, setKeyValue);
            }
        }

        public static void Collect(this CommandLineNode commandLineNode, string prefix, SetKeyValue setKeyValue)
        {
            commandLineNode.Options.Collect(prefix, setKeyValue);
            commandLineNode.Verbs.Collect(prefix, setKeyValue);
            commandLineNode.Arguments.Collect(prefix, setKeyValue);
        }

    }
}
