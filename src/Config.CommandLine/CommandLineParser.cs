using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Luger.Configuration.CommandLine
{
    /// <summary>
    /// State of parsing. Keeps track of token queue and provide the accept method.
    /// </summary>
    public record ParseState(ImmutableQueue<TokenBase> Tokens)
    {
        /// <summary>
        /// The empty state.
        /// </summary>
        public static readonly ParseState Empty = new(ImmutableQueue<TokenBase>.Empty);

        /// <summary>
        /// Transition into next state if current token is accepted. Otherwise; a noop.
        /// </summary>
        /// <typeparam name="TToken">Type of acceptable token.</typeparam>
        /// <param name="predicate">Optional predicate to constrain acceptance of token.</param>
        /// <returns>Next parse state and accepted token if accepted. Otherwise; this parse state and no token.</returns>
        public (ParseState state, TToken? token) Accept<TToken>(Func<TToken, bool>? predicate = null) where TToken : TokenBase

            => Tokens.Any() && Tokens.PeekRef() is TToken t && (predicate?.Invoke(t) ?? true)
                ? (this with { Tokens = Tokens.Dequeue() }, t)
                : (this, null);
    }

    /// <summary>
    /// Delegate used by <see cref="CommandLineParser{TResult}"/> for parsing.
    /// </summary>
    /// <typeparam name="TResult">Type of parsed result.</typeparam>
    /// <param name="state">Current parse state.</param>
    /// <returns>Parse result.</returns>
    public delegate ParseResult<TResult> ParseDelegate<TResult>(ParseState state);

    /// <summary>
    /// Command line parser.
    /// </summary>
    /// <remarks>
    /// Mostly just a container of <see cref="ParseDelegate{TResult}"/> providing some binary operators.
    /// This design is debatable.
    /// </remarks>
    /// <typeparam name="TResult">Type of parse result.</typeparam>
    public readonly struct CommandLineParser<TResult>
    {
        /// <summary>
        /// Create a new command line parser from given parse delegate.
        /// </summary>
        /// <param name="parse"></param>
        public CommandLineParser(ParseDelegate<TResult> parse) => Parse = parse;

        /// <summary>
        /// Parse delegate.
        /// </summary>
        public ParseDelegate<TResult> Parse { get; }

        /// <summary>
        /// Alias for <see cref="CommandLineParser.Or"/> operator.
        /// </summary>
        /// <param name="p1">One command line parser operand.</param>
        /// <param name="p2">Other command line parser operand.</param>
        /// <returns>A command line parser with result of the union of both command line parser operands results.</returns>
        /// <example><code>XOrYParser = XParser | YParser</code></example>
        public static CommandLineParser<TResult> operator |(
            CommandLineParser<TResult> p1,
            CommandLineParser<TResult> p2)

            => p1.Or(p2);

        /// <summary>
        /// Alias for <see cref="CommandLineParser.SelectMany"/> operator.
        /// </summary>
        /// <param name="left">Left command line parser operand.</param>
        /// <param name="right">Right command line parser operand.</param>
        /// <returns>
        /// A command line parser with result of list with two elements corresponding to the results of the command line parser
        /// operands parsed right after left.
        /// </returns>
        /// <example><code>ListOf3XParser = XParser &amp; XParser &amp; XParser;</code></example>
        /// <remarks>Used as initial operator for constructing a parser parsing a sequence of equally typed results.</remarks>
        public static CommandLineParser<ImmutableList<TResult>> operator &(
            CommandLineParser<TResult> left,
            CommandLineParser<TResult> right)

            => from l in left from r in right select ImmutableList.Create(l, r);

        /// <summary>
        /// Alias for <see cref="CommandLineParser.SelectMany"/> operator where <paramref name="acc"/> command line parser operand
        /// has a result of list of <see cref="TResult"/>.
        /// </summary>
        /// <param name="acc">Accumulator command line parser operand.</param>
        /// <param name="next">Next command line parser operand.</param>
        /// <returns>
        /// A command line parser with result of list of <paramref name="acc"/> parser with result of <paramref name="next"/> parser
        /// appended.
        /// </returns>
        /// <example><code>ListOf3XParser = XParser &amp; XParser &amp; XParser;</code></example>
        /// <remarks>Used as following operators for constructing a parser parsing a sequence of equally typed results.</remarks>
        public static CommandLineParser<ImmutableList<TResult>> operator &(
            CommandLineParser<ImmutableList<TResult>> acc,
            CommandLineParser<TResult> next)

            => from rs in acc
               from r in next
               select rs.Add(r);
    }

    /// <summary>
    /// Extension methods and factories for <see cref="CommandLineParser{TResult}"/>
    /// </summary>
    public static class CommandLineParser
    {
        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which unconditionally succeed with given result and no change in
        /// state.
        /// </summary>
        public static CommandLineParser<TResult> True<TResult>(TResult value) => new(state => ParseResult.Success(value, state));

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which unconditionally fail.
        /// </summary>
        public static CommandLineParser<TResult> False<TResult>(string message)

            => new(state => ParseResult.Failure<TResult>(message, state));

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse both <paramref name="parser"/> and
        /// <paramref name="alternative"/> and return the union of their results.
        /// </summary>
        public static CommandLineParser<TResult> Or<TResult>(
            this CommandLineParser<TResult> parser,
            CommandLineParser<TResult> alternative)
        {
            ParseResult<TResult> parse(ParseState state)
            {
                var parserResult = parser.Parse(state);
                var alternativeResult = alternative.Parse(state);

                var successes = ImmutableList.CreateRange(parserResult.Successes.Concat(alternativeResult.Successes));
                var failures = successes.Count > 0
                    ? ImmutableList.Create<(string, ParseState)>()
                    : ImmutableList.CreateRange(parserResult.Failures.Concat(alternativeResult.Failures));

                return new(successes, failures);
            }

            return new(parse);
        }


        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse <paramref name="left"/> and <paramref name="right"/> in
        /// sequence and return a tuple of the results.
        /// </summary>
        public static CommandLineParser<(TLeft left, TRight right)> And<TLeft, TRight>(
            this CommandLineParser<TLeft> left,
            CommandLineParser<TRight> right)

            => from lr in left
               from rr in right
               select (lr, rr);

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse all of the <paramref name="parsers"/> and returns the
        /// union of their results.
        /// </summary>
        public static CommandLineParser<TResult> Any<TResult>(this IEnumerable<CommandLineParser<TResult>> parsers)

            => parsers.Aggregate(
                seed: False<TResult>("No successful alternative"),
                func: Or);

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse all of the <paramref name="parsers"/> in sequence and
        /// return a list of result values.
        /// </summary>
        public static CommandLineParser<ImmutableList<TResult>> All<TResult>(
            this IEnumerable<CommandLineParser<TResult>> parsers)

            => parsers.Aggregate(
                seed: True(ImmutableList.Create<TResult>()),
                func: (acc, p) => acc & p);

        /// <summary>
        /// Support for LINQ map over <see cref="CommandLineParser{TResult}"/>.
        /// </summary>
        public static CommandLineParser<TResult> Select<TSource, TResult>(
            this CommandLineParser<TSource> source,
            Func<TSource, TResult> selector)
        {
            ParseResult<TResult> parse(ParseState state) => source.Parse(state).Select(selector);

            return new(parse);
        }

        /// <summary>
        /// Support for LINQ bind over <see cref="CommandLineParser{TResult}"/>.
        /// </summary>
        public static CommandLineParser<TResult> SelectMany<TSource, TNext, TResult>(
            this CommandLineParser<TSource> source,
            Func<TSource, CommandLineParser<TNext>> selector,
            Func<TSource, TNext, TResult> projection)
        {
            ParseResult<TResult> parse(ParseState state)

                => from sourceValue in source.Parse(state)
                   from nextValue in selector(sourceValue).Parse
                   select projection(sourceValue, nextValue);

            return new(parse);
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which greedily parse <paramref name="parser"/> zero or more times in
        /// sequence and return the union of all results.
        /// </summary>
        public static CommandLineParser<ImmutableList<TResult>> ZeroOrMore<TResult>(this CommandLineParser<TResult> parser)
        {
            ParseResult<ImmutableList<TResult>> step(ParseResult<ImmutableList<TResult>> results, CommandLineParser<TResult> parser)
            {
                var next =
                    from success in results
                    from nextSuccess in parser.Parse
                    select success.Add(nextSuccess);

                return next.Successes.Count > 0
                    ? step(next, parser)
                    : results;
            }

            ParseResult<ImmutableList<TResult>> parse(ParseState state)

                => step(ParseResult.Success(ImmutableList<TResult>.Empty, state), parser);

            return new(parse);
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse <paramref name="parser"/> zero or one time and return the
        /// union of both results.
        /// </summary>
        public static CommandLineParser<ImmutableList<TResult>> ZeroOrOne<TResult>(this CommandLineParser<TResult> parser)
        {
            var alternative = True(ImmutableList<TResult>.Empty);

            return parser.Select(ImmutableList.Create<TResult>).Or(alternative);
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse an argument according to given
        /// <paramref name="argumentSpecification"/>.
        /// </summary>
        public static CommandLineParser<ArgumentNode> ArgumentParser(ArgumentSpecification argumentSpecification)
        {
            ParseResult<ArgumentNode> parse(ParseState state)

                => state.Accept<ArgumentToken>() is (ParseState nextState, ArgumentToken token)
                    ? ParseResult.Success(new ArgumentNode(argumentSpecification.Name, token.Value), nextState)
                    : ParseResult.Failure<ArgumentNode>($"Expected {argumentSpecification}", state);

            return new(parse);
        }

        /// <summary>
        /// A <see cref="CommandLineParser{TResult}"/> which parse an anonymous argument as a string.
        /// </summary>
        public static readonly CommandLineParser<string> AnonymousArgumentParser = new(state

            => state.Accept<ArgumentToken>() is (ParseState nextState, ArgumentToken token)
                ? ParseResult.Success(token.Value, nextState)
                : ParseResult.Failure<string>("Expected Argument", state));

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a given literal argument as a string.
        /// </summary>
        /// <param name="literal">The literal to accept.</param>
        /// <param name="comparison">Optional string comparison.</param>
        public static CommandLineParser<string> LiteralArgumentParser(
            string literal,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            bool predicate(ArgumentToken token) => token.Value.Equals(literal, comparison);

            ParseResult<string> parse(ParseState state)

                => state.Accept<ArgumentToken>(predicate) is (ParseState nextState, ArgumentToken token)
                    ? ParseResult.Success(token.Value, nextState)
                    : ParseResult.Failure<string>($"Expected Argument '{literal}'", state);

            return new CommandLineParser<string>(parse);
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a list of multi arguments according to the given
        /// <paramref name="multiArgumentSpecification"/>.
        /// </summary>
        public static CommandLineParser<ListNode<MultiArgumentNode>> MultiArgumentParser(
            MultiArgumentSpecification multiArgumentSpecification)
        {
            static MultiArgumentNode toMultiArgumentNode(ArgumentNode node, int index)

                => new(node.Name, index, node.Value);

            return
                from argumentNodes in ArgumentParser(multiArgumentSpecification).ZeroOrMore()
                select new ListNode<MultiArgumentNode>(ImmutableList.CreateRange(argumentNodes.Select(toMultiArgumentNode)));
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a list of arguments in sequence according to the given
        /// <paramref name="argumentSpecifications"/>.
        /// </summary>
        public static CommandLineParser<ListNode<ArgumentNode>> ArgumentsParser(
            ImmutableList<ArgumentSpecification> argumentSpecifications)
        {
            static CommandLineParser<ListNode<ArgumentNode>> singleArgumentsParser(
                IEnumerable<ArgumentSpecification> argumentSpecifications)

                => argumentSpecifications.Select(ArgumentParser).All().Select(arguments => new ListNode<ArgumentNode>(arguments));

            if (argumentSpecifications.Count > 0 &&
                argumentSpecifications[^1] is MultiArgumentSpecification multiArgumentSpecification)
            {
                var singleArgumentSpecifications = argumentSpecifications.Take(argumentSpecifications.Count - 1);

                return from singleArgumentNodes in singleArgumentsParser(singleArgumentSpecifications)
                       from multiArgumentNodes in MultiArgumentParser(multiArgumentSpecification)
                       select singleArgumentNodes with { List = singleArgumentNodes.List.AddRange(multiArgumentNodes.List) };
            }
            else
            {
                return singleArgumentsParser(argumentSpecifications);
            }
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a flag according to given
        /// <paramref name="flagSpecification"/>.
        /// </summary>
        public static CommandLineParser<FlagNode> FlagParser(FlagSpecificationBase flagSpecification)
        {
            ParseDelegate<string> parseFlagName<TToken>(Func<TToken, bool> predicate) where TToken : TokenBase

                => state => state.Accept<TToken>(predicate) is (ParseState nextState, TToken _)
                    ? ParseResult.Success(flagSpecification.Name, nextState)
                    : ParseResult.Failure<string>($"Expected {flagSpecification}", state);

            var flagNameParser =
                new CommandLineParser<string>(parseFlagName<LongFlagToken>(token => token.Key == flagSpecification.LongName));

            if (flagSpecification.ShortName != default)
            {
                flagNameParser |= new CommandLineParser<string>(
                    parseFlagName<ShortFlagToken>(token => token.Flag == flagSpecification.ShortName));
            }

            return flagSpecification switch
            {
                FlagWithValueSpecification =>
                     from flagName in flagNameParser
                     from argument in AnonymousArgumentParser
                     select new FlagNode(flagName, argument),

                FlagSpecification fs =>
                     from flagName in flagNameParser
                     select new FlagNode(flagName, fs.Value),

                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse zero or more options in sequence according to any of the
        /// given <paramref name="optionSpecifications"/>.
        /// </summary>
        public static CommandLineParser<ListNode<FlagNode>> FlagsParser(
            IEnumerable<FlagSpecificationBase> optionSpecifications) =>

            optionSpecifications.Select(FlagParser).Any().ZeroOrMore().Select(flags => new ListNode<FlagNode>(flags));

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a verb according to given
        /// <paramref name="verbSpecification"/>. 
        /// </summary>
        public static CommandLineParser<VerbNode> VerbParser(VerbSpecification verbSpecification)
        {
            var verbNodeParser =
                from name in LiteralArgumentParser(verbSpecification.Name, verbSpecification.NameComparison)
                from flags in FlagsParser(verbSpecification.Flags)
                select new VerbNode(name, flags);

            return (verbSpecification.Verbs.IsEmpty, verbSpecification.Arguments.IsEmpty) switch
            {
                (true, true) => verbNodeParser,

                (false, true) => from verbNode in verbNodeParser
                                 from verb in VerbsParser(verbSpecification.Verbs)
                                 select verb.List.IsEmpty
                                    ? verbNode
                                    : new VerbNodeWithVerb(verbNode.Name, verbNode.Flags, verb.List[0]),

                (true, false) => from verbNode in verbNodeParser
                                 from arguments in ArgumentsParser(verbSpecification.Arguments)
                                 select (VerbNode)new VerbNodeWithArguments(verbNode.Name, verbNode.Flags, arguments),

                _ => throw new InvalidOperationException()
            };
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse zero or one verb according to any of the given
        /// <paramref name="verbSpecifications"/>.
        /// </summary>
        public static CommandLineParser<ListNode<VerbNode>> VerbsParser(ImmutableList<VerbSpecification> verbSpecifications) =>

            verbSpecifications.Select(VerbParser).Any().ZeroOrOne().Select(verb => new ListNode<VerbNode>(verb));

        public static CommandLineParser<ValueTuple> SentinelParser()
        {
            static ParseResult<ValueTuple> parse(ParseState state)

                => state.Tokens.IsEmpty
                    ? ParseResult.Success<ValueTuple>(default, state)
                    : state.Accept<SentinelToken>() is (ParseState nextState, SentinelToken)
                        ? ParseResult.Success<ValueTuple>(default, nextState)
                        : ParseResult.Failure<ValueTuple>($"Unexpected {state.Tokens.PeekRef()}", state);

            return new(parse);
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a command line according to given
        /// <paramref name="commandLineSpecification"/>.
        /// </summary>
        public static CommandLineParser<CommandLineNode> Create(CommandLineSpecification commandLineSpecification)
        {
            var commandLineParser = (commandLineSpecification.Verbs.IsEmpty, commandLineSpecification.Arguments.IsEmpty) switch
            {
                (true, true) => from flags in FlagsParser(commandLineSpecification.Flags)
                                select new CommandLineNode(flags),

                (false, true) => from flags in FlagsParser(commandLineSpecification.Flags)
                                 from verb in VerbsParser(commandLineSpecification.Verbs)
                                 select verb.List.IsEmpty
                                    ? new CommandLineNode(flags)
                                    : new CommandLineNodeWithVerb(flags, verb.List[0]),

                (true, false) => from flags in FlagsParser(commandLineSpecification.Flags)
                                 from arguments in ArgumentsParser(commandLineSpecification.Arguments)
                                 select (CommandLineNode)new CommandLineNodeWithArguments(flags, arguments),

                _ => throw new InvalidOperationException()
            };

            return from commandLine in commandLineParser
                   from _ in SentinelParser()
                   select commandLine;
        }
    }
}
