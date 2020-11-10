using Luger.Functional;

using System;

using static Luger.Functional.Maybe;


namespace Luger.Examples.Maybe
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
            private readonly IRepository repo;

            public AgeService(IRepository repo) => this.repo = repo;

            // The implementation is made a little more concise by using C# 6 null-conditional member access.
            public TimeSpan? GetAgeById(Guid id, DateTime date)
            {
                DateTime? dob = repo.Find(id)?.DateOfBirth;

                if (dob.HasValue)
                    return date - dob.Value;
                else
                    return null;
            }
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
            private readonly IRepository repo;

            public AgeService(IRepository repo) => this.repo = repo;

            // Some local state juggling is necessary to combine missing cases.
            public bool TryGetAgeById(Guid id, DateTime date, out TimeSpan age)
            {
                bool success;

                (success, age) = repo.TryFind(id, out Person person) && person.DateOfBirth.HasValue
                    ? (true, date - person.DateOfBirth.Value)
                    : (false, default);

                return success;
            }

            // Combining missing cases becomes a little simpler without out parameters.
            Maybe<TimeSpan> MaybeBased.IAgeService.GetAgeById(Guid id, DateTime date)
                => repo.TryFind(id, out Person person) && person.DateOfBirth.HasValue
                    ? Some(date - person.DateOfBirth.Value)
                    : None<TimeSpan>();
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
            private readonly IRepository repo;

            public AgeService(IRepository repo) => this.repo = repo;

            // Very readable and concise due to C# LINQ integration with Maybe<> monad.
            public Maybe<TimeSpan> GetAgeById(Guid id, DateTime date)
                => from person in repo.Find(id)
                   from dob in person.DateOfBirth
                   select date - dob;

            // Translating Maybe<> to Nullable<> is done by pattern matching with Maybe<>.Match .
            TimeSpan? NullBased.IAgeService.GetAgeById(Guid id, DateTime date)
                => GetAgeById(id, date).Match<TimeSpan?>(
                    some: ts => ts,
                    none: () => null);

            // Translating Maybe<> to Try* pattern is done by pattern matching with Maybe<>.Match . But still the awkward local state juggling.
            bool TryBased.IAgeService.TryGetAgeById(Guid id, DateTime date, out TimeSpan age)
            {
                bool success;

                (success, age) = GetAgeById(id, date).Match(
                    some: dob => (true, dob),
                    none: () => (false, default));

                return success;
            }
        }
    }
}
