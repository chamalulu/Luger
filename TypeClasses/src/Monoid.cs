using System.Numerics;

namespace Luger.TypeClasses;

public interface IMonoid<TSelf, TSet> where TSelf : IMonoid<TSelf, TSet>
{
    static abstract TSet Identity { get; }

    static abstract TSet Operation(TSet left, TSet right);

    static virtual TSet Concat(IEnumerable<TSet> ts) => ts.Aggregate(TSelf.Identity, TSelf.Operation);
}

public class NumericsSumInstance<TSet> : IMonoid<NumericsSumInstance<TSet>, TSet>
    where TSet : IAdditiveIdentity<TSet, TSet>, IAdditionOperators<TSet, TSet, TSet>
{
    public static TSet Identity => TSet.AdditiveIdentity;

    public static TSet Operation(TSet left, TSet right) => left + right;
}

public class NumericsProductInstance<TSet> : IMonoid<NumericsProductInstance<TSet>, TSet>
    where TSet : IMultiplicativeIdentity<TSet, TSet>, IMultiplyOperators<TSet, TSet, TSet>
{
    public static TSet Identity => TSet.MultiplicativeIdentity;

    public static TSet Operation(TSet left, TSet right) => left * right;
}

public class EnumerableConcatInstance<TSource> : IMonoid<EnumerableConcatInstance<TSource>, IEnumerable<TSource>>
{
    public static IEnumerable<TSource> Identity => Enumerable.Empty<TSource>();

    public static IEnumerable<TSource> Operation(IEnumerable<TSource> left, IEnumerable<TSource> right)
    
        => Enumerable.Concat(left, right);
}
