using ViciOne.ServiceBus.SignalR.Runtime;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class RuntimeStateTests
{
    [Fact]
    public void PendingInvocations_RejectDuplicateIdentifiersWithoutReplacingTheOriginal()
    {
        var tracker = new PendingClientInvocationTracker();
        PendingClientInvocation original = tracker.AddLocal("invocation", "connection", typeof(int));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            tracker.AddLocal("invocation", "other-connection", typeof(string)));

        Assert.Equal("SignalR invocation 'invocation' is already pending.", exception.Message);
        Assert.True(tracker.TryGet("invocation", out PendingClientInvocation? candidate));
        PendingClientInvocation retained = Assert.IsType<PendingClientInvocation>(candidate);
        Assert.Same(original, retained);
        Assert.Equal("connection", retained.ConnectionId);
        Assert.Equal(typeof(int), retained.ResultType);
    }

    [Fact]
    public async Task SubscriptionIndex_RemovingAnUnknownIdentifier_IsIdempotentAsync()
    {
        var index = new ConnectionSubscriptionIndex();
        await using var client = new HubConnectionTestClient();

        index.RemoveSubscription("missing", client.HubConnection);

        Assert.Equal(0, index.Count);
        Assert.Empty(index.GetConnections("missing"));
    }
}
