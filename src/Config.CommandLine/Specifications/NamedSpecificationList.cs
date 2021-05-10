using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Luger.Configuration.CommandLine.Specifications
{
    public class NamedSpecificationList<TSpec> : IReadOnlyList<TSpec> where TSpec : NamedSpecification
    {
        public static readonly NamedSpecificationList<TSpec> Empty = new(ImmutableList<TSpec>.Empty);

        private readonly ImmutableList<TSpec> _list;

        private NamedSpecificationList(ImmutableList<TSpec> list) => _list = list;

        public NamedSpecificationList(TSpec item) : this(ImmutableList.Create(item)) { }

        public TSpec this[int index] => ((IReadOnlyList<TSpec>)_list)[index];

        public IEnumerable<SpecificationName> Names => _list.Select(s => s.Name);

        public int Count => ((IReadOnlyCollection<TSpec>)_list).Count;

        public IEnumerator<TSpec> GetEnumerator() => ((IEnumerable<TSpec>)_list).GetEnumerator();

        public NamedSpecificationList<TSpec> Add(TSpec specification)

            => Names.Contains(specification.Name, new SpecificationNameEqualityComparer(StringComparison.OrdinalIgnoreCase))
                ? throw new ArgumentException($"Duplicate name '{specification.Name}'.", nameof(specification))
                : new(_list.Add(specification));

        public NamedSpecificationList<TSpec> AddRange(IEnumerable<TSpec> specifications)

            => specifications.Aggregate(this, (acc, spec) => acc.Add(spec));

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();

        public override string ToString() => _list.Count == 0 ? "[]" : $"[ {string.Join(", ", _list)} ]";
    }
}
