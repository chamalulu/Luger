using System;
using System.Collections.Generic;
using System.Threading;

using Luger.Functional;

using static Luger.Functional.Maybe;

namespace Luger.Utilities
{
    public static class EnumeratorMultiplexer
    {
        public static EnumeratorMultiplexer<T> GetEnumeratorMultiplexer<T>(this IEnumerable<T> enumerable)
        {
            enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));

            return new EnumeratorMultiplexer<T>(enumerable.GetEnumerator());
        }
    }

    /// <summary>
    /// Multiplexer of <see cref="IEnumerator{T}"/>. Provides ability for many consumers of a single enumerator.
    /// </summary>
    /// <remarks>
    /// Consumed elements are lazily appended as nodes in a singly linked list.
    /// Head of list is deallocated by garbage collection when no consumers are referencing it any more.
    /// </remarks>
    /// <typeparam name="T">Type of enumerated elements</typeparam>
    public sealed class EnumeratorMultiplexer<T> : IDisposable
    {
        /// <summary>
        /// Source enumerator
        /// </summary>
        private readonly IEnumerator<T> _enumerator;

        internal EnumeratorMultiplexer(IEnumerator<T> enumerator)

            => _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));


        /// <summary>
        /// Lock object for serialization of ProduceNext()
        /// </summary>
        private readonly object _producerLock = new();

        /// <summary>
        /// Produce next node of enumerator.
        /// </summary>
        /// <remarks>
        /// This will consume an element from the enumerator.
        /// </remarks>
        /// <returns>Returns next node or None if enumerator is exhausted</returns>
        internal Maybe<EnumeratorMultiplexerNode<T>> ProduceNext()
        {
            lock (_producerLock)
            {
                return _enumerator.MoveNext()
                    ? Some(new EnumeratorMultiplexerNode<T>(this, _enumerator.Current))
                    : None<EnumeratorMultiplexerNode<T>>();
            }
        }

        /// <summary>
        /// Threadsafe flag to prevent double start. 0 := not started, 1 := started
        /// </summary>
        private int _started = 0;

        /// <summary>
        /// Produce first node of enumerator.
        /// </summary>
        /// <remarks>
        /// This will consume an element from the enumerator and should only be called once by the consumer.
        /// </remarks>
        /// <returns>Returns first node or None if enumerator is exhausted</returns>
        /// <exception cref="InvalidOperationException">Thrown if invoked more than once</exception>
        public Maybe<EnumeratorMultiplexerNode<T>> MoveNext()

            => Interlocked.Exchange(ref _started, 1) == 0
                ? ProduceNext()
                : throw new InvalidOperationException();

        /// <summary>
        /// Dispose enumerator multiplexer.
        /// </summary>
        /// <remarks>
        /// Semantics of disposal are determined by enumerator implementation of Dispose.
        /// E.g. <see cref="List{T}.Enumerator"/> will happily continue enumeration after disposal. 
        /// </remarks>
        public void Dispose() => _enumerator.Dispose();
    }

    /// <summary>
    /// Node of <see cref="EnumeratorMultiplexer{T}"/>. Encapsulates an enumerated value and provides reference to next node.
    /// </summary>
    /// <typeparam name="T">Type of enumerated elements</typeparam>
    public sealed class EnumeratorMultiplexerNode<T>
    {
        /// <summary>
        /// Reference to next node or the multiplexer if next node is not yet enumerated.
        /// </summary>
        private object _next;

        /// <summary>
        /// Value of this node instance.
        /// </summary>
        public T Value { get; }

        internal EnumeratorMultiplexerNode(EnumeratorMultiplexer<T> multiplexer, T value)
        {
            _next = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
            Value = value;
        }

        /// <summary>
        /// Produce next node or None if enumerator is exhausted.
        /// </summary>
        public Maybe<EnumeratorMultiplexerNode<T>> MoveNext()
        {
            Maybe<EnumeratorMultiplexerNode<T>> some(EnumeratorMultiplexerNode<T> node)
            {
                _next = node;
                return Some(node);
            }

            lock (_next)
            {
                return _next switch
                {
                    EnumeratorMultiplexerNode<T> node => Some(node),
                    EnumeratorMultiplexer<T> multiplexer => multiplexer.ProduceNext().Match(some, None<EnumeratorMultiplexerNode<T>>),
                    _ => throw new InvalidOperationException()
                };
            }
        }
    }
}
