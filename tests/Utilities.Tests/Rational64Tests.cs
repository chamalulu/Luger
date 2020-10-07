using System.Collections.Generic;
using Xunit;
using System.Linq;
using System;

namespace Luger.Utilities.Tests
{
    public class Rational64Tests
    {
        public static IEnumerable<object[]> CreateTestData =
            from data in new (int n, uint d, Rational64 r)[]
            {
                (int.MinValue, 0x8000_0000u, -1),
                (int.MinValue + 1, 0x7FFF_FFFF, -1),
                (1, 2, Rational64.Create(1, 2)),
                (4849845, 9699690, Rational64.Create(1, 2)),
            }
            select new object[] { data.n, data.d, data.r };

        [Theory]
        [MemberData(nameof(CreateTestData))]
        public void CreateTest(int numerator, uint denominator, Rational64 expected)
        {
            var actual = Rational64.Create(numerator, denominator);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CreateDBZTest() => Assert.Throws<DivideByZeroException>(() => Rational64.Create(1, 0));

        public static IEnumerable<object[]> CompareToTestData =
            from data in new (Rational64 x, Rational64 y, int expected)[]
            {
                (1, 1, 0),
                (1, 2, -1),
                (2, 1, 1),
                (Rational64.Create(1, 2), Rational64.Create(2, 3), -1),
                (Rational64.Create(1, 2), Rational64.Create(2, 4), 0),
                (Rational64.Create(1, 2), Rational64.Create(1, 3), 1),
            }
            select new object[] { data.x, data.y, data.expected };

        [Theory]
        [MemberData(nameof(CompareToTestData))]
        public void CompareToTest(Rational64 x, Rational64 y, int expected)
        {
            var actual = x.CompareTo(y);
            if (expected == 0)
                Assert.Equal(expected, actual);
            else
                Assert.True(expected * actual > 0);
        }

        [Fact]
        public void EqualsTest() => Assert.Equal(Rational64.Create(0, 1), Rational64.Create(0, 2));

        public static IEnumerable<object[]> ExplicitOperatorRational64DoubleTestData =
            from data in new (double f, Rational64 r)[]
            {
                (double.NaN, Rational64.NaN),
                (double.NegativeInfinity, Rational64.NegativeInfinity),
                (double.PositiveInfinity, Rational64.PositiveInfinity),
                (0d, Rational64.Zero),
                (1d, Rational64.Create(1, 1)),
                (-123.456, Rational64.Create(-15432, 125)),
                /* A better approximation of pi which fits in Rational64 is
                 *  1,068,966,896 / 340,262,731 but it is not one of the
                 *  convergents this algorithm produce for pi. */
                (Math.PI, Rational64.Create(817696623, 260280919))
            }
            select new object[] { data.f, data.r };

        [Theory]
        [MemberData(nameof(ExplicitOperatorRational64DoubleTestData))]
        public void ExplicitOperatorRational64DoubleTest(double f, Rational64 expected)
        {
            var actual = (Rational64)f;
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ExplicitOperatorRational64DoubleTestData))]
        public void ExplicitOperatorDoubleRational64Test(double expected, Rational64 r)
        {
            var actual = (double)r;
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> OpUnaryNegationTestData =
            from data in new (Rational64 r, Rational64 e)[]
            {
                (Rational64.NaN, Rational64.NaN),
                (Rational64.NegativeInfinity, Rational64.PositiveInfinity),
                (Rational64.PositiveInfinity, Rational64.NegativeInfinity),
                (Rational64.Zero, Rational64.Zero)
            }
            select new object[] { data.r, data.e };

        [Theory]
        [MemberData(nameof(OpUnaryNegationTestData))]
        public void OpUnaryNegationTest(Rational64 r, Rational64 expected)
        {
            var actual = -r;
            Assert.Equal(expected, actual);
        }

        // op_LogicalNot is overloaded to provide rational reciprocal.
        public static IEnumerable<object[]> OpLogicalNotTestData =
            from data in new (Rational64 r, Rational64 e)[]
            {
                (Rational64.NaN, Rational64.NaN),
                (Rational64.NegativeInfinity, Rational64.Zero),
                (Rational64.PositiveInfinity, Rational64.Zero),
                (Rational64.Zero, Rational64.PositiveInfinity),
                (Rational64.Create(-1, 2), Rational64.Create(-2, 1)),
                (Rational64.Create(1, 2), Rational64.Create(2, 1))
            }
            select new object[] { data.r, data.e };

        [Theory]
        [MemberData(nameof(OpLogicalNotTestData))]
        public void OpLogicalNotTest(Rational64 r, Rational64 expected)
        {
            var actual = !r;
            Assert.Equal(expected, actual);
        }
    }
}
