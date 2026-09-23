using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga;

public sealed class SagaInstanceEqualityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-IDENTITY", "state-value-and-wrapper-type-control-lookup")]
    public void SagaInstanceEquality_PreservesStateValueAndWrapperTypeInKeyedLookup()
    {
        Guid correlationId = Guid.NewGuid();
        var first = new SagaInstance<EqualitySaga>(new EqualitySaga { CorrelationId = correlationId });
        var equivalent = new SagaInstance<EqualitySaga>(new EqualitySaga { CorrelationId = correlationId });
        var different = new SagaInstance<EqualitySaga>(new EqualitySaga { CorrelationId = Guid.NewGuid() });
        var derived = new DerivedSagaInstance(new EqualitySaga { CorrelationId = correlationId });
        var lookup = new Dictionary<SagaInstance<EqualitySaga>, string> { [first] = "retained" };

        Assert.True(first.Equals(first));
        Assert.True(first.Equals((object)first));
        Assert.True(first.Equals(equivalent));
        Assert.True(first.Equals((object)equivalent));
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal("retained", lookup[equivalent]);

        Assert.False(first.Equals((SagaInstance<EqualitySaga>?)null));
        Assert.False(first.Equals((object?)null));
        Assert.False(first.Equals(different));
        Assert.False(first.Equals((object)"unrelated"));
        Assert.False(lookup.ContainsKey(different));
        Assert.False(first.Equals((SagaInstance<EqualitySaga>)derived));
        Assert.False(first.Equals((object)derived));
        Assert.False(derived.Equals(first));
        Assert.False(lookup.ContainsKey(derived));
    }

    private sealed record EqualitySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class DerivedSagaInstance(EqualitySaga state) : SagaInstance<EqualitySaga>(state);
}
