using System;

namespace Luger.Utilities
{

#pragma warning disable CA1815 // Override equals and operator equals on value types

    public ref struct IndirectSpanEnumerator<T>
    {
        private readonly IndirectReadOnlySpan<T> _span;
        private int _index;

        internal IndirectSpanEnumerator(IndirectReadOnlySpan<T> span)
        {
            _span = span;
            _index = -1;
        }

        public bool MoveNext()
        {
            int index = _index + 1;
            if (index < _span.Length)
            {
                _index = index;
                return true;
            }

            return false;
        }

        public ref readonly T Current => ref _span[_index];
    }

#pragma warning restore CA1815 // Override equals and operator equals on value types

#pragma warning disable CA1032 // Implement standard exception constructors

    public class DestinationTooShortException : ArgumentException
    {
        public DestinationTooShortException(string? paramName) : base(null, paramName) { }
    }

    public class SourceTooLongException : ArgumentException
    {
        public SourceTooLongException(string? paramName) : base(null, paramName) { }
    }

#pragma warning restore CA1032 // Implement standard exception constructors

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
#pragma warning disable CA1000 // Do not declare static members on generic types

    public readonly ref struct IndirectSpan<T>
    {
        private readonly Span<T> _left, _right;

        public static IndirectSpan<T> Empty => default;

        public IndirectSpan(Span<T> left, Span<T> right)
        {
            _left = left;
            _right = right;
        }

        public bool IsEmpty => _left.IsEmpty && _right.IsEmpty;

        public bool IsHalfEmpty => _left.IsEmpty ^ _right.IsEmpty;

        public Span<T> AsSpan() =>
            (_left.IsEmpty, _right.IsEmpty) switch
            {
                (false, false) => throw new InvalidOperationException(),
                (false, true) => _left,
                (true, false) => _right,
                (true, true) => Span<T>.Empty
            };

        public ref T this[int index] =>
            ref index < _left.Length
                ? ref _left[index]
                : ref _right[index - _left.Length];

        public int Length => _left.Length + _right.Length;

        public void Clear()
        {
            _left.Clear();
            _right.Clear();
        }

        public bool TryCopyTo(Span<T> destination) => ((IndirectReadOnlySpan<T>)this).TryCopyTo(destination);

        public void CopyTo(Span<T> destination) => ((IndirectReadOnlySpan<T>)this).CopyTo(destination);

        public bool TryCopyFrom(ReadOnlySpan<T> source)
        {
            if (Length < source.Length)
                return false;

            if (!_right.Overlaps(source))
            {
                // Move memory right first, left second
                source[_left.Length..].CopyTo(_right);
                source[.._left.Length].CopyTo(_left);
            }
            else if (!_left.Overlaps(source))
            {
                // Move memory left first, right second
                source[.._left.Length].CopyTo(_left);
                source[_left.Length..].CopyTo(_right);
            }
            else if (_left.Length < _right.Length)
            {
                // Make swap copy of left part of source, move right first, left second
                var swapBuffer = new T[_left.Length];
                var swap = new Span<T>(swapBuffer);

                source[.._left.Length].CopyTo(swap);
                source[_left.Length..].CopyTo(_right);
                swap.CopyTo(_left);
            }
            else
            {
                // Make swap copy of right part of source, move left first, right second
                var swapBuffer = new T[_right.Length];
                var swap = new Span<T>(swapBuffer);

                source[_left.Length..].CopyTo(swap);
                source[.._left.Length].CopyTo(_left);
                swap.CopyTo(_right);
            }

            return true;
        }

        public void CopyFrom(ReadOnlySpan<T> source)
        {
            if (!TryCopyFrom(source))
                throw new SourceTooLongException(nameof(source));
        }

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations

        [Obsolete("Equals() on IndirectSpan will always throw an exception. Use == instead.")]
        public override bool Equals(object? obj) => throw new NotSupportedException();

#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        public void Fill(T value)
        {
            _left.Fill(value);
            _right.Fill(value);
        }

        public static implicit operator IndirectReadOnlySpan<T>(IndirectSpan<T> span) =>
            new IndirectReadOnlySpan<T>(span._left, span._right);

        public IndirectReadOnlySpan<T> ToIndirectReadOnlySpan() => this;

        public IndirectSpanEnumerator<T> GetEnumerator() => new IndirectSpanEnumerator<T>(this);

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations

        [Obsolete("GetHashCode() on IndirectSpan will always throw an exception.")]
        public override int GetHashCode() => throw new NotSupportedException();

#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        /* The single parameter Slice method is implemented in Span<T> but it does not seem to be
         *  used by range parametered indexer expressions even when end of range is ^0.
         * We implement it in IndirectSpan<T> as well in the hope it may be used by a compiler
         *  optimizing for such cases.
         */
        public IndirectSpan<T> Slice(int start)
        {
            Span<T> left, right;

            if (start >= _left.Length)
            {
                left = Span<T>.Empty;
                right = _right.Slice(start - _left.Length);
            }
            else
            {
                left = _left.Slice(start);
                right = _right;
            }

            return new IndirectSpan<T>(left, right);
        }

        public IndirectSpan<T> Slice(int start, int length)
        {
            Span<T> left, right;

            if (start >= _left.Length)
            {
                left = Span<T>.Empty;
                right = _right.Slice(start - _left.Length, length);
            }
            else
            {
                if (start + length <= _left.Length)
                {
                    left = _left.Slice(start, length);
                    right = Span<T>.Empty;
                }
                else
                {
                    left = _left.Slice(start);
                    right = _right.Slice(0, length - _left.Length);
                }
            }

            return new IndirectSpan<T>(left, right);
        }

        public T[] ToArray() => ((IndirectReadOnlySpan<T>)this).ToArray();

        public static bool operator ==(IndirectSpan<T> left, IndirectSpan<T> right) =>
            left._left == right._left && left._right == right._right;

        public static bool operator !=(IndirectSpan<T> left, IndirectSpan<T> right) =>
            left._left != right._left || left._right != right._right;
    }

