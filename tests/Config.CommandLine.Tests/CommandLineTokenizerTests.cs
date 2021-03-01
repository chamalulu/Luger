using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Luger.Configuration.CommandLine.Tests
{
    public class CommandLineTokenizerTests
    {
        [Theory]
        [InlineData("-ab", 1, 'a', "-a")]
        [InlineData("-ab", 2, 'b', "-b")]
        public void ShortOptionTokenTest(string arg, int index, char flag, string repr)
        {
            var actual = new ShortFlagToken(arg, index);

            Assert.Equal(flag, actual.Flag);
            Assert.Equal(repr, actual.ToString());
        }

        [Theory]
        [InlineData("--option", 2, 8, "option", "--option")]
        [InlineData("option=value", 0, 6, "option", "--option")]
        public void LongOptionTokenTest(string arg, int start, int end, string key, string repr)
        {
            var actual = new LongFlagToken(arg, start..end);

            Assert.Equal(key, actual.Key);
            Assert.Equal(repr, actual.ToString());
        }

        [Theory]
        [InlineData("value", 0, "value")]
        [InlineData("option=value", 7, "value")]
        public void ArgumentTokenTest(string arg, int index, string value)
        {
            var actual = new ArgumentToken(arg, index);

            Assert.Equal(value, actual.Value);
        }

        public static IEnumerable<object[]> TokenizeTestData => new object[][]
        {
            new object[]
            {
                "-hal", new []{1,2,3}.Select(i => new ShortFlagToken("-hal", i)).ToArray()
            },
            new object[]
            {
                "/Option", new TokenBase[]
                {
                    new LongFlagToken("/Option", 1..)
                }
            },
            new object[]
            {
                "--option", new TokenBase[]
                {
                    new LongFlagToken("--option", 2..)
                }
            },
            new object[]
            {
                "Option=Value", new TokenBase[]
                {
                    new LongFlagToken("Option=Value", ..6),
                    new ArgumentToken("Option=Value", 7)
                }
            },
            new object[]
            {
                "--", new TokenBase[]
                {
                    new SentinelToken()
                }
            },
            new object[]
            {
                "", new TokenBase[]
                {
                    new ArgumentToken("", 0)
                }
            }
        };

        [Theory]
        [MemberData(nameof(TokenizeTestData))]
        public void TokenizeTest(string arg, TokenBase[] expected)
        {
            var actual = CommandLineTokenizer.Tokenize(arg);

            Assert.Equal(expected, actual);
        }
    }
}
