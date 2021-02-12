using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Luger.Utilities
{
    public record EquatableSequence<TElement> : IImmutableList<TElement>
    {
        private readonly Lazy<int> _hashCode;

        public EquatableSequence(ImmutableList<TElement> list, IEqualityComparer<TElement>? elementEqualityComparer = null)
        {
            elementEqualityComparer ??= EqualityComparer<TElement>.Default;

            HashCode add(HashCode hc, TElement e)
            {
                hc.Add(e, elementEqualityComparer);
                return hc;
            }

            int initHashCode() => list.Aggregate<TElement, HashCode>(new(), add).ToHashCode();

            _hashCode = new Lazy<int>(initHashCode, LazyThreadSafetyMode.PublicationOnly);

            List = list;
            ElementEqualityComparer = elementEqualityComparer;
        }

        public TElement this[int index] => List[index];

        public ImmutableList<TElement> List { get; init; }

        public IEqualityComparer<TElement> ElementEqualityComparer { get; init; }

        public int Count => List.Count;

        public EquatableSequence<TElement> Add(TElement value) => this with { List = List.Add(value) };

        public EquatableSequence<TElement> AddRange(IEnumerable<TElement> items) => this with { List = List.AddRange(items) };

        public EquatableSequence<TElement> Clear() => this with { List = List.Clear() };

        public virtual bool Equals(EquatableSequence<TElement>? other) =>

            other is not null && List.SequenceEqual(other.List, ElementEqualityComparer);

        public IEnumerator<TElement> GetEnumerator() => List.GetEnumerator();

        public override int GetHashCode() => _hashCode.Value;

        public int IndexOf(TElement item, int index, int count, IEqualityComparer<TElement>? equalityComparer) =>

            List.IndexOf(item, index, count, equalityComparer ?? ElementEqualityComparer);

        public EquatableSequence<TElement> Insert(int index, TElement element) => this with { List = List.Insert(index, element) };

        public EquatableSequence<TElement> InsertRange(int index, IEnumerable<TElement> items) =>

            this with { List = List.InsertRange(index, items) };

        public int LastIndexOf(TElement item, int index, int count, IEqualityComparer<TElement>? equalityComparer) =>

            List.LastIndexOf(item, index, count, equalityComparer ?? ElementEqualityComparer);

        public EquatableSequence<TElement> Remove(TElement value, IEqualityComparer<TElement>? equalityComparer) =>

            this with { List = List.Remove(value, equalityComparer ?? ElementEqualityComparer) };

        public EquatableSequence<TElement> RemoveAll(Predicate<TElement> match) => this with { List = List.RemoveAll(match) };

        public EquatableSequence<TElement> RemoveAt(int index) => this with { List = List.RemoveAt(index) };

        public EquatableSequence<TElement> RemoveRange(
            IEnumerable<TElement> items,
            IEqualityComparer<TElement>? equalityComparer) =>

            this with { List = List.RemoveRange(items, equalityComparer ?? ElementEqualityComparer) };

        public EquatableSequence<TElement> RemoveRange(int index, int count) => this with { List = List.RemoveRange(index, count) };

        public EquatableSequence<TElement> Replace(
            TElement oldValue,
            TElement newValue,
            IEqualityComparer<TElement>? equalityComparer) =>

            this with { List = List.Replace(oldValue, newValue, equalityComparer ?? ElementEqualityComparer) };

        public EquatableSequence<TElement> SetItem(int index, TElement value) => this with { List = List.SetItem(index, value) };

        IImmutableList<TElement> IImmutableList<TElement>.Add(TElement value) => Add(value);

        IImmutableList<TElement> IImmutableList<TElement>.AddRange(IEnumerable<TElement> items) => AddRange(items);

        IImmutableList<TElement> IImmutableList<TElement>.Clear() => Clear();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IImmutableList<TElement> IImmutableList<TElement>.Insert(int index, TElement element) => Insert(index, element);

        IImmutableList<TElement> IImmutableList<TElement>.InsertRange(int index, IEnumerable<TElement> items) =>

            InsertRange(index, items);

        IImmutableList<TElement> IImmutableList<TElement>.Remove(TElement value, IEqualityComparer<TElement>? equalityComparer) =>

            Remove(value, equalityComparer);

        IImmutableList<TElement> IImmutableList<TElement>.RemoveAll(Predicate<TElement> match) => RemoveAll(match);

        IImmutableList<TElement> IImmutableList<TElement>.RemoveAt(int index) => RemoveAt(index);

        IImmutableList<TElement> IImmutableList<TElement>.RemoveRange(
            IEnumerable<TElement> items,
            IEqualityComparer<TElement>? equalityComparer) =>

            RemoveRange(items, equalityComparer);

        IImmutableList<TElement> IImmutableList<TElement>.RemoveRange(int index, int count) => RemoveRange(index, count);

        IImmutableList<TElement> IImmutableList<TElement>.Replace(
            TElement oldValue,
            TElement newValue,
            IEqualityComparer<TElement>? equalityComparer) =>

            Replace(oldValue, newValue, equalityComparer);

        IImmutableList<TElement> IImmutableList<TElement>.SetItem(int index, TElement value) => SetItem(index, value);
    }

    public static class EquatableSequence
    {
        public static EquatableSequence<TElement> Create<TElement>() where TElement : IEquatable<TElement> =>

            new(ImmutableList.Create<TElement>());

        public static EquatableSequence<TElement> Create<TElement>(IEqualityComparer<TElement>? elementEqualityComparer) =>

            new(ImmutableList.Create<TElement>(), elementEqualityComparer);
    }
}
