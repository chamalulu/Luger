using System;
using System.Collections.Generic;

namespace Luger.Utilities
{
    public static class Combinatorics
    {
        // This is a non-recursive implementation of Heap's algorithm (https://en.wikipedia.org/wiki/Heap%27s_algorithm)
        //  for producing all permutations of a set of elements while minimizing movement.
        //  The performance can probably be improved in an unsafe context.
        /// <summary>
        /// Enumerate permutations of elements in array.
        /// </summary>
        /// <param name="buffer">Array to permutate</param>
        /// <typeparam name="T">Element type of array</typeparam>
        /// <returns>Sequence counter of each new permutation</returns>
        /// <remarks>
        /// The iterator will yield n! times where n is the length of the array.
        /// At first yield the content of array is unchanged and counter is 0.
        /// At subsequent yields the content of array is a unique permutation of the original content and counter is increased.
        /// </remarks>
        public static IEnumerable<ulong> Permutations<T>(T[] buffer)
        {
            /* A buffer length of n will make the iterator yield n! times. Factorial grows very fast.
             * If n > 20 the number of permutations will cause the sequence counter to roll over.
             */
            var n = buffer.Length;

            // Prepare counters
            var seqno = 0UL;
            var counters = new int[n];

            // Yield first permutation (identity permutation) with sequence counter 0
            yield return seqno++;

            var i = 0;
            while (i < n)
            {
                if (counters[i] < i)
                {
                    var j = -(i & 0b1) & counters[i]; // if i is even then j = 0 else j = counters[i]

                    // Swap values buffer[i] and buffer[j]
                    var t = buffer[i];
                    buffer[i] = buffer[j];
                    buffer[j] = t;

                    // Yield next permutation
                    yield return unchecked(seqno++);

                    // Increase counter and reset index
                    counters[i]++;
                    i = 0;
                }
                else
                {
                    // Reset counter and increase index
                    counters[i] = 0;
                    i++;
                }
            }
        }
    }
}