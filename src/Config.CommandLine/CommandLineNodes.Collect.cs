using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    public delegate void SetKeyValue(string key, string value);

    public interface INode
    {
        IEnumerable<(string key, string value)> Collect(ImmutableList<string> path);
    }

    public sealed partial record ListNode<T> : INode where T : INode
    {
        public IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)

            => List.SelectMany(node => node.Collect(path));
    }

    public sealed partial record SetNode<T> : INode where T : INode
    {
        public IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)

            => Set.SelectMany(node => node.Collect(path));
    }

    public abstract partial record NamedNode : INode
    {
        protected virtual string GetKey(ImmutableList<string> path) => ConfigurationPath.Combine(path.Append(Name));

        protected virtual string GetValue() => bool.TrueString;

        public virtual IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)
        {
            yield return (GetKey(path), GetValue());
        }
    }

    public partial record FlagNode
    {
    }

    public partial record FlagNodeWithValue
    {
        protected override string GetValue() => Value;
    }

    public partial record ArgumentNode
    {
        protected override string GetValue() => Value;
    }

    public partial record MultiArgumentNode
    {
        protected override string GetKey(ImmutableList<string> path)

            => ConfigurationPath.Combine(path.Append(Name).Append(Index.ToString()));
    }

    public partial record VerbNode
    {
        protected virtual IEnumerable<(string key, string value)> CollectChildItems(ImmutableList<string> path)

            => Flags.Collect(path);

        public override sealed IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)
        {
            var childItems = CollectChildItems(path.Add(Name)).ToArray();

            return childItems.Length > 0
                ? childItems
                : base.Collect(path);
        }
    }

    public partial record VerbNodeWithVerb
    {
        protected override IEnumerable<(string key, string value)> CollectChildItems(ImmutableList<string> path)

            => base.CollectChildItems(path).Concat(Verb.Collect(path));
    }

    public partial record VerbNodeWithArguments
    {
        protected override IEnumerable<(string key, string value)> CollectChildItems(ImmutableList<string> path)

            => base.CollectChildItems(path).Concat(Arguments.Collect(path));
    }

    public partial record CommandLineNode : INode
    {
        public virtual IEnumerable<(string key, string value)> Collect(ImmutableList<string> path) => Flags.Collect(path);
    }

    public partial record CommandLineNodeWithVerb
    {
        public override IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)

            => base.Collect(path).Concat(Verb.Collect(path));
    }

    public partial record CommandLineNodeWithArguments
    {
        public override IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)

            => base.Collect(path).Concat(Arguments.Collect(path));
    }
}
