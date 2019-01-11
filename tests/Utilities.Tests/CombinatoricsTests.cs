using System.Collections.Generic;
using System.Linq;
using Luger.Functional;
using Xunit;

namespace Luger.Utilities.Tests
{
    public class CombinatoricsTests
    {
        private class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
        {
            public bool Equals(T[] x, T[] y) =>
                x?.Length == y?.Length &&
                (x?.Zip(y, (ex, ey) => (ex, ey))
                  ?.All(p => p.ex.Equals(p.ey)) ?? true);

            public int GetHashCode(T[] obj) =>
                EnumerableExt.GetHashCode(obj);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 6)]
        [InlineData(4, 24)]
        public void PermutationsTest(int elements, int expected)
        {
            var source = Enumerable.Range(0, elements).ToArray();

            // Count number of distinct permutations
            var pSet = new HashSet<int[]>(new ArrayEqualityComparer<int>());
            foreach (var _ in Combinatorics.Permutations(source))
            {
                var p = new int[source.Length];
                source.CopyTo(p, 0);
                pSet.Add(p);
            }

            var actual = pSet.Count;
            Assert.Equal(expected, actual);
        }
    }
}