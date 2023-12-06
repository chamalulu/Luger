using CsCheck;

using Xunit.Abstractions;

namespace Luger.TypeClasses.Tests;

public class MonoidTests(ITestOutputHelper testOutputHelper)
{
    readonly ITestOutputHelper testOutputHelper = testOutputHelper;

    static void IdentityTest<T, TMonoid>(T x) where TMonoid : IMonoid<TMonoid, T>
    {
        var id = TMonoid.Identity;
        var op = TMonoid.Operation;

        Assert.Equal(op(id, x), x);
        Assert.Equal(op(x, id), x);
    }

    [Fact]
    public void NumericsSumInstance_Int32_Identity()
    
        => Gen.Int.Sample(IdentityTest<int, NumericsSumInstance<int>>, testOutputHelper.WriteLine);

    [Fact]
    public void NumericsProductInstance_Int32_Identity()

        => Gen.Int.Sample(IdentityTest<int, NumericsProductInstance<int>>, testOutputHelper.WriteLine);

    [Fact]
    public void EnumerableConcatInstance_Char_Identity()

        => Gen.Char.List.Sample(
            IdentityTest<IEnumerable<char>, EnumerableConcatInstance<char>>,
            testOutputHelper.WriteLine);

    static void AssociativityTest<T, TMonoid>(T a, T b, T c) where TMonoid : IMonoid<TMonoid, T>
    {
        var op = TMonoid.Operation;

        Assert.Equal(op(op(a, b), c), op(a, op(b, c)));
    }

    [Fact]
    public void NumericsSumInstance_Int32_Associativity()

        => Gen.Select(Gen.Int, Gen.Int, Gen.Int).Sample(
            AssociativityTest<int, NumericsSumInstance<int>>,
            testOutputHelper.WriteLine);
        
    [Fact]
    public void NumericsProductInstance_Int32_Associativity()

        => Gen.Select(Gen.Int, Gen.Int, Gen.Int).Sample(
            AssociativityTest<int, NumericsProductInstance<int>>,
            testOutputHelper.WriteLine);
    
    [Fact]
    public void EnumerableConcatInstance_Char_Associativity()

        => Gen.Select(Gen.Char.List, Gen.Char.List, Gen.Char.List).Sample(
            AssociativityTest<IEnumerable<char>, EnumerableConcatInstance<char>>,
            testOutputHelper.WriteLine);
}
