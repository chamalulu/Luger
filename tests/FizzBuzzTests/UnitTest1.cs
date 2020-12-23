using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Luger.FizzBuzzTests
{
    internal static class TestData
    {
        public static readonly string[] Expected = new[]
        {
            "1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz",
            "11", "Fizz", "13", "14", "FizzBuzz", "16", "17", "Fizz", "19", "Buzz",
            "Fizz", "22", "23", "Fizz", "Buzz", "26", "Fizz", "28", "29", "FizzBuzz",
            "31", "32", "Fizz", "34", "Buzz", "Fizz", "37", "38", "Fizz", "Buzz",
            "41", "Fizz", "43", "44", "FizzBuzz", "46", "47", "Fizz", "49", "Buzz",
            "Fizz", "52", "53", "Fizz", "Buzz", "56", "Fizz", "58", "59", "FizzBuzz",
            "61", "62", "Fizz", "64", "Buzz", "Fizz", "67", "68", "Fizz", "Buzz",
            "71", "Fizz", "73", "74", "FizzBuzz", "76", "77", "Fizz", "79", "Buzz",
            "Fizz", "82", "83", "Fizz", "Buzz", "86", "Fizz", "88", "89", "FizzBuzz",
            "91", "92", "Fizz", "94", "Buzz", "Fizz", "97", "98", "Fizz", "Buzz",
        };
    }

    public class EnumerableTests
    {
        [Fact]
        public void FizzBuzzTest()
        {
            // Act
            var actual = FizzBuzz.Enumerable.FizzBuzz();

            // Assert
            Assert.Equal(TestData.Expected, actual);
        }
    }

    public class DryEnumerableTests
    {
        [Fact]
        public void FizzBuzzTest()
        {
            // Act
            var actual = FizzBuzz.DryEnumerable.FizzBuzz();

            // Assert
            Assert.Equal(TestData.Expected, actual);
        }
    }

    public class ParameterizedDryEnumerableTests
    {
        [Fact]
        public void FizzBuzzTest()
        {
            // Act
            var actual = FizzBuzz.ParameterizedDryEnumerable.FizzBuzz();

            // Assert
            Assert.Equal(TestData.Expected, actual);
        }
    }

    public class ParameterizedDryEnumerableWithLoopAbstractionTests
    {
        [Fact]
        public void FizzBuzzTest()
        {
            // Act
            var actual = FizzBuzz.ParameterizedDryEnumerableWithLoopAbstraction.FizzBuzz();

            // Assert
            Assert.Equal(TestData.Expected, actual);
        }
    }

    public class ParameterizedDryEnumerableWithMappedLoopAbstractionTests
    {
        [Fact]
        public void FizzBuzzTest()
        {
            // Act
            var actual = FizzBuzz.ParameterizedDryEnumerableWithMappedLoopAbstraction.FizzBuzz();

            // Assert
            Assert.Equal(TestData.Expected, actual);
        }
    }

    public class MoreParameterizedDryEnumerableWithMappedLoopAbstractionTests
    {
        [Fact]
        public void FizzBuzzTest()
        {
            // Act
            var actual = FizzBuzz.MoreParameterizedDryEnumerableWithMappedLoopAbstraction.FizzBuzz();

            // Assert
            Assert.Equal(TestData.Expected, actual);
        }

        private static readonly string[] Expected = new[]
        {
            "1", "2", "Fizz", "4", "Buzz", "Fizz", "Dazz",
            "8", "Fizz", "Buzz", "11", "Fizz", "13", "Dazz",
            "FizzBuzz", "16", "17", "Fizz", "19", "Buzz", "FizzDazz"
        };

        [Fact]
        public void FizzBuzzDazzTest()
        {
            // Act
            var actual = FizzBuzz.MoreParameterizedDryEnumerableWithMappedLoopAbstraction.FizzBuzz(21, (3, "Fizz"), (5, "Buzz"), (7, "Dazz"));

            // Assert
            Assert.Equal(Expected, actual);
        }
    }

    public class GenericMoreParameterizedDryEnumerableWithMappedLoopAbstractionTests
    {
        private readonly struct ResultTypeClassInstance : FizzBuzz.GenericMoreParameterizedDryEnumerableWithMappedLoopAbstraction.IResultTypeClass<string>
        {
            public string Concat(IEnumerable<string> ts) => string.Concat(ts);

            public string FromInt(int i) => i.ToString();
        }

        [Fact]
        public void FizzBuzzDazzTest()
        {
            // Act
            var actual = FizzBuzz.GenericMoreParameterizedDryEnumerableWithMappedLoopAbstraction.FizzBuzz<string, ResultTypeClassInstance>(100, (3, "Fizz"), (5, "Buzz"));

            // Assert
            Assert.Equal(TestData.Expected, actual);
        }
    }
}
