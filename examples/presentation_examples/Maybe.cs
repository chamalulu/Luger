using Luger.Functional;

using System;

using static Luger.Functional.Maybe;


namespace Luger.Examples.presentation_examples
{
    // Example of code using null to indicate missing value.
    namespace NullBased
    {
        class Person
        {
            // Date of birth may be missing, indicated by nullable value type.
            // Consuming code must remember to guard against null.
            public DateTime? DateOfBirth;
        }

        interface IRepository
        {
            // Person may not exist, indicated by nullable reference type.
            // Consuming code must remember to guard against null.
            Person? Find(Guid id);
        }

        interface IAgeService
        {
            // Combination of missing cases is indicated by nullable value type.
            // Consuming code must remember to guard against null.
            TimeSpan? GetAgeById(Guid id, DateTime date);
        }

        class AgeService : IAgeService
        {
            readonly IRepository repo;

            public AgeService(IRepository repo) => this.repo = repo;

            // The implementation can be made pretty concise due to pattern matching with inline variable declarations.
            public TimeSpan? GetAgeById(Guid id, DateTime date)
            {
                if (repo.Find(id) is Person person && person.DateOfBirth is DateTime dob)
                {
                    return date - dob;
                }
                else
                {
                    return null;
                }
            }

            /* The implementation can be made even more concise due to C# 6 null-conditional member access and nullable
             * lifted operators but takes some careful reading to examine all the null handling.
             * In the expression below there are two distinct null checks.
             * The first one is somewhat obvious and made descreetly explicit by the elvis operator (?.).
             * The second one is not that obvious.
             */
            public TimeSpan? GetAgeByIdConciseButLessReadable(Guid id, DateTime date)

                => date - repo.Find(id)?.DateOfBirth;
        }
    }

    // Example of code using Try* with out parameter pattern.
    namespace TryBased
    {
        // The same contract as NullBased.Person .
        class Person
        {
            // Date of birth may be missing, indicated by nullable value type.
            // Consuming code must remember to guard against null.
            public DateTime? DateOfBirth;
        }

        interface IRepository
        {
            // Return true and set person out parameter if person found.
            // Consuming code needs explicit branching but will probably remember to guard agains missing value.
            bool TryFind(Guid id, out Person person);
        }

        interface IAgeService
        {
            // Return true and set age out parameter if person and its date of birth is available.
            // Consuming code needs explicit branching but will probably remember to guard agains missing value.
            bool TryGetAgeById(Guid id, DateTime date, out TimeSpan age);
        }

        // Also implements MaybeBased.IAgeService to illustrate bridging styles.
        class AgeService : IAgeService, MaybeBased.IAgeService
        {
            readonly IRepository repo;

            public AgeService(IRepository repo) => this.repo = repo;

            // Some local state juggling is necessary to combine missing cases.
            public bool TryGetAgeById(Guid id, DateTime date, out TimeSpan age)
            {
                if (repo.TryFind(id, out var person) && person.DateOfBirth is DateTime dob)
                {
                    age = date - dob;
                    return true;
                }
                else
                {
                    age = default;
                    return false;
                }
            }

            // Combining missing cases becomes a little simpler without out parameters.
            Maybe<TimeSpan> MaybeBased.IAgeService.GetAgeById(Guid id, DateTime date)
            {
                if (TryGetAgeById(id, date, out var age))
                {
                    return age;
                }
                else
                {
                    return default;
                }
            }
        }
    }

    // Example of code using higher order option type.
    namespace MaybeBased
    {
        class Person
        {
            // Date of birth may be missing, indicated by wrapping with Maybe<>.
            // Consuming code is forced to handle missing case.
            public Maybe<DateTime> DateOfBirth;
        }

        interface IRepository
        {
            // Person may not exist, indicated by wrapping by wrapping with Maybe<>.
            // Consuming code is forced to handle missing case.
            Maybe<Person> Find(Guid id);
        }

        interface IAgeService
        {
            // Combination of missing cases indicated by wrapping with Maybe<>.
            // Consuming code is forced to handle missing case.
            Maybe<TimeSpan> GetAgeById(Guid id, DateTime date);
        }

        // Also implements NullBased.IAgeService and TryBased.IAgeService to illustrate bridging styles.
        class AgeService : IAgeService, NullBased.IAgeService, TryBased.IAgeService
        {
            readonly IRepository repo;

            public AgeService(IRepository repo) => this.repo = repo;

            // Very readable and concise due to C# LINQ integration with Maybe<> monad.
            public Maybe<TimeSpan> GetAgeById(Guid id, DateTime date)

                => from person in repo.Find(id)
                   from dob in person.DateOfBirth
                   select date - dob;

            // Translating Maybe<> to Nullable<> is provided by ToNullable extension method.
            TimeSpan? NullBased.IAgeService.GetAgeById(Guid id, DateTime date)

                => GetAgeById(id, date).ToNullable();

            // With Maybe exposing the Try extension using Try* pattern is straightforward.
            bool TryBased.IAgeService.TryGetAgeById(Guid id, DateTime date, out TimeSpan age)

                => GetAgeById(id, date).Try(out age);
        }
    }
}
