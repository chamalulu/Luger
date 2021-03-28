using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Luger.Configuration.CommandLine
{
    /// <summary>
    /// Common interface for nodes subject to collection of configuration items.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Collect configuration items from node.
        /// </summary>
        /// <param name="path">List of configuration section segments to use as root for configuration items.</param>
        /// <returns>Sequence of configuration items.</returns>
        IEnumerable<(string key, string value)> Collect(ImmutableList<string> path);
    }

    public sealed partial record ListNode<T> : INode where T : INode
    {
        public IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)

            => List.SelectMany(node => node.Collect(path));
    }

    public sealed partial record SetNode<T> : INode where T : notnull, INode
    {
        public IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)

            => Set.SelectMany(node => node.Collect(path));
    }

    public abstract partial record NamedNode : INode
    {
        /// <summary>
        /// Produce configuration item key by combining configuration section <paramref name="path"/> with <see cref="Name"/>
        /// </summary>
        /// <param name="path">List of configuration section segments to use as root for configuration items.</param>
        /// <remarks>Subclasses may override this method to customize their specific key.</remarks>
        /// <returns>Configuration item key.</returns>
        protected virtual string GetKey(ImmutableList<string> path) => ConfigurationPath.Combine(path.Append(Name));

        /// <summary>
        /// Produce configuration item value.
        /// </summary>
        /// <remarks>Subclasses should override this method to produce their specific value.</remarks>
        /// <returns>Configuration item value.</returns>
        protected virtual string GetValue() => bool.TrueString;

        public virtual IEnumerable<(string key, string value)> Collect(ImmutableList<string> path)
        {
            yield return (GetKey(path), GetValue());
        }
    }

    public partial record FlagNode
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
        /// <summary>
        /// Collect configuration items from child nodes.
        /// </summary>
        /// <remarks>
        /// Subclasses should override this method to customize their collection of child node configuration items.
        /// </remarks>
        /// <inheritdoc cref="INode.Collect(ImmutableList{string})"/>
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
