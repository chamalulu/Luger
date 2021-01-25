using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Luger.Extensions.Configuration.CommandLine
{
    public class CommandLineConfigurationProvider : ConfigurationProvider
    {
        private readonly CommandLineSpecification _commandLineSpecification;

        public CommandLineConfigurationProvider(IEnumerable<string>? args, CommandLineSpecification? commandLineSpecification = null)
        {
            Args = args?.ToArray() ?? throw new ArgumentNullException(nameof(args));

            _commandLineSpecification = commandLineSpecification ?? CommandLineSpecification.Empty;
        }

        protected IEnumerable<string> Args { get; private init; }

        //protected enum ErrorEnum { None = 0, UnexpectedEndOfTokens, UnexpectedTokenType }

        //protected record LoadState(
        //    ImmutableQueue<TokenBase> Tokens,   // Remaining tokens to load from
        //    ImmutableStack<string> Path,    // Configuration path (as stack of names)
        //    ImmutableDictionary<string, string> Data,   // Building configuration data
        //    ErrorEnum Error = ErrorEnum.None)   // Load error
        //{
        //    public LoadState DequeueAndRun<TToken>(Func<LoadState, TToken, LoadState> func) where TToken : TokenBase
        //    {
        //        if (Tokens.IsEmpty)
        //        {
        //            return this with { Error = ErrorEnum.UnexpectedEndOfTokens };
        //        }
        //        else if (Tokens.PeekRef() is TToken dequeueingToken)
        //        {
        //            return func(this with { Tokens = Tokens.Dequeue() }, dequeueingToken);
        //        }
        //        else
        //        {
        //            return this with { Error = ErrorEnum.UnexpectedTokenType };
        //        }
        //    }

        //    public LoadState DequeueAndRun<TSpecification, TToken>(Func<LoadState, TSpecification, TToken, LoadState> func, TSpecification specification)
        //        where TSpecification : SpecificationBase
        //        where TToken : TokenBase
        //    {
        //        if (Tokens.IsEmpty)
        //        {
        //            return this with { Error = ErrorEnum.UnexpectedEndOfTokens };
        //        }
        //        else if (Tokens.PeekRef() is TToken dequeueingToken)
        //        {
        //            return func(this with { Tokens = Tokens.Dequeue() }, specification, dequeueingToken);
        //        }
        //        else
        //        {
        //            return this with { Error = ErrorEnum.UnexpectedTokenType };
        //        }
        //    }

        //    public LoadState PushName(string name) => this with { Path = Path.Push(name) };

        //    public LoadState PushName(NamedSpecificationBase specification) => PushName(specification.Name);

        //    public LoadState SetDataItem(string value)
        //    {
        //        var key = string.Join(':', Path);

        //        return this with { Data = Data.SetItem(key, value) };
        //    }

        //    public LoadState SetDataItem(TokenBase token) => SetDataItem(token.ToString());

        //    public LoadState SetDataItem() => SetDataItem(bool.TrueString);
        //}

        //protected static bool TryDequeue<TToken>(ref LoadState state, [NotNullWhen(true)] out TToken? token) where TToken : TokenBase
        //{
        //    (state, token) = state.Tokens.IsEmpty
        //        ? (state with { Error = ErrorEnum.UnexpectedEndOfTokens }, null)
        //        : state.Tokens.Peek() is TToken dequeueingToken
        //            ? (state with { Tokens = state.Tokens.Dequeue() }, dequeueingToken)
        //            : (state with { Error = ErrorEnum.UnexpectedTokenType }, null);

        //    return token is TToken;
        //}

        //protected static LoadState Load(LoadState state, ArgumentSpecification argumentSpecification)
        //{
        //    LoadState func(LoadState state, ArgumentToken argumentToken) =>
        //        state.PushName(argumentSpecification)
        //             .SetDataItem(argumentToken);

        //    return state.DequeueAndRun<ArgumentToken>(func);
        //}

        //protected static LoadState Load(LoadState state, ShortOptionSpecification shortOptionSpecification)
        //{
        //    LoadState func(LoadState state, ShortOptionToken shortOptionToken)
        //    {
        //        state = state.PushName(shortOptionSpecification);

        //        return shortOptionSpecification.Arguments.Count == 0
        //            ? state.SetDataItem()
        //            : shortOptionSpecification.Arguments.Aggregate(state, Load);
        //    }

        //    return state.DequeueAndRun<ShortOptionToken>(func);
        //}

        //protected static LoadState Load(LoadState state, LongOptionSpecification longOptionSpecification)
        //{
        //    LoadState func(LoadState state, LongOptionToken longOptionToken)
        //    {
        //        state = state.PushName(longOptionSpecification);

        //        return longOptionSpecification.Arguments.Count == 0
        //    }
        //}

        //protected static LoadState Load(LoadState state, SpecificationBase specification) =>
        //    specification switch
        //    {
        //        ArgumentSpecification argSpec => Load(state, argSpec),
        //        ShortOptionSpecification shortOptSpec => Load(state, shortOptSpec),
        //        LongOptionSpecification longOptSpec => Load(state, longOptSpec),
        //        VerbSpecification verbSpec => Load(state, verbSpec),
        //        CommandLineSpecification cmdLineSpec => Load(state, cmdLineSpec),
        //        _ => throw new NotImplementedException()
        //    };

        public override void Load()
        {
            // Create parser from specification
            var parser = CommandLineParser.Create(_commandLineSpecification);

            // Tokenize arguments
            var tokens = ImmutableQueue.CreateRange(Args.SelectMany(CommandLineTokenizer.Tokenize));

            var state = new ParseState(tokens);

            // Parse tokens
            // TODO: errorhandling
            var (successes, failures) = parser.Parse(state);
            var value = successes.Single().value; // Throws on ambiguous parsing

            // Collect configuration items
            value.Collect("", Set);
        }
    }
}
