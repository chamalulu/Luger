using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Luger.Utilities.Tests
{
    public class CombinatoricsTests
    {
        private class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
        {
            private readonly IEqualityComparer<T> _memberEqualityComparer;

            public ArrayEqualityComparer(IEqualityComparer<T> memberEqualityComparer) =>
                _memberEqualityComparer = memberEqualityComparer;

            public ArrayEqualityComparer() : this(EqualityComparer<T>.Default) { }

            public bool Equals(T[]? xs, T[]? ys)
            {
                if (xs is null || ys is null)
                    return xs is null && ys is null;

                if (xs.Length == ys.Length)
                    return xs.Zip(ys, (x, y) => (x, y))
                        .All(p => _memberEqualityComparer.Equals(p.x, p.y));

                return false;
            }

            public int GetHashCode(T[] obj)
            {
                var hc = new HashCode();

                foreach (var o in obj)
                    hc.Add(o, _memberEqualityComparer);

                return hc.ToHashCode();
            }
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
