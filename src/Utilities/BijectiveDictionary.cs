using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Luger.Functional;
using static Luger.Functional.Maybe;

namespace Luger.Utilities
{
    /// <summary>
    /// Provides a set of initialization methods for instances of the <see cref="BijectiveDictionary{TI, TCI}"/> class.
    /// </summary>
    public static class BijectiveDictionary
    {
        /// <summary>
        /// Creates an empty bijective dictionary with default comparers.
        /// </summary>
        /// <typeparam name="TI">Type of item</typeparam>
        /// <typeparam name="TCI">Type of co-item</typeparam>
        /// <returns>An empty bijective dictionary with default comparers.</returns>
        public static BijectiveDictionary<TI, TCI> Create<TI, TCI>() => BijectiveDictionary<TI, TCI>.Empty;

        /// <summary>
        /// Creates an empty bijective dictionary with specified comparers.
        /// </summary>
        /// <param name="itemComparer">Comparer to use to determine the equality of items in the dictionary.</param>
        /// <param name="coItemComparer">Comparer to use to determine the equality of co-items in the dictionary.</param>
        /// <returns>An empty bijective dictionary with specified comparers.</returns>
        /// <inheritdoc cref="Create{TI, TCI}"/>
        public static BijectiveDictionary<TI, TCI> Create<TI, TCI>(
            IEqualityComparer<TI> itemComparer,
            IEqualityComparer<TCI> coItemComparer)

            => Create<TI, TCI>().WithComparers(itemComparer, coItemComparer);

        /// <summary>
        /// Private helper to aggregate transformed source elements into a bijective dictionary.
        /// </summary>
        /// <typeparam name="TSource">Type of source elements.</typeparam>
        /// <param name="source">The sequence to enumerate to generate the dictionary.</param>
        /// <param name="seed">The (empty) bijective dictionary to seed aggregation with.</param>
        /// <param name="itemSelector">The function that will produce the item from each source element.</param>
        /// <param name="coItemSelector">The dunction that will produce the co-item from each source element.</param>
        /// <returns>A bijective dictionary that contains the items and co-items from the specified sequence.</returns>
        /// <inheritdoc cref="Create{TI, TCI}"/>
        private static BijectiveDictionary<TI, TCI> AggregateSource<TSource, TI, TCI>(
            IEnumerable<TSource> source,
            BijectiveDictionary<TI, TCI> seed,
            Func<TSource, TI> itemSelector,
            Func<TSource, TCI> coItemSelector)

            => source.Aggregate(seed, (bd, s) => bd.Add(itemSelector(s), coItemSelector(s)));

        /// <summary>
        /// Enumerates and transforms a sequence, and produces a bijective dictionary of its contents.
        /// </summary>
        /// <inheritdoc cref="AggregateSource{TSource, TI, TCI}"/>
        public static BijectiveDictionary<TI, TCI> ToBijectiveDictionary<TSource, TI, TCI>(
            this IEnumerable<TSource> source,
            Func<TSource, TI> itemSelector,
            Func<TSource, TCI> coItemSelector)

            => AggregateSource(source, Create<TI, TCI>(), itemSelector, coItemSelector);

        /// <summary>
        /// Enumerates and transforms a sequence, and produces a bijective dictionary of its contents by using the specified comparers.
        /// </summary>
        /// <inheritdoc cref="AggregateSource{TSource, TI, TCI}"/>
        /// <inheritdoc cref="Create{TI, TCI}(IEqualityComparer{TI}, IEqualityComparer{TCI})"/>
        public static BijectiveDictionary<TI, TCI> ToBijectiveDictionary<TSource, TI, TCI>(
            this IEnumerable<TSource> source,
            Func<TSource, TI> itemSelector,
            Func<TSource, TCI> coItemSelector,
            IEqualityComparer<TI> itemComparer,
            IEqualityComparer<TCI> coItemComparer)

            => AggregateSource(source, Create(itemComparer, coItemComparer), itemSelector, coItemSelector);

        /// <summary>
        /// Enumerates a sequence of key/value pairs and produces a bijective dictionary of its contents.
        /// </summary>
        /// <param name="pairs">The sequence of key/value pairs to enumerate.</param>
        /// <returns>A bijective dictionary that contains the items and co-items from the specified key/value pairs.</returns>
        /// <inheritdoc cref="Create{TI, TCI}"/>
        public static BijectiveDictionary<TI, TCI> ToBijectiveDictionary<TI, TCI>(
            this IEnumerable<KeyValuePair<TI, TCI>> pairs)

            => AggregateSource(pairs, Create<TI, TCI>(), kvp => kvp.Key, kvp => kvp.Value);

