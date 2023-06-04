using Xunit;

namespace Luger.Functional.Tests;

public class TransitionTests
{
    /// <summary>
    /// Return creates a transition returning given value and unaltered state.
    /// </summary>
    [Fact]
    public void ReturnFact() => Assert.Equal(("banan", 42), Transition<int>.Return("banan")(42));

    /// <summary>
    /// Run runs a transition yielding its result and discarding end state.
    /// </summary>
    [Fact]
    public void RunFact() => Assert.Equal("banan", Transition<int>.Return("banan").Run(42));

}
