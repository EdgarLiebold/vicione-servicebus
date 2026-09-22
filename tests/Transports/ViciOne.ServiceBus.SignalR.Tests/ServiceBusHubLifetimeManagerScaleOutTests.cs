using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class ServiceBusHubLifetimeManagerScaleOutTests : IAsyncLifetime
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

        Assert.True(await _first.Broadcast.Consumed.AnyAsync<BroadcastMessage<TestHub>>(TestContext.Current.CancellationToken));
        Assert.True(await _second.Broadcast.Consumed.AnyAsync<BroadcastMessage<TestHub>>(TestContext.Current.CancellationToken));
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

        Assert.True(await _first.Broadcast.Consumed.AnyAsync<BroadcastMessage<TestHub>>(TestContext.Current.CancellationToken));
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

        Assert.True(await _first.Connection.Consumed.AnyAsync<ConnectionMessage<TestHub>>(TestContext.Current.CancellationToken));
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

        Assert.True(await _first.Group.Consumed.AnyAsync<GroupMessage<TestHub>>(TestContext.Current.CancellationToken));
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
            await _first.GroupCommand.Consumed.AnyAsync<GroupCommand<TestHub>>(
                TestContext.Current.CancellationToken));
        var response = await acknowledgement.WaitAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        Assert.Equal(_first.Manager.NodeId, response.Message.NodeId);
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
        Assert.Equal(_first.Manager.NodeId, response.Message.NodeId);

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
        Assert.True(await _first.Group.Consumed.AnyAsync<GroupMessage<TestHub>>(TestContext.Current.CancellationToken));
        Guid? firstBackplaneMessageId = Assert.Single(
            _first.Group.Consumed.Snapshot<GroupMessage<TestHub>>()).Context.MessageId;
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

        Assert.True(await _first.Group.Consumed.AnyAsync<GroupMessage<TestHub>>(
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

        Assert.Empty(_first.Connection.Consumed.Snapshot<ConnectionMessage<TestHub>>());
        Assert.Empty(_second.Connection.Consumed.Snapshot<ConnectionMessage<TestHub>>());
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
            await _second.Connection.Consumed.AnyAsync<ConnectionMessage<TestHub>>(
                TestContext.Current.CancellationToken));
        var failure = await _environment.ObserveLogAsync(
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message.StartsWith(
                         "A SignalR connection invocation could not be written to connection ",
                         StringComparison.Ordinal),
            TestContext.Current.CancellationToken);
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "duplicate-remote-delivery-is-idempotent")]
    public async Task ClientResultInvocation_DuplicateBackplaneDelivery_WritesOnlyOnceAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        const string invocationId = "duplicate-invocation";
        var message = new ConnectionMessage<TestHub>(
            client.HubConnection.ConnectionId,
            new[] { new JsonHubProtocol() }.SerializeInvocation("GetAnswer", [], invocationId),
            invocationId,
            _second.Manager.NodeId);

        await _environment.PublishAsync(message, TestContext.Current.CancellationToken);
        await _environment.PublishAsync(message, TestContext.Current.CancellationToken);
        Assert.Equal(
            2,
            await _first.Connection.Consumed
                .SelectAsync<ConnectionMessage<TestHub>>(TestContext.Current.CancellationToken)
                .Take(2)
                .CountObservedAsync(TestContext.Current.CancellationToken));

        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        Assert.Equal(invocationId, invocation.InvocationId);
        Assert.Null(client.TryReadInvocation());

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.WithResult(invocationId, 42));
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "unsupported-group-command-faults")]
    public async Task GroupCommand_WithUnsupportedAction_FaultsWithoutChangingMembershipAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        var command = new GroupCommand<TestHub>(
            (GroupCommandAction)int.MaxValue,
            "group",
            client.HubConnection.ConnectionId);

        RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
            _environment.RequestGroupCommandAsync(command, TestContext.Current.CancellationToken));

        ExceptionInfo fault = Assert.Single(exception.Fault!.Exceptions);
        Assert.Equal(typeof(InvalidDataException).FullName, fault.ExceptionType);
        Assert.Equal(
            $"Unsupported SignalR group command action '{int.MaxValue}'.",
            fault.Message);
        Assert.Empty(_first.Manager.Groups.GetConnections("group"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SCALEOUT", "remote-group-cancellation-propagates")]
    public async Task AddToGroup_ForUnknownRemoteConnection_PropagatesCancellationAsync()
    {
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        Task<bool> consumed = _first.GroupCommand.Consumed.AnyAsync<GroupCommand<TestHub>>(
            TestContext.Current.CancellationToken);
        Task operation = _second.Manager.AddToGroupAsync(
            "unknown-connection",
            "group",
            cancellationSource.Token);

        Assert.True(await consumed);
        Assert.False(operation.IsCompleted);
        cancellationSource.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
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