        /// <summary>
        /// Enumerates a sequence of key/value pairs and produces a bijective dictionary of its contents by using the specified comparers.
        /// </summary>
        /// <inheritdoc cref="ToBijectiveDictionary{TI, TCI}(IEnumerable{KeyValuePair{TI, TCI}})"/>
        /// <inheritdoc cref="Create{TI, TCI}(IEqualityComparer{TI}, IEqualityComparer{TCI})" />
        public static BijectiveDictionary<TI, TCI> ToBijectiveDictionary<TI, TCI>(
            this IEnumerable<KeyValuePair<TI, TCI>> pairs,
            IEqualityComparer<TI> itemComparer,
            IEqualityComparer<TCI> coItemComparer)

            => AggregateSource(pairs, Create(itemComparer, coItemComparer), kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Represents an immutable, bijective dictionary of associated items and co-items.
    /// </summary>
    /// <typeparam name="TI">Type of item</typeparam>
    /// <typeparam name="TCI">Type of co-item</typeparam>
    /// <remarks>
    /// A bijective dictionary is a symmetric dictionary with strict one-to-one association between items and co-items.
    /// This implementation is modeled over <see cref="ImmutableDictionary{TKey, TValue}"/> and is immutable in the same way.
    /// When you manipulate a <see cref="BijectiveDictionary{TI, TCI}"/> a copy of the original dictionary is made,
    ///  manipulations applied and a new <see cref="BijectiveDictionary{TI, TCI}"/> is returned.
    /// </remarks>
    public class BijectiveDictionary<TI, TCI> : IEnumerable<(TI Item, TCI CoItem)>
    {
        private readonly ImmutableDictionary<TI, TCI> _itemDict;
        private readonly ImmutableDictionary<TCI, TI> _coItemDict;

        private BijectiveDictionary(ImmutableDictionary<TI, TCI> itemDict, ImmutableDictionary<TCI, TI> coItemDict)
        {
            // Invariant: No public method can introduce itemDict and coItemDict with incompatible comparers.
            Debug.Assert(itemDict.KeyComparer.Equals(coItemDict.ValueComparer));
            Debug.Assert(coItemDict.KeyComparer.Equals(itemDict.ValueComparer));

            // Invariant; No public method can introduce itemDict and coItemDict with incompatibe key and value sets.
            Debug.Assert(itemDict.All(kvp => itemDict.KeyComparer.Equals(kvp.Key, coItemDict[itemDict[kvp.Key]])));
            Debug.Assert(coItemDict.All(kvp => coItemDict.KeyComparer.Equals(kvp.Key, itemDict[coItemDict[kvp.Key]])));

            _itemDict = itemDict;
            _coItemDict = coItemDict;
        }

        /// <summary>
        /// Gets an empty bijective dictionary.
        /// </summary>
        public readonly static BijectiveDictionary<TI, TCI> Empty = new BijectiveDictionary<TI, TCI>(
                ImmutableDictionary<TI, TCI>.Empty,
                ImmutableDictionary<TCI, TI>.Empty);

        /// <summary>
        /// Gets the number of associations in the bijective dictionary.
        /// </summary>
        /// <remarks>
        /// As the dictionary is bijective, the number of items is always equal to the number of co-items.
        /// </remarks>
        public int Count => _itemDict.Count;

        /// <summary>
        /// Gets a value that indicates whether this bijective dictionary is empty.
        /// </summary>
        public bool IsEmpty => _itemDict.IsEmpty;

        /// <summary>
        /// Gets the co-item associated with the specified item.
        /// </summary>
        /// <param name="item">The specified item.</param>
        /// <returns>The co-item wrapped in a <see cref="Maybe{T}"/>. None if item not found.</returns>
        public Maybe<TCI> this[TI item] => _itemDict.TryGetValue(item, out var coItem) ? Some(coItem) : None<TCI>();

        /// <summary>
        /// Gets the item associated with the specified co-item.
        /// </summary>
        /// <param name="coItem">The specified co-item.</param>
        /// <returns>The item wrapped in a <see cref="Maybe{T}"/>. None if co-item not found.</returns>
        public Maybe<TI> this[TCI coItem] => _coItemDict.TryGetValue(coItem, out var item) ? Some(item) : None<TI>();

        /// <summary>
        /// Gets the item comparer for the bijective dictionary.
        /// </summary>
        public IEqualityComparer<TI> ItemComparer => _itemDict.KeyComparer;

        /// <summary>
        /// Gets the items in the bijective dictionary.
        /// </summary>
        public IEnumerable<TI> Items => _itemDict.Keys;

        /// <summary>
        /// Gets the co-item comparer for the bijective dictionary.
        /// </summary>
        public IEqualityComparer<TCI> CoItemComparer => _itemDict.ValueComparer;

        /// <summary>
        /// Gets the co-items in the bijective dictionary.
        /// </summary>
        public IEnumerable<TCI> CoItems => _coItemDict.Keys;

        /// <summary>
        /// Manipulate dictionary with copy on write semantics.
        /// </summary>
        /// <param name="itemDict">Item dictionary</param>
        /// <param name="coItemDict">Co-item dictionary</param>
        /// <returns>A new bijective dictionary with the given items and co-items if any of item or co-item dictionaries were manipulated; otherwise, the same instance.</returns>
        private BijectiveDictionary<TI, TCI> With(
            ImmutableDictionary<TI, TCI> itemDict,
            ImmutableDictionary<TCI, TI> coItemDict)

            => ReferenceEquals(_itemDict, itemDict) && ReferenceEquals(_coItemDict, coItemDict)
                ? this
                : new BijectiveDictionary<TI, TCI>(itemDict, coItemDict);

        /// <summary>
        /// Adds an association with the specified item and co-item to the bijective dictionary.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="coItem">The co-item to add.</param>
        /// <returns>A new bijective dictionary that contains the additional association.</returns>
        public BijectiveDictionary<TI, TCI> Add(TI item, TCI coItem) => With(_itemDict.Add(item, coItem), _coItemDict.Add(coItem, item));

        /// <summary>
        /// Adds the specified item/co-item pairs to the bijective dictionary.
        /// </summary>
        /// <param name="pairs">The item/co-item pairs to add.</param>
        /// <returns>A new bijective dictionary that contains the additional item/co-item pairs.</returns>
        public BijectiveDictionary<TI, TCI> AddRange(IEnumerable<(TI item, TCI coItem)> pairs)

            => pairs.Aggregate(this, (bd, p) => bd.Add(p.item, p.coItem));

        // BUG: inheritdoc does not seem to work with ValueTuple type parameters.
        // <inheritdoc cref="AddRange(IEnumerable{(TI item, TCI coItem)})"/>
        /// <summary>
        /// Adds the specified item/co-item pairs to the bijective dictionary.
        /// </summary>
        /// <param name="pairs">The item/co-item pairs to add.</param>
        /// <returns>A new bijective dictionary that contains the additional item/co-item pairs.</returns>
        public BijectiveDictionary<TI, TCI> AddRange(IEnumerable<KeyValuePair<TI, TCI>> pairs)

            => AddRange(pairs.Select(kvp => (kvp.Key, kvp.Value)));

        /// <summary>
        /// Retrieves an empty bijective dictionary that has the same comparers as this dictionary instance.
        /// </summary>
        /// <returns>An empty bijective dictionary with the same comparers.</returns>
        public BijectiveDictionary<TI, TCI> Clear() => With(_itemDict.Clear(), _coItemDict.Clear());

        public bool Contains(TI item, TCI coItem)
            => _itemDict.TryGetValue(item, out TCI ci) && _itemDict.ValueComparer.Equals(coItem, ci);

        public bool Contains(TI item) => _itemDict.ContainsKey(item);

        public bool Contains(TCI coItem) => _coItemDict.ContainsKey(coItem);

        public IEnumerator<(TI Item, TCI CoItem)> GetEnumerator()
            => _itemDict.Select(kvp => (kvp.Key, kvp.Value)).GetEnumerator();

        /// <summary>
        /// Removes the association with the specified item from the bijective dictionary.
        /// </summary>
        /// <param name="item">The specified item</param>
        /// <returns>A new bijective dictionary with the specified association removed; or this instance if the specified item cannot be found in the dictionary.</returns>
        public BijectiveDictionary<TI, TCI> Remove(TI item)

            => With(_itemDict.Remove(item), _coItemDict.Remove(_itemDict[item]));

        /// <summary>
        /// Removes the association with the specified co-item from the bijective dictionary.
        /// </summary>
        /// <param name="coItem">The specified co-item</param>
        /// <returns>A new bijective dictionary with the specified association removed; or this instance if the specified co-item cannot be found in the dictionary.</returns>
        public BijectiveDictionary<TI, TCI> Remove(TCI coItem)

            => With(_itemDict.Remove(_coItemDict[coItem]), _coItemDict.Remove(coItem));

        /// <summary>
        /// Private helper to remove range of associations.
        /// </summary>
        /// <param name="iciArray">Materialized range of associations to remove</param>
        /// <returns>A new bijective dictionary with the specified associations removed; or this instance if the specified associations cannot be found in the dictionary.</returns>
        /// <remarks>
        /// For performance reasons we materialize item/co-item pairs and remove them from inner dictionaries with ImmutableDictionary<TKey, TValue>.RemoveRange.
        /// The alternative of aggregating removals with BijectiveDictionary<TI, TCI>.Remove would probably be slower.
        /// </remarks>
        private BijectiveDictionary<TI, TCI> RemoveRange(IEnumerable<(TI item, TCI coItem)> iciArray)

            => With(
                _itemDict.RemoveRange(from ici in iciArray select ici.item),
                _coItemDict.RemoveRange(from ici in iciArray select ici.coItem));

        /// <summary>
        /// Removes the associations with the specified items from the bijective dictionary.
        /// </summary>
        /// <param name="items">The items of the associations to remove.</param>
        /// <returns>A new bijective dictionary with the specified associations removed; or this instance if the specified items cannot be found in the dictionary.</returns>
        public BijectiveDictionary<TI, TCI> RemoveRange(IEnumerable<TI> items)
        {
            var filterExistingPairs = from i in items
                                      from ci in this[i].AsEnumerable()
                                      select (item: i, coItem: ci);

            return RemoveRange(filterExistingPairs.ToArray());
        }

        /// <summary>
        /// Removes the associations with the specified co-items from the bijective dictionary.
        /// </summary>
        /// <param name="coItems">The co-items of the associations to remove.</param>
        /// <returns>A new bijective dictionary with the specified associations removed; or this instance if the specified co-items cannot be found in the dictionary.</returns>
        public BijectiveDictionary<TI, TCI> RemoveRange(IEnumerable<TCI> coItems)
        {
            var filterExistingPairs = from ci in coItems
                                      from i in this[ci].AsEnumerable()
                                      select (item: i, coItem: ci);

            return RemoveRange(filterExistingPairs.ToArray());
        }

        /// <summary>
        /// Sets the specified item and co-item association in the bijective dictionary, possibly overwriting removing one or two previous associations.
        /// </summary>
        /// <param name="item">The item of the association to set.</param>
        /// <param name="coItem">The co-item of the association to set.</param>
        /// <returns>A new bijective dictionary that contains the specified association.</returns>
        /// <remarks>
        /// If the specified association already exist in the dictionary, this method returns the existing instance of the dictionary.
        /// If the item and/or co-item already exist but with different association(s), this method returns a new instance of the dictionary with it or them replaced by the specified association.
        /// </remarks>
        public BijectiveDictionary<TI, TCI> SetItem(TI item, TCI coItem)

            => Contains(item, coItem)
                ? this
                : Remove(item)
                 .Remove(coItem)
                 .Add(item, coItem);

        /// <summary>
        /// Sets the specified associations in the bijective dictionary, possibly overwriting on or two previous associations per new association.
        /// </summary>
        /// <param name="pairs">The associations to set.</param>
        /// <returns>A new bijective dictionary that contains the specified associations.</returns>
        /// <remarks>
        /// If all the specified associations already exist in the dictionary, this method returns the existing instance of the dictionary.
        /// If any specified item or co-item already exist but with different association(s), this method returns a new instance of the dictionary with it or them replaced by the specified association(s).
        /// This implementation can probably be more efficient. The cleverness of a community is welcome.
        /// </remarks>
        public BijectiveDictionary<TI, TCI> SetItems(IEnumerable<(TI item, TCI coItem)> pairs)
        {
            var newPairs = pairs.Where(p => !Contains(p.item, p.coItem)).ToArray();

            return newPairs.Length == 0
                ? this
                : RemoveRange(from p in newPairs select p.item)
                 .RemoveRange(from p in newPairs select p.coItem)
                 .AddRange(newPairs);
        }

        /// <summary>
        /// Gets an instance of the bijective dictionary that uses the specified comparers.
        /// </summary>
        /// <param name="itemComparer">Comparer to use to determine the equality of items in the dictionary.</param>
        /// <param name="coItemComparer">Comparer to use to determine the equality of co-items in the dictionary.</param>
        /// <returns>A new bijective dictionary that uses the given comparers if any comparer is different from the current ones; otherwise, the same instance.</returns>
        /// <remarks>
        /// The equallity comparison of comparers is done by the implementation of <see cref="ImmutableDictionary{TKey, TValue}.WithComparers(IEqualityComparer{TKey}, IEqualityComparer{TValue})"/>.
        /// </remarks>
        public BijectiveDictionary<TI, TCI> WithComparers(
            IEqualityComparer<TI> itemComparer,
            IEqualityComparer<TCI> coItemComparer)

            => With(_itemDict.WithComparers(itemComparer, coItemComparer), _coItemDict.WithComparers(coItemComparer, itemComparer));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
