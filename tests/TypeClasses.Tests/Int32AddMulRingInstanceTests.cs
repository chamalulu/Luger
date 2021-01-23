using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace Luger.TypeClasses.Tests
{
    public class Int32AddMulRingInstanceTests
    {
        public static IEnumerable<object[]> RootsOfUnityTestData => new (uint degree, int[] roots)[]
        {
            (1u, new []{ 1 }),
            (2u, new []{ 1, -1, int.MaxValue, int.MinValue + 1}),
        }.Select(testCase => new object[] { testCase.degree, testCase.roots.ToHashSet() });

        [Theory]
        [MemberData(nameof(RootsOfUnityTestData))]
        public void RootsOfUnityTest(uint degree, ISet<int> expected)
        {
            // Act
            var actual = default(Int32AddMulRingInstance).RootsOfUnity(degree);

            // Assert
            Assert.Subset(expected, actual);
            Assert.Superset(expected, actual);
        }

        private static IEnumerable<uint> GetRandomUInts()
        {
            var rng = new Random();

            while (true)
                yield return (uint)rng.Next();
        }

        public static IEnumerable<object[]> OddDegrees => GetRandomUInts().Select(n => new object[] { n | 1u }).Take(100);

        [Theory]
        [MemberData(nameof(OddDegrees))]
        public void RootsOfUnityTest_OddDegrees(uint oddDegree)
        {
            // Arrange
            var expected = new HashSet<int>(1) { 1 };

            // Act
            var actual = default(Int32AddMulRingInstance).RootsOfUnity(oddDegree);

            // Assert
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> EvenDegrees => GetRandomUInts().Select(n => n & 0xFFFF_FFFE).Where(n => n > 0).Select(n => new object[] { n }).Take(100);

        [Theory]
        [MemberData(nameof(EvenDegrees))]
        public void RootsOfUnityTest_EvenDegrees(uint evenDegree)
        {
            // Act
            var actual = default(Int32AddMulRingInstance).RootsOfUnity(evenDegree);

            // Assert
            void action(int rou)
            {
                int pow = 1;

                for (uint i = 0; i < evenDegree; i++) unchecked
                {
                    pow *= rou;
                }

                Assert.Equal(1, pow);
            }

            Assert.All(actual, action);
        }
    }
}
