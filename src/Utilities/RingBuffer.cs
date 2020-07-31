using System;
using System.Collections;
using System.Collections.Generic;

namespace Luger.Utilities
{
    public class RingBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;
        private uint _position;

        public RingBuffer(uint capacity)
        {
            _buffer = new T[capacity];
            _position = 0;
        }

        public uint Capacity => (uint)_buffer.Length;

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Capacity; i++)
                yield return _buffer[(_position + i) % Capacity];
        }

        public RingBuffer<T> Push(T value)
        {
            _buffer[_position++] = value;
            _position %= Capacity;
            return this;
        }

#pragma warning disable CA1303 // Do not pass literals as localized parameters

        public RingBuffer<T> Push(T[] buffer, uint index, uint count)
        {
            buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

            if (index + count > buffer.Length)
                throw new ArgumentException("index + count > buffer.Length");

            if (count > Capacity)
                (index, count) = (index + count - Capacity, Capacity);

            var headLength = Capacity - _position;
            var tailLength = count - headLength;

            Array.Copy(buffer, index, _buffer, _position, headLength);
            Array.Copy(buffer, index + headLength, _buffer, 0, tailLength);

            _position = (_position + count) % Capacity;
            return this;
        }

#pragma warning restore CA1303 // Do not pass literals as localized parameters

        public RingBuffer<T> Push(T[] buffer)
        {
            buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

            return Push(buffer, 0, (uint)buffer.Length);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
