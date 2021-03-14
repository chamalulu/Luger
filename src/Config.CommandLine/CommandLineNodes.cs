using System.Collections.Immutable;

namespace Luger.Configuration.CommandLine
{
    public sealed partial record ListNode<T>(ImmutableList<T> List)
    {
        public static readonly ListNode<T> Empty = new(ImmutableList<T>.Empty);
    }

    public sealed partial record SetNode<T>(ImmutableHashSet<T> Set);

    public abstract partial record NamedNode(string Name);

    /// <summary>
    /// Parse tree node representing a flag.
    /// </summary>
    public partial record FlagNode(string Name, string Value = "True") : NamedNode(Name);

    public partial record ArgumentNode(string Name, string Value) : NamedNode(Name);

    public partial record MultiArgumentNode(string Name, int Index, string Value) : ArgumentNode(Name, Value);

    /// <summary>
    /// Parse tree node representing a verb. A verb has a name, flag* and ((sub)verb* | argument*)
    /// </summary>
    public partial record VerbNode(string Name, ListNode<FlagNode> Flags) : NamedNode(Name);

    public partial record VerbNodeWithVerb(string Name, ListNode<FlagNode> Flags, VerbNode Verb) : VerbNode(Name, Flags);

    public partial record VerbNodeWithArguments(string Name, ListNode<FlagNode> Flags, ListNode<ArgumentNode> Arguments)
        : VerbNode(Name, Flags);

    public partial record CommandLineNode(ListNode<FlagNode> Flags);

    public partial record CommandLineNodeWithVerb(ListNode<FlagNode> Flags, VerbNode Verb) : CommandLineNode(Flags);

    public partial record CommandLineNodeWithArguments(ListNode<FlagNode> Flags, ListNode<ArgumentNode> Arguments)
        : CommandLineNode(Flags);
}
