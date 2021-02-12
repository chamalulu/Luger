using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Luger.Utilities
{
    public record EquatableSet<TElement> : IImmutableSet<TElement>
    {
        private readonly Lazy<int> _hashCode;

        public EquatableSet(ImmutableHashSet<TElement> set)
        {
            int xor(int hc, TElement e) => e is not null ? hc ^ set.KeyComparer.GetHashCode(e) : hc;

            int initHashCode() => set.Aggregate(0, xor);

            _hashCode = new Lazy<int>(initHashCode, LazyThreadSafetyMode.PublicationOnly);

            Set = set;
        }

        public ImmutableHashSet<TElement> Set { get; init; }

        public int Count => Set.Count;

        public EquatableSet<TElement> Add(TElement value) => this with { Set = Set.Add(value) };

        public EquatableSet<TElement> Clear() => this with { Set = Set.Clear() };

        public bool Contains(TElement value) => Set.Contains(value);

        public virtual bool Equals(EquatableSet<TElement>? other) => other is not null && Set.SetEquals(other.Set);

        public EquatableSet<TElement> Except(IEnumerable<TElement> other) => this with { Set = Set.Except(other) };

        public IEnumerator<TElement> GetEnumerator() => Set.GetEnumerator();

        public override int GetHashCode() => _hashCode.Value;

        public EquatableSet<TElement> Intersect(IEnumerable<TElement> other) => this with { Set = Set.Intersect(other) };

        public bool IsProperSubsetOf(IEnumerable<TElement> other) => Set.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<TElement> other) => Set.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<TElement> other) => Set.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<TElement> other) => Set.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<TElement> other) => Set.Overlaps(other);

        public EquatableSet<TElement> Remove(TElement value) => this with { Set = Set.Remove(value) };

        public bool SetEquals(IEnumerable<TElement> other) => Set.SetEquals(other);

        public EquatableSet<TElement> SymmetricExcept(IEnumerable<TElement> other) =>

            this with { Set = Set.SymmetricExcept(other) };

        public bool TryGetValue(TElement equalValue, out TElement actualValue) => Set.TryGetValue(equalValue, out actualValue);

        public EquatableSet<TElement> Union(IEnumerable<TElement> other) => this with { Set = Set.Union(other) };

        IImmutableSet<TElement> IImmutableSet<TElement>.Add(TElement value) => Add(value);

        IImmutableSet<TElement> IImmutableSet<TElement>.Clear() => Clear();

        IImmutableSet<TElement> IImmutableSet<TElement>.Except(IEnumerable<TElement> other) => Except(other);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IImmutableSet<TElement> IImmutableSet<TElement>.Intersect(IEnumerable<TElement> other) => Intersect(other);

        IImmutableSet<TElement> IImmutableSet<TElement>.Remove(TElement value) => Remove(value);

        IImmutableSet<TElement> IImmutableSet<TElement>.SymmetricExcept(IEnumerable<TElement> other) => SymmetricExcept(other);

        IImmutableSet<TElement> IImmutableSet<TElement>.Union(IEnumerable<TElement> other) => Union(other);
    }

    public static class EquatableSet
    {
        public static EquatableSet<TElement> Create<TElement>() where TElement : IEquatable<TElement> =>

            new(ImmutableHashSet.Create<TElement>());

        public static EquatableSet<TElement> Create<TElement>(IEqualityComparer<TElement> elementEqualityComparer) =>

            new(ImmutableHashSet.Create(elementEqualityComparer));

        public static EquatableSet<TElement> Create<TElement>(TElement item) where TElement : IEquatable<TElement> =>

            new(ImmutableHashSet.Create(item));

        public static EquatableSet<TElement> Create<TElement>(TElement item, IEqualityComparer<TElement> elementEqualityComparer) =>

            new(ImmutableHashSet.Create(elementEqualityComparer, item));

        public static EquatableSet<TElement> ToEquatableSet<TElement>(this IEnumerable<TElement> elements)
            where TElement : IEquatable<TElement> =>

            new(elements.ToImmutableHashSet());

        public static EquatableSet<TElement> ToEquatableSet<TElement>(
            this IEnumerable<TElement> elements,
            IEqualityComparer<TElement> elementEqualityComparer) =>

            new(elements.ToImmutableHashSet(elementEqualityComparer));
    }
}
