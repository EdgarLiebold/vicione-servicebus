using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureStateTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "new-state-has-non-null-persisted-collections")]
    public void NewState_InitializesEveryPersistedCollection()
    {
        var state = new FutureState();

        Assert.Empty(state.Pending);
        Assert.Empty(state.Subscriptions);
        Assert.Empty(state.Variables);
        Assert.Empty(state.Results);
        Assert.Empty(state.Faults);
        Assert.Empty(state.RowVersion);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "null-persisted-collections-rehydrate")]
    public void NullPersistedCollections_AreRehydratedWithTheirRequiredSemantics()
    {
        var state = new FutureState
        {
            Pending = null!,
            Subscriptions = null!,
            Variables = null!,
            Results = null!,
            Faults = null!,
        };

        state.Variables["CorrelationId"] = Guid.Empty;

        Assert.Empty(state.Pending);
        Assert.Empty(state.Subscriptions);
        Assert.True(state.Variables.ContainsKey("correlationid"));
        Assert.Empty(state.Results);
        Assert.Empty(state.Faults);
    }
}