    public readonly ref struct IndirectReadOnlySpan<T>
    {
        private readonly ReadOnlySpan<T> _left, _right;

        public static IndirectReadOnlySpan<T> Empty => default;

        public IndirectReadOnlySpan(ReadOnlySpan<T> left, ReadOnlySpan<T> right)
        {
            _left = left;
            _right = right;
        }

        public bool IsEmpty => _left.IsEmpty && _right.IsEmpty;

        public bool IsHalfEmpty => _left.IsEmpty ^ _right.IsEmpty;

        public ReadOnlySpan<T> AsReadOnlySpan() =>
            (_left.IsEmpty, _right.IsEmpty) switch
            {
                (false, false) => throw new InvalidOperationException(),
                (false, true) => _left,
                (true, false) => _right,
                (true, true) => ReadOnlySpan<T>.Empty
            };

        public ref readonly T this[int index] =>
            ref index < _left.Length
                ? ref _left[index]
                : ref _right[index - _left.Length];

        public int Length => _left.Length + _right.Length;

        public bool TryCopyTo(Span<T> destination)
        {
            if (Length > destination.Length)
                return false;

            if (!_right.Overlaps(destination))
            {
                // Move memory left first, right second
                _left.CopyTo(destination);
                _right.CopyTo(destination[_left.Length..]);
            }
            else if (!_left.Overlaps(destination))
            {
                // Move memory right first, left second
                _right.CopyTo(destination[_left.Length..]);
                _left.CopyTo(destination);
            }
            else if (_left.Length < _right.Length)
            {
                // Make swap copy of left, move right first, left second
                var swapBuffer = new T[_left.Length];
                var swap = new Span<T>(swapBuffer);

                _left.CopyTo(swap);
                _right.CopyTo(destination[_left.Length..]);
                swap.CopyTo(destination);
            }
            else
            {
                // Make swap copy of right, move left first, right second
                var swapBuffer = new T[_right.Length];
                var swap = new Span<T>(swapBuffer);

                _right.CopyTo(swap);
                _left.CopyTo(destination);
                swap.CopyTo(destination[_left.Length..]);
            }

            return true;
        }

        public void CopyTo(Span<T> destination)
        {
            if (!TryCopyTo(destination))
                throw new DestinationTooShortException(nameof(destination));
        }

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations

        [Obsolete("Equals() on IndirectSpan will always throw an exception. Use == instead.")]
        public override bool Equals(object? obj) => throw new NotSupportedException();

#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        public IndirectSpanEnumerator<T> GetEnumerator() => new IndirectSpanEnumerator<T>(this);

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations

        [Obsolete("GetHashCode() on IndirectSpan will always throw an exception.")]
        public override int GetHashCode() => throw new NotSupportedException();

#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        public IndirectReadOnlySpan<T> Slice(int start)
        {
            ReadOnlySpan<T> left, right;

            if (start >= _left.Length)
            {
                left = ReadOnlySpan<T>.Empty;
                right = _right.Slice(start - _left.Length);
            }
            else
            {
                left = _left.Slice(start);
                right = _right;
            }

            return new IndirectReadOnlySpan<T>(left, right);
        }

        public IndirectReadOnlySpan<T> Slice(int start, int length)
        {
            ReadOnlySpan<T> left, right;

            if (start >= _left.Length)
            {
                left = ReadOnlySpan<T>.Empty;
                right = _right.Slice(start - _left.Length, length);
            }
            else
            {
                if (start + length <= _left.Length)
                {
                    left = _left.Slice(start, length);
                    right = ReadOnlySpan<T>.Empty;
                }
                else
                {
                    left = _left.Slice(start);
                    right = _right.Slice(0, length - _left.Length);
                }
            }

            return new IndirectReadOnlySpan<T>(left, right);
        }

        public T[] ToArray()
        {
            if (Length == 0)
                return Array.Empty<T>();

            var a = new T[Length];
            CopyTo(a.AsSpan());

            return a;
        }

        public static bool operator ==(IndirectReadOnlySpan<T> left, IndirectReadOnlySpan<T> right) =>
            left._left == right._left && left._right == right._right;

        public static bool operator !=(IndirectReadOnlySpan<T> left, IndirectReadOnlySpan<T> right) =>
            left._left != right._left || left._right != right._right;
    }
}
