using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Luger.Functional;

using Microsoft.Reactive.Testing;

using Xunit;

namespace Luger.Utilities.Tests
{
    public class EnumeratorMultiplexerTests
    {
        [Fact()]
        public void EnumeratorMultiplexerTest()
        {
            using var multiplexer = Enumerable.Range(0, 2).GetEnumeratorMultiplexer();

            var node0 = multiplexer.MoveNext().ValueUnsafe();
            var node1 = node0.MoveNext().ValueUnsafe();
            var node1Again = node0.MoveNext().ValueUnsafe();
            var maybeNode2 = node1.MoveNext();

            Assert.Equal(0, node0.Value);
            Assert.Equal(1, node1.Value);
            Assert.Same(node1, node1Again);
            Assert.False(maybeNode2.IsSome);
        }

        [Fact]
        public void EnumerableMultiplexerTestDoubleStart()
        {
            using var multiplexer = Enumerable.Range(0, 2).GetEnumeratorMultiplexer();

            var node0 = multiplexer.MoveNext().ValueUnsafe();

            void testCode() => multiplexer.MoveNext();

            Assert.Throws<InvalidOperationException>(testCode);
        }

        [Fact]
        public async Task EnumerableMultiplexerTestConcurrency()
        {
            /* scheduler:   0--1--2-
             * source:      ---0--1|
             */
            var aTick = TimeSpan.FromTicks(1);
            var scheduler = new TestScheduler();
            var source = Observable.Timer(aTick, aTick, scheduler).Take(2);

            // Create enumerator multiplexer from enumerable wrapping observable.
            using var multiplexer = source.ToEnumerable().GetEnumeratorMultiplexer();

            /* Advance to 1.
             * Otherwise multiplexer.MoveNext() will block waiting for first element.
             * (For some reason we cannot have Timer start at 0. TestScheduler will not enqueue first element when advanced to 0.)
             */
            scheduler.AdvanceTo(1);
            var node0 = multiplexer.MoveNext().ValueUnsafe();

            Assert.Equal(0, node0.Value);

            /* Create and start two tasks competing for node0.MoveNext()
             * One task will lock node0 and block on node0.MoveNext().
             * The other task will wait for lock on node0.
             */
            Task<EnumeratorMultiplexerNode<long>> node0MoveNextTask() => Task.Run(() => node0.MoveNext().ValueUnsafe());
            var node1Tasks = new[] { node0MoveNextTask(), node0MoveNextTask() };

            /* Advance to 2.
             * The task blocked in node0.MoveNext() will create node1 and return it.
             * The other task will wait for lock on node0 and then return node1.
             */
            scheduler.AdvanceTo(2);

            // Await nodes1. Both elements reference node1.
            var nodes1 = await Task.WhenAll(node1Tasks).ConfigureAwait(false);

            Assert.Equal(2, nodes1.Length);
            Assert.Equal(1, nodes1[0].Value);
            Assert.Same(nodes1[0], nodes1[1]);
        }
    }
}
