using System;
using System.Linq;

namespace Luger.Configuration.CommandLine
{
    public sealed partial record ListNode<T>
    {
        public bool Equals(ListNode<T>? other) => other is not null && List.SequenceEqual(other.List);

        public override int GetHashCode()
        {
            var hc = new HashCode();

            List.ForEach(hc.Add);

            return hc.ToHashCode();
        }
    }

    public sealed partial record SetNode<T>
    {
        public bool Equals(SetNode<T>? other) => other is not null && Set.SetEquals(other.Set);

        public override int GetHashCode() => Set.Aggregate(0, (hc, t) => hc ^ t.GetHashCode());
    }

    public abstract partial record NamedNode
    {
        public virtual bool Equals(NamedNode? other, StringComparison comparisonType)

            => other is not null && Name.Equals(other.Name, comparisonType);

        public virtual bool Equals(NamedNode? other) => Equals(other, StringComparison.OrdinalIgnoreCase);

        public virtual int GetHashCode(StringComparison comparisonType) => Name.GetHashCode(comparisonType);

        public override int GetHashCode() => GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
