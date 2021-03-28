using System.Collections.Immutable;

namespace Luger.Configuration.CommandLine
{
    /// <summary>
    /// Parse tree node representing an ordered list of nodes.
    /// </summary>
    /// <typeparam name="T">Type of nodes in list.</typeparam>
    public sealed partial record ListNode<T>(ImmutableList<T> List)
    {
        /// <summary>
        /// The empty list.
        /// </summary>
        public static readonly ListNode<T> Empty = new(ImmutableList<T>.Empty);
    }

    /// <summary>
    /// Parse tree node representing an unordered set of nodes.
    /// </summary>
    /// <typeparam name="T">Type of nodes in set.</typeparam>
    public sealed partial record SetNode<T>(ImmutableHashSet<T> Set);

    /// <summary>
    /// Base class for named parse tree nodes.
    /// </summary>
    public abstract partial record NamedNode(string Name);

    /// <summary>
    /// Parse tree node representing a flag.
    /// </summary>
    public partial record FlagNode(string Name, string Value = "True") : NamedNode(Name);

    /// <summary>
    /// Parse tree node representing an argument.
    /// </summary>
    public partial record ArgumentNode(string Name, string Value) : NamedNode(Name);

    /// <summary>
    /// Parse tree node representing a multi-argument. I.e. an indexed list of equally named arguments.
    /// </summary>
    /// <remarks>
    /// A multi-argument node can only be the last node in a list node of argument nodes.
    /// </remarks>
    public partial record MultiArgumentNode(string Name, int Index, string Value) : ArgumentNode(Name, Value);

    /// <summary>
    /// Parse tree node representing a verb. A verb has a name and may have multiple flags.
    /// </summary>
    public partial record VerbNode(string Name, ListNode<FlagNode> Flags) : NamedNode(Name);

    /// <summary>
    /// Parse tree node representing a verb with a subverb. A verb with a subverb has a name and may have multiple flags. 
    /// </summary>
    public partial record VerbNodeWithVerb(string Name, ListNode<FlagNode> Flags, VerbNode Verb) : VerbNode(Name, Flags);

    /// <summary>
    /// Parse tree node representing a verb with arguments. A verb with arguments has a name and may have mutiple flags and
    /// arguments.
    /// </summary>
    public partial record VerbNodeWithArguments(string Name, ListNode<FlagNode> Flags, ListNode<ArgumentNode> Arguments)
        : VerbNode(Name, Flags);

    /// <summary>
    /// Parse tree node representing a command line. A command line may have multiple flags.
    /// </summary>
    public partial record CommandLineNode(ListNode<FlagNode> Flags);

    /// <summary>
    /// Parse tree node representing a command line with a verb. A command line with a verb may have multiple flags.
    /// </summary>
    public partial record CommandLineNodeWithVerb(ListNode<FlagNode> Flags, VerbNode Verb) : CommandLineNode(Flags);

    /// <summary>
    /// Parse tree node representing a command line with arguments. A command line with arguments may have multiple flags and
    /// arguments.
    /// </summary>
    public partial record CommandLineNodeWithArguments(ListNode<FlagNode> Flags, ListNode<ArgumentNode> Arguments)
        : CommandLineNode(Flags);
}
