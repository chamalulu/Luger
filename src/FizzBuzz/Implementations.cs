using System;
using System.Collections.Generic;
using System.Linq;

namespace Luger.FizzBuzz
{
    /// <summary>
    /// Simple, canonical example of FizzBuzz.
    /// </summary>
    public static class Simple
    {
        public static void FizzBuzz()
        {
            for (int i = 1; i <= 100; i++)
            {
                if (i % 3 == 0 && i % 5 == 0)
                    Console.WriteLine("FizzBuzz");
                else if (i % 3 == 0)
                    Console.WriteLine("Fizz");
                else if (i % 5 == 0)
                    Console.WriteLine("Buzz");
                else
                    Console.WriteLine(i);
            }
        }
    }

    // Push dependecies out.

    /// <summary>
    /// Enumerable FizzBuzz.
    /// </summary>
    public static class Enumerable
    {
        public static IEnumerable<string> FizzBuzz()
        {
            for (int i = 1; i <= 100; i++)
            {
                if (i % 3 == 0 && i % 5 == 0)
                    yield return "FizzBuzz";
                else if (i % 3 == 0)
                    yield return "Fizz";
                else if (i % 5 == 0)
                    yield return "Buzz";
                else
                    yield return i.ToString();
            }
        }
    }

    // Reduce repeated divisibility logic and duplicated string constants.

    /// <summary>
    /// Dry, enumerable FizzBuzz.
    /// </summary>
    public static class DryEnumerable
    {
        const string FIZZ = "Fizz";
        const string BUZZ = "Buzz";
        const string FIZZBUZZ = FIZZ + BUZZ;

        public static IEnumerable<string> FizzBuzz()
        {
            for (int i = 1; i <= 100; i++)
            {
                bool isFizz = i % 3 == 0;
                bool isBuzz = i % 5 == 0;

                if (isFizz && isBuzz)
                    yield return FIZZBUZZ;
                else if (isFizz)
                    yield return FIZZ;
                else if (isBuzz)
                    yield return BUZZ;
                else
                    yield return i.ToString();
            }
        }
    }

    // The requirement of 100 numbers may change. Parameterize.

    /// <summary>
    /// Parameterized, dry, enumerable FizzBuzz.
    /// </summary>
    public static class ParameterizedDryEnumerable
    {
        const string FIZZ = "Fizz";
        const string BUZZ = "Buzz";
        const string FIZZBUZZ = FIZZ + BUZZ;

        public static IEnumerable<string> FizzBuzz(int count = 100)
        {
            for (int i = 1; i <= count; i++)
            {
                bool isFizz = i % 3 == 0;
                bool isBuzz = i % 5 == 0;

                if (isFizz && isBuzz)
                    yield return FIZZBUZZ;
                else if (isFizz)
                    yield return FIZZ;
                else if (isBuzz)
                    yield return BUZZ;
                else
                    yield return i.ToString();
            }
        }
    }

    // Abstract iterated logic from loop makes more readable.

    /// <summary>
    /// Parameterized, dry, enumerable FizzBuzz with loop abstraction.
    /// </summary>
    public static class ParameterizedDryEnumerableWithLoopAbstraction
    {
        const string FIZZ = "Fizz";
        const string BUZZ = "Buzz";
        const string FIZZBUZZ = FIZZ + BUZZ;

        private static string FizzBuzzStep(int i)
        {
            bool isFizz = i % 3 == 0;
            bool isBuzz = i % 5 == 0;

            if (isFizz && isBuzz)
                return FIZZBUZZ;
            else if (isFizz)
                return FIZZ;
            else if (isBuzz)
                return BUZZ;
            else
                return i.ToString();
        }

        public static IEnumerable<string> FizzBuzz(int count = 100)
        {
            for (int i = 1; i <= count; i++)
            {
                yield return FizzBuzzStep(i);
            }
        }
    }

    // Map over Enumerable.Range

    /// <summary>
    /// Parameterized, dry, enumerable FizzBuzz mapping loop abstraction over Enumerable.Range .
    /// </summary>
    public static class ParameterizedDryEnumerableWithMappedLoopAbstraction
    {
        const string FIZZ = "Fizz";
        const string BUZZ = "Buzz";
        const string FIZZBUZZ = FIZZ + BUZZ;

        private static string FizzBuzzStep(int i)
        {
            bool isFizz = i % 3 == 0;
            bool isBuzz = i % 5 == 0;

            if (isFizz && isBuzz)
                return FIZZBUZZ;
            else if (isFizz)
                return FIZZ;
            else if (isBuzz)
                return BUZZ;
            else
                return i.ToString();
        }

        public static IEnumerable<string> FizzBuzz(int count = 100) =>
            System.Linq.Enumerable.Range(1, count).Select(FizzBuzzStep);
    }

    // Parameterize rules.

    /// <summary>
    /// Parameterized, dry, enumerable FizzBuzz mapping loop abstraction over Enumerable.Range .
    /// </summary>
    public static class MoreParameterizedDryEnumerableWithMappedLoopAbstraction
    {
        static readonly (int, string)[] DefaultRules = new[] { (3, "Fizz"), (5, "Buzz") };

        public static IEnumerable<string> FizzBuzz(int count = 100, params (int divisor, string phrase)[] rules)
        {
            if (rules.Length == 0)
            {
                rules = DefaultRules;
            }

            string step(int i)
            {
                var applicableRules = rules.Where(rule => i % rule.divisor == 0);

                var phrases = applicableRules.Select(rule => rule.phrase);

                return applicableRules.Any()
                    ? string.Concat(phrases)
                    : i.ToString();
            }

            return System.Linq.Enumerable.Range(1, count).Select(step); 
        }
    }

    // And make generic, shedding default rules and stepping well into YAGNI land.

    /// <summary>
    /// Generic, parameterized, dry, enumerable FizzBuzz mapping loop abstraction over Enumerable.Range .
    /// </summary>
    public static class GenericMoreParameterizedDryEnumerableWithMappedLoopAbstraction
    {
        public interface IResultTypeClass<T>
        {
            T FromInt(int i);
            T Concat(IEnumerable<T> ts);
        }

        public static IEnumerable<T> FizzBuzz<T, TI>(int count = 100, params (int divisor, T phrase)[] rules)
            where TI : struct, IResultTypeClass<T>
        {
            T step(int i)
            {
                var applicableRules = rules.Where(rule => i % rule.divisor == 0);

                var phrases = applicableRules.Select(rule => rule.phrase);

                return applicableRules.Any()
                    ? default(TI).Concat(phrases)
                    : default(TI).FromInt(i);
            }

            return System.Linq.Enumerable.Range(1, count).Select(step);
        }
    }
}
