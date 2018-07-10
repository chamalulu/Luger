using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Luger.Functional;
using static Luger.Functional.Optional;

namespace Luger.Utilities
{
    public static class BijectiveDictionary
    {
        public static BijectiveDictionary<TI, TCI> Create<TI, TCI>(params (TI item, TCI coItem)[] pairs)
            => pairs.Aggregate(
                BijectiveDictionary<TI, TCI>.Empty,
                (bd, pair) => bd.Add(pair.item, pair.coItem)
            );

        public static BijectiveDictionary<TI, TCI> ToBijectiveDictionary<TI, TCI>(
            this IEnumerable<KeyValuePair<TI, TCI>> pairs
        )
            => pairs.Aggregate(
                BijectiveDictionary<TI, TCI>.Empty,
                (bd, kvp) => bd.Add(kvp.Key, kvp.Value)
            );
    }

    public class BijectiveDictionary<TI, TCI> : IEnumerable<(TI Item, TCI CoItem)>
    {
        private readonly ImmutableDictionary<TI, TCI> _itemDict;
        private readonly ImmutableDictionary<TCI, TI> _coItemDict;

        private BijectiveDictionary(ImmutableDictionary<TI, TCI> itemDict, ImmutableDictionary<TCI, TI> coItemDict)
            => (_itemDict, _coItemDict) = (itemDict, coItemDict);

        private BijectiveDictionary<TI, TCI> With(
            ImmutableDictionary<TI, TCI> itemDict,
            ImmutableDictionary<TCI, TI> coItemDict
        )
            => object.ReferenceEquals(_itemDict, itemDict) && object.ReferenceEquals(_coItemDict, coItemDict)
                ? this
                : new BijectiveDictionary<TI, TCI>(itemDict, coItemDict);

        public static BijectiveDictionary<TI, TCI> Empty
            = new BijectiveDictionary<TI, TCI>(ImmutableDictionary<TI, TCI>.Empty, ImmutableDictionary<TCI, TI>.Empty);

        public IEnumerable<TI> Items => _itemDict.Keys;

        public IEnumerable<TCI> CoItems => _coItemDict.Keys;

        public int Count => _itemDict.Count;

        public TCI this[TI item] => _itemDict[item];

        public TI this[TCI coItem] => _coItemDict[coItem];

        public BijectiveDictionary<TI, TCI> Clear() => Empty;

        public BijectiveDictionary<TI, TCI> Add(TI item, TCI coItem)
            => this.With(_itemDict.Add(item, coItem), _coItemDict.Add(coItem, item));

        public BijectiveDictionary<TI, TCI> AddRange(IEnumerable<KeyValuePair<TI, TCI>> pairs)
            => pairs.Aggregate(this, (bd, kvp) => bd.Add(kvp.Key, kvp.Value));

        public BijectiveDictionary<TI, TCI> Remove(TI item)
            => With(_itemDict.Remove(item), _coItemDict.Remove(this[item]));
        
        public BijectiveDictionary<TI, TCI> Remove(TCI coItem)
            => With(_itemDict.Remove(this[coItem]), _coItemDict.Remove(coItem));

        public BijectiveDictionary<TI, TCI> RemoveRange(IEnumerable<TI> items)
            => items.Aggregate(this, (bd, item) => Remove(item));

        public BijectiveDictionary<TI, TCI> RemoveRange(IEnumerable<TCI> coItems)
            => coItems.Aggregate(this, (bd, coItem) => Remove(coItem));

        public bool Contains(TI item) => _itemDict.ContainsKey(item);

        public bool Contains(TCI coItem) => _coItemDict.ContainsKey(coItem);

        public Optional<TCI> OptGetValue(TI item)
            => _itemDict.TryGetValue(item, out var value)
                ? Some(value)
                : None;
        
        public Optional<TI> OptGetValue(TCI coItem)
            => _coItemDict.TryGetValue(coItem, out var value)
                ? Some(value)
                : None;

        public IEnumerator<(TI Item, TCI CoItem)> GetEnumerator()
            => _itemDict.Select(kvp => (kvp.Key, kvp.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
