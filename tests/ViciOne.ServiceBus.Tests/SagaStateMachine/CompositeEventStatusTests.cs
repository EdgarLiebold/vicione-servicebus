using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class CompositeEventStatusTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "status-value-equality-and-hashing")]
    public void EqualBitPatterns_HaveEqualValuesAndHashCodes()
    {
        var left = new CompositeEventStatus(unchecked((int)0xA5A5A5A5));
        var right = new CompositeEventStatus(unchecked((int)0xA5A5A5A5));
        var different = new CompositeEventStatus(unchecked((int)0x5A5A5A5A));

        Assert.True(left.Equals(right));
        Assert.True(left.Equals((object)right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.False(left.Equals(different));
        Assert.NotEqual(left.GetHashCode(), different.GetHashCode());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-COMPOSITE", "status-ordering-full-integer-range")]
    public void Comparison_UsesNaturalOrderingAcrossTheFullIntegerRange()
    {
        var lowest = new CompositeEventStatus(int.MinValue);
        var zero = new CompositeEventStatus(0);
        var highest = new CompositeEventStatus(int.MaxValue);

        Assert.True(lowest.CompareTo(zero) < 0);
        Assert.True(zero.CompareTo(highest) < 0);
        Assert.True(lowest.CompareTo(highest) < 0);
        Assert.Equal(0, highest.CompareTo(new CompositeEventStatus(int.MaxValue)));
    }
}
