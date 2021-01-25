using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Luger.Extensions.Configuration.CommandLine
{
    public abstract record TokenBase(string Arg, Range Range);

    public abstract record OptionTokenBase(string Arg, Range Range) : TokenBase(Arg, Range);

    public record ShortOptionToken(string Arg, Index Index) : OptionTokenBase(Arg, Index..(Index.IsFromEnd ? ^(Index.Value - 1) : Index.Value + 1))
    {
        public char Flag => Arg[Index];

        public override string ToString() => $"-{Flag}";
    }

    public record LongOptionToken(string Arg, Range Range) : OptionTokenBase(Arg, Range)
    {
        public string Key => Arg[Range];

        public override string ToString() => $"--{Key}";
    }

    public record ArgumentToken(string Arg, Index Start) : TokenBase(Arg, Start..)
    {
        public string Value => Arg[Range];

        public override string ToString() => Value;
    }

    public record SentinelToken() : TokenBase("--", Range.All)
    {
        public override string ToString() => "--";
    }

    public static class CommandLineTokenizer
    {
        private static readonly Regex FlagsRex = new Regex(@"^-(?<flag>[\p{L}\p{N}])+$");

        private static readonly Regex KeyRex = new Regex(@"^(?<prefix>\/|--)(?<key>\w+(?::\w+)*)$");

        private static readonly Regex KeyValueRex = new Regex(@"^(?<key>\w+(?::\w+)*)=(?<value>.*)$");

        private static bool TryMatch(this Regex regex, string input, out Match match)
        {
            match = regex.Match(input);
            return match.Success;
        }

        public static IEnumerable<TokenBase> Tokenize(string arg)
        {
            if (FlagsRex.TryMatch(arg, out var flagsMatch))
            {
                foreach (Capture flag in flagsMatch.Groups["flag"].Captures)
                {
                    yield return new ShortOptionToken(arg, flag.Index);
                }
            }
            else if (KeyRex.TryMatch(arg, out var keyMatch))
            {
                Capture key = keyMatch.Groups["key"];
                yield return new LongOptionToken(arg, key.Index..);
            }
            else if (KeyValueRex.TryMatch(arg, out var keyValueMatch))
            {
                Capture key = keyValueMatch.Groups["key"];
                yield return new LongOptionToken(arg, ..key.Length);

                Capture value = keyValueMatch.Groups["value"];
                yield return new ArgumentToken(arg, value.Index);
            }
            else if (arg == "--")
            {
                yield return new SentinelToken();
            }
            else
            {
                yield return new ArgumentToken(arg, Index.Start);
            }
        }
    }
}
