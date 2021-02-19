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

        public static void Collect(this OptionNode optionNode, SetKeyValue setKeyValue, ImmutableList<string>? path = null)
        {
            var key = path is null
                ? optionNode.Name
                : ConfigurationPath.Combine(path.Add(optionNode.Name));

            var value = optionNode.Value ?? bool.TrueString;

            setKeyValue(key, value);
        }

        public static void Collect(
            this IEnumerable<OptionNode> optionNodes,
            SetKeyValue setKeyValue,
            ImmutableList<string>? path = null)
        {
            foreach (var node in optionNodes)
            {
                node.Collect(setKeyValue, path);
            }
        }

        public static void Collect(this VerbNode verbNode, SetKeyValue setKeyValue, ImmutableList<string>? path = null)
        {
            path = path?.Add(verbNode.Name) ?? ImmutableList.Create(verbNode.Name);

            if (verbNode.Options.Any() || verbNode.Verbs.Any() || verbNode.Arguments.Any())
            {
                verbNode.Options.Collect(setKeyValue, path);
                verbNode.Verbs.Collect(setKeyValue, path);
                verbNode.Arguments.Collect(setKeyValue, path);
            }
            else
            {
                setKeyValue(ConfigurationPath.Combine(path), bool.TrueString);
            }
        }

        public static void Collect(
            this IEnumerable<VerbNode> verbNodes,
            SetKeyValue setKeyValue,
            ImmutableList<string>? path = null)
        {
            foreach (var node in verbNodes)
            {
                node.Collect(setKeyValue, path);
            }
        }

        public static void Collect(this ArgumentNode argumentNode, SetKeyValue setKeyValue, ImmutableList<string>? path = null)
        {
            var key = path is null
                ? argumentNode.Name
                : ConfigurationPath.Combine(path.Add(argumentNode.Name));

            setKeyValue(key, argumentNode.Value);
        }

        public static void Collect(
            this IEnumerable<ArgumentNode> argumentNodes,
            SetKeyValue setKeyValue,
            ImmutableList<string>? path = null)
        {
            foreach (var node in argumentNodes)
            {
                node.Collect(setKeyValue, path);
            }
        }

        public static void Collect(
            this CommandLineNode commandLineNode,
            SetKeyValue setKeyValue,
            ImmutableList<string>? path = null)
        {
            commandLineNode.Options.Collect(setKeyValue, path);
            commandLineNode.Verbs.Collect(setKeyValue, path);
            commandLineNode.Arguments.Collect(setKeyValue, path);
        }

    }
}
