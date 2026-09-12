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

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-STATE-PERSISTENCE", "persisted-collection-setters-detach-inputs")]
    public void PersistedCollectionSetters_DetachEveryCallerOwnedCollection()
    {
        Guid first = Guid.Parse("5ce4cbb0-6424-4f67-a155-c1da07aee9d5");
        Guid second = Guid.Parse("963c5759-7cfe-4872-a295-c578f3252cf7");
        var subscription = new FutureSubscription(new Uri("loopback://localhost/results"));
        var pending = new HashSet<Guid> { first };
        var subscriptions = new HashSet<FutureSubscription> { subscription };
        var variables = new Dictionary<string, object> { ["Value"] = "first" };
        var results = new Dictionary<Guid, FutureMessage> { [first] = new FutureMessage() };
        var faults = new Dictionary<Guid, FutureMessage> { [first] = new FutureMessage() };
        var state = new FutureState
        {
            Pending = pending,
            Subscriptions = subscriptions,
            Variables = variables,
            Results = results,
            Faults = faults,
        };

        pending.Add(second);
        subscriptions.Add(new FutureSubscription(new Uri("loopback://localhost/other")));
        variables["Value"] = "mutated";
        results[second] = new FutureMessage();
        faults[second] = new FutureMessage();

        Assert.Equal([first], state.Pending);
        Assert.Equal([subscription], state.Subscriptions);
        Assert.Equal("first", state.Variables["value"]);
        Assert.Equal([first], state.Results.Keys);
        Assert.Equal([first], state.Faults.Keys);
    }
}
