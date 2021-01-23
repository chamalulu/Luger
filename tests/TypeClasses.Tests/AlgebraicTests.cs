using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using TestData = System.ValueTuple<object, System.Collections.IEnumerable>;

namespace Luger.TypeClasses.Tests
{

#pragma warning disable CA1062 // Validate arguments of public methods

    public class AlgebraicTests
    {
        private static IEnumerable<object[]> GetTestData(IEnumerable<TestData> testData)
            => from testDatum in testData
               let instance = testDatum.Item1
               from sample in testDatum.Item2.Cast<object>()
               select new object[] { instance, sample };

        // Boolean operations are not lossy. We cover the whole range of values. :)
        private static readonly IEnumerable<bool> BooleanSample = new[] { false, true };

        private static readonly IEnumerable<int> Int32Sample = new[] { int.MinValue, -1, 0, 1, int.MaxValue };

        // We cannot use too extreme values or values with lossy representation in these tests since IEEE 754 operations loose precision. MinValue, MaxValue, Epsilon and 3 are examples of bad candidates.
        private static readonly IEnumerable<double> DoubleSample = new double[] { -1, 0, 1, 2, 4 };

        private static readonly TestData[] IdentityTestData = new TestData[]
        {
            (default(BooleanXorGroupInstance), BooleanSample),
            (default(BooleanOrMonoidInstance), BooleanSample),
            (default(BooleanAndMonoidInstance), BooleanSample),
            (default(Int32AddGroupInstance), Int32Sample),
            (default(Int32MulMonoidInstance), Int32Sample),
            (default(DoubleAddGroupInstance), DoubleSample),
            (default(DoubleMulGroupInstance), DoubleSample)
        };

        public static IEnumerable<object[]> GetIdentityTestData => GetTestData(IdentityTestData);

        [Theory]
        [MemberData(nameof(GetIdentityTestData))]
        public void IdentityTest<T>(IMonoid<T> monoid, T sample) where T : IEquatable<T>
        {
            Assert.Equal(monoid.Operation(monoid.Identity, sample), sample);
            Assert.Equal(monoid.Operation(sample, monoid.Identity), sample);
        }

        private static IEnumerable<(T, T, T)> TripleSample<T>(IEnumerable<T> samples)
            => from a in samples
               from b in samples
               from c in samples
               select (a, b, c);

        private static readonly TestData[] AssociativityTestData = new TestData[]
        {
            (default(BooleanXorGroupInstance), TripleSample(BooleanSample)),
            (default(BooleanOrMonoidInstance), TripleSample(BooleanSample)),
            (default(BooleanAndMonoidInstance), TripleSample(BooleanSample)),
            (default(Int32AddGroupInstance), TripleSample(Int32Sample)),
            (default(Int32MulMonoidInstance), TripleSample(Int32Sample)),
            (default(DoubleAddGroupInstance), TripleSample(DoubleSample)),
            (default(DoubleMulGroupInstance), TripleSample(DoubleSample))
        };

        public static IEnumerable<object[]> GetAssociativityTestData => GetTestData(AssociativityTestData);

        [Theory]
        [MemberData(nameof(GetAssociativityTestData))]
        public void AssociativityTest<T>(IMonoid<T> monoid, (T, T, T) sample) where T : IEquatable<T>
        {
            var (a, b, c) = sample;

            T x = monoid.Operation(monoid.Operation(a, b), c);
            T y = monoid.Operation(a, monoid.Operation(b, c));

            Assert.Equal(x, y);
        }

        private static readonly TestData[] InvertibilityTestData = new TestData[]
        {
            (default(BooleanXorGroupInstance), BooleanSample),
            (default(Int32AddGroupInstance), Int32Sample),
            (default(DoubleAddGroupInstance), DoubleSample),
            (default(DoubleMulGroupInstance), DoubleSample.Where(x => x != 0))  // Double is a field. A.s. inverse of zero is undefined.
        };

        public static IEnumerable<object[]> GetInvertibilityTestData => GetTestData(InvertibilityTestData);

        [Theory]
        [MemberData(nameof(GetInvertibilityTestData))]
        public void InvertibilityTest<T>(IGroup<T> group, T sample) where T : IEquatable<T>
        {
            T inverse = group.Inverse(sample);

            T x = group.Operation(sample, inverse);
            T y = group.Operation(inverse, sample);

            Assert.Equal(x, group.Identity);
            Assert.Equal(y, group.Identity);
        }

        private static IEnumerable<(T, T)> PairSample<T>(IEnumerable<T> samples)
            => from a in samples
               from b in samples
               select (a, b);

        private static readonly TestData[] CommutativityTestData = new TestData[]
        {
            (default(BooleanXorGroupInstance), PairSample(BooleanSample)),
            (default(BooleanOrMonoidInstance), PairSample(BooleanSample)),
            (default(BooleanAndMonoidInstance), PairSample(BooleanSample)),
            (default(Int32AddGroupInstance), PairSample(Int32Sample)),
            (default(Int32MulMonoidInstance), PairSample(Int32Sample)),
            (default(DoubleAddGroupInstance), PairSample(DoubleSample)),
            (default(DoubleMulGroupInstance), PairSample(DoubleSample))
        };

        public static IEnumerable<object[]> GetCommutativityTestData => GetTestData(CommutativityTestData);

        [Theory]
        [MemberData(nameof(GetCommutativityTestData))]
        public void CommutativityTest<T>(ICommutativeMonoid<T> commutativeMonoid, (T, T) sample) where T : IEquatable<T>
        {
            var (a, b) = sample;

            T x = commutativeMonoid.Operation(a, b);
            T y = commutativeMonoid.Operation(b, a);

            Assert.Equal(x, y);
        }

        private static readonly TestData[] DistributivityTestData = new TestData[]
        {
            (default(BooleanOrAndSemiringInstance), TripleSample(BooleanSample)),
            (default(BooleanXorAndRingInstance), TripleSample(BooleanSample)),
            (default(Int32AddMulRingInstance), TripleSample(Int32Sample)),
            (default(DoubleAddMulFieldInstance), TripleSample(DoubleSample))
        };

        public static IEnumerable<object[]> GetDistributivityTestData => GetTestData(DistributivityTestData);

        [Theory]
        [MemberData(nameof(GetDistributivityTestData))]
        public void LeftDistributivityTest<T>(ISemiring<T> semiring, (T, T, T) sample) where T : IEquatable<T>
        {
            var (a, b, c) = sample;

            T x = semiring.Multiply(a, semiring.Add(b, c));
            T y = semiring.Add(semiring.Multiply(a, b), semiring.Multiply(a, c));

            Assert.Equal(x, y);
        }

        [Theory]
        [MemberData(nameof(GetDistributivityTestData))]
        public void RightDistributivityTest<T>(ISemiring<T> semiring, (T, T, T) sample) where T : IEquatable<T>
        {
            var (a, b, c) = sample;

            T x = semiring.Multiply(semiring.Add(a, b), c);
            T y = semiring.Add(semiring.Multiply(a, c), semiring.Multiply(b, c));

            Assert.Equal(x, y);
        }

    }

#pragma warning restore CA1062 // Validate arguments of public methods

}
