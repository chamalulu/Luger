using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Luger.Extensions.Configuration.CommandLine
{
    public record ParseState(ImmutableQueue<TokenBase> Tokens)
    {
        public static readonly ParseState Empty = new ParseState(ImmutableQueue<TokenBase>.Empty);

        public (ParseState state, TToken? token) Accept<TToken>(Func<TToken, bool>? predicate = null) where TToken : TokenBase =>

            Tokens.Any() && Tokens.PeekRef() is TToken t && (predicate?.Invoke(t) ?? true)
                ? (this with { Tokens = Tokens.Dequeue() }, t)
                : (this, null);
    }

    public delegate ParseResult<TResult> ParseDelegate<TResult>(ParseState state);

    public readonly struct CommandLineParser<TResult>
    {
        public CommandLineParser(ParseDelegate<TResult> parse) => Parse = parse;

        public ParseDelegate<TResult> Parse { get; }

        public static CommandLineParser<TResult> operator |(
            CommandLineParser<TResult> p1,
            CommandLineParser<TResult> p2) =>

            p1.Or(p2);

        public static CommandLineParser<ImmutableList<TResult>> operator &(
            CommandLineParser<TResult> left,
            CommandLineParser<TResult> right) =>

            from l in left from r in right select ImmutableList.Create(l, r);


        public static CommandLineParser<ImmutableList<TResult>> operator &(
            CommandLineParser<ImmutableList<TResult>> acc,
            CommandLineParser<TResult> p) =>

            from rs in acc from r in p select rs.Add(r);
    }

    public static class CommandLineParser
    {
        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which unconditionally succeed with given result and no change in
        ///  state.
        /// </summary>
        public static CommandLineParser<TResult> True<TResult>(TResult value) => new(state => ParseResult.Success(value, state));

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which unconditionally fail.
        /// </summary>
        public static CommandLineParser<TResult> False<TResult>(string message) =>

            new(state => ParseResult.Failure<TResult>(message, state));

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse both <paramref name="parser"/> and
        ///  <paramref name="alternative"/> and return the union of their results.
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
        ///  sequence and return a tuple of the results.
        /// </summary>
        public static CommandLineParser<(TLeft left, TRight right)> And<TLeft, TRight>(
            this CommandLineParser<TLeft> left,
            CommandLineParser<TRight> right) =>

            from lr in left
            from rr in right
            select (lr, rr);

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse all of the <paramref name="parsers"/> and returns the
        ///  union of their results.
        /// </summary>
        public static CommandLineParser<TResult> Any<TResult>(this IEnumerable<CommandLineParser<TResult>> parsers) =>

            parsers.Aggregate(
                seed: False<TResult>("No successful alternative"),
                func: Or);

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse all of the <paramref name="parsers"/> in sequence and
        ///  return the sequence of result values.
        /// </summary>
        public static CommandLineParser<ImmutableList<TResult>> All<TResult>(
            this IEnumerable<CommandLineParser<TResult>> parsers) =>

            parsers.Aggregate(
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
            ParseResult<TResult> parse(ParseState state) =>

                from sourceValue in source.Parse(state)
                from nextValue in selector(sourceValue).Parse
                select projection(sourceValue, nextValue);

            return new(parse);
        }

        private static ParseResult<ImmutableList<TResult>> ZeroOrMoreStep<TResult>(
            ParseResult<ImmutableList<TResult>> results,
            CommandLineParser<TResult> parser)
        {
            var next = from success in results
                       from nextSuccess in parser.Parse
                       select success.Add(nextSuccess);

            return next.Successes.Count > 0
                ? ZeroOrMoreStep(next, parser)
                : results;
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse <paramref name="parser"/> zero or more times in sequence
        ///  and return the union off all results.
        /// </summary>
        public static CommandLineParser<ImmutableList<TResult>> ZeroOrMore<TResult>(this CommandLineParser<TResult> parser)
        {
            ParseResult<ImmutableList<TResult>> parse(ParseState state) =>

                ZeroOrMoreStep(ParseResult.Success(ImmutableList.Create<TResult>(), state), parser);

            return new(parse);
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse an argument according to given
        ///  <paramref name="argumentSpecification"/>.
        /// </summary>
        public static CommandLineParser<ArgumentNode> ArgumentParser(ArgumentSpecification argumentSpecification) =>

            new CommandLineParser<ArgumentNode>(state =>

                state.Accept<ArgumentToken>() is (ParseState nextState, ArgumentToken token)
                    ? ParseResult.Success(new ArgumentNode(argumentSpecification.Name, token.Value), nextState)
                    : ParseResult.Failure<ArgumentNode>($"Expected {argumentSpecification}", state));

        public static readonly CommandLineParser<string> AnonymousArgumentParser = new CommandLineParser<string>(state =>

            state.Accept<ArgumentToken>() is (ParseState nextState, ArgumentToken token)
                ? ParseResult.Success(token.Value, nextState)
                : ParseResult.Failure<string>("Expected Argument", state));

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a list of arguments in sequence according to the given
        ///  <paramref name="argumentSpecifications"/>.
        /// </summary>
        /// <param name="argumentSpecifications"></param>
        /// <returns></returns>
        public static CommandLineParser<ImmutableList<ArgumentNode>> ArgumentListParser(
            IEnumerable<ArgumentSpecification> argumentSpecifications) =>

            argumentSpecifications.Select(ArgumentParser).All();

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse an option according to given
        ///  <paramref name="optionSpecification"/>.
        /// </summary>
        public static CommandLineParser<OptionNode> OptionParser(OptionSpecificationBase optionSpecification)
        {
            ParseDelegate<OptionNode> parse<TToken>(Func<TToken, bool> predicate) where TToken : TokenBase => state =>

                state.Accept<TToken>(predicate) is (ParseState nextState, TToken _)
                    ? ParseResult.Success(new OptionNode(optionSpecification.Name), nextState)
                    : ParseResult.Failure<OptionNode>($"Expected {optionSpecification}", state);

            var optionNameParser = new CommandLineParser<OptionNode>(optionSpecification switch
            {
                ShortOptionSpecification spec => parse<ShortOptionToken>(token => token.Flag == spec.Flag),
                LongOptionSpecification spec => parse<LongOptionToken>(token => token.Key == spec.Key),
                _ => throw new ArgumentException($"Unexpected option specification type {optionSpecification.GetType()}.")
            });

            return optionSpecification.TakesArgument
                ? from optionName in optionNameParser
                  from argument in AnonymousArgumentParser
                  select optionName with { Value = argument }
                : optionNameParser;
        }

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse zero or more options in sequence according to any of the
        ///  given <paramref name="optionSpecifications"/>.
        /// </summary>
        public static CommandLineParser<ImmutableList<OptionNode>> OptionSetParser(
            IEnumerable<OptionSpecificationBase> optionSpecifications) =>

            optionSpecifications.Select(OptionParser).Any().ZeroOrMore();

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a verb according to given
        ///  <paramref name="verbSpecification"/>. 
        /// </summary>
        public static CommandLineParser<VerbNode> VerbParser(VerbSpecification verbSpecification) =>

            from name in AnonymousArgumentParser
            from options in OptionSetParser(verbSpecification.Options)
            from verbs in VerbSetParser(verbSpecification.Verbs)
            from arguments in ArgumentListParser(verbSpecification.Arguments)
            select new VerbNode(name, options, verbs, arguments);

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse zero or more verbs in sequence according to any of the
        ///  given <paramref name="verbSpecifications"/>.
        /// </summary>
        public static CommandLineParser<ImmutableList<VerbNode>> VerbSetParser(IEnumerable<VerbSpecification> verbSpecifications) =>

            verbSpecifications.Select(VerbParser).Any().ZeroOrMore();

        /// <summary>
        /// Create a <see cref="CommandLineParser{TResult}"/> which parse a command line according to given
        ///  <paramref name="commandLineSpecification"/>.
        /// </summary>
        public static CommandLineParser<CommandLineNode> Create(CommandLineSpecification commandLineSpecification) =>

            from options in OptionSetParser(commandLineSpecification.Options)
            from verbs in VerbSetParser(commandLineSpecification.Verbs)
            from arguments in ArgumentListParser(commandLineSpecification.Arguments)
            select new CommandLineNode(options, verbs, arguments);
    }
}
