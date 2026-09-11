using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class ViciOneServiceBusHubLifetimeManagerScaleOutTests : IAsyncLifetime
{
    private static readonly TimeSpan NoMessageWindow = TimeSpan.FromMilliseconds(250);
    private HubLifetimeManagerTestEnvironment<TestHub> _environment = null!;
    private SignalRBackplaneEndpoint<TestHub> _first = null!;
    private SignalRBackplaneEndpoint<TestHub> _second = null!;

    public async ValueTask InitializeAsync()
    {
        _environment = await HubLifetimeManagerTestEnvironment<TestHub>.StartAsync(2).ConfigureAwait(false);
        _first = _environment.Endpoints[0];
        _second = _environment.Endpoints[1];
    }

    public ValueTask DisposeAsync() => _environment.DisposeAsync();

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "broadcast-crosses-server-boundary")]
    public async Task SendAll_FromOneServer_ReachesConnectionsOnBothServersAsync()
    {
        await using var firstClient = new HubConnectionTestClient();
        await using var secondClient = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(firstClient.HubConnection);
        await _second.Manager.OnConnectedAsync(secondClient.HubConnection);

        await _first.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _first.All.Consumed.AnyAsync<All<TestHub>>(TestContext.Current.CancellationToken));
        Assert.True(await _second.All.Consumed.AnyAsync<All<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(firstClient);
        await AssertInvocationAsync(secondClient);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "disconnected-remote-client-excluded")]
    public async Task SendAll_DoesNotReachADisconnectedConnectionOnAnotherServerAsync()
    {
        await using var connected = new HubConnectionTestClient();
        await using var disconnected = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(connected.HubConnection);
        await _second.Manager.OnConnectedAsync(disconnected.HubConnection);
        await _second.Manager.OnDisconnectedAsync(disconnected.HubConnection);

        await _second.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _first.All.Consumed.AnyAsync<All<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(connected);
        await AssertNoInvocationAsync(disconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "connection-routing-crosses-server-boundary")]
    public async Task SendConnection_FromNonOwningServer_ReachesTheOwningServerAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        await _second.Manager.SendConnectionAsync(
            client.HubConnection.ConnectionId,
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _first.Connection.Consumed.AnyAsync<Connection<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "group-routing-crosses-server-boundary")]
    public async Task SendGroup_FromNonOwningServer_ReachesTheRemoteGroupMemberAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        await _first.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        await _second.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _first.Group.Consumed.AnyAsync<Group<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "remote-remove-missing-acknowledged")]
    public async Task RemoveFromGroup_OnNonOwningServer_AcknowledgesTheOwningServerAsync()
    {
        await using var client = new HubConnectionTestClient();
        var acknowledgement = _environment.ObserveNextAcknowledgementAsync();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        await _second.Manager.RemoveFromGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        Assert.True(
            await _first.GroupManagement.Consumed.AnyAsync<GroupManagement<TestHub>>(
                TestContext.Current.CancellationToken));
        var response = await acknowledgement.WaitAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        Assert.Equal(_first.Manager.ServerName, response.Message.ServerName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "remote-add-acknowledged-and-effective")]
    public async Task AddToGroup_OnNonOwningServer_AcknowledgesAndAddsTheRemoteConnectionAsync()
    {
        await using var client = new HubConnectionTestClient();
        var acknowledgement = _environment.ObserveNextAcknowledgementAsync();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        await _second.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        var response = await acknowledgement.WaitAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        Assert.Equal(_first.Manager.ServerName, response.Message.ServerName);

        await _second.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);
        await AssertInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "duplicate-remote-add-is-idempotent")]
    public async Task AddToGroup_RemotelyAfterLocalAdd_DoesNotDuplicateDeliveryAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        await _first.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);
        await _second.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        await _second.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(client);
        Assert.Null(client.TryReadInvocation());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "remote-remove-is-effective")]
    public async Task RemoveFromGroup_OnNonOwningServer_RemovesTheRemoteConnectionAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        await _first.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);
        await _second.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);
        Assert.True(await _first.Group.Consumed.AnyAsync<Group<TestHub>>(TestContext.Current.CancellationToken));
        Guid? firstBackplaneMessageId = Assert.Single(
            _first.Group.Consumed.Snapshot<Group<TestHub>>()).Context.MessageId;
        Assert.NotNull(firstBackplaneMessageId);
        await AssertInvocationAsync(client);

        await _second.Manager.RemoveFromGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);
        await _second.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _first.Group.Consumed.AnyAsync<Group<TestHub>>(
            consumed => consumed.Context.MessageId != firstBackplaneMessageId,
            TestContext.Current.CancellationToken));
        await AssertNoInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "local-connection-bypasses-backplane")]
    public async Task SendConnection_ForLocalConnection_DoesNotPublishToTheBackplaneAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        await _first.Manager.SendConnectionAsync(
            client.HubConnection.ConnectionId,
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.Empty(_first.Connection.Consumed.Snapshot<Connection<TestHub>>());
        Assert.Empty(_second.Connection.Consumed.Snapshot<Connection<TestHub>>());
        await AssertInvocationAsync(client);
        Assert.Null(client.TryReadInvocation());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "remote-write-failure-is-contained")]
    public async Task SendConnection_RemoteWriteFailure_DoesNotEscapeThePublishingServerAsync()
    {
        await using var client = new HubConnectionTestClient(failWrites: true);
        await _second.Manager.OnConnectedAsync(client.HubConnection);

        await _first.Manager.SendConnectionAsync(
            client.HubConnection.ConnectionId,
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(
            await _second.Connection.Consumed.AnyAsync<Connection<TestHub>>(
                TestContext.Current.CancellationToken));
        var failure = await _environment.ObserveLogAsync(
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message == "Failed to write message",
            TestContext.Current.CancellationToken);
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    private static async Task AssertInvocationAsync(HubConnectionTestClient client)
    {
        InvocationMessage message = await client.ReadInvocationAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.Equal("Hello", message.Target);
        Assert.Single(message.Arguments);
        Assert.Equal("World", message.Arguments[0]?.ToString());
    }

    private static async Task AssertNoInvocationAsync(HubConnectionTestClient client)
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await client.ReadInvocationAsync(NoMessageWindow, TestContext.Current.CancellationToken));
    }
}
