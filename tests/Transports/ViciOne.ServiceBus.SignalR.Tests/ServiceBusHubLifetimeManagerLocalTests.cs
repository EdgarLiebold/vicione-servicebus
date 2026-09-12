using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class ServiceBusHubLifetimeManagerLocalTests : IAsyncLifetime
{
    private static readonly TimeSpan NoMessageWindow = TimeSpan.FromMilliseconds(250);
    private HubLifetimeManagerTestEnvironment<TestHub> _environment = null!;
    private SignalRBackplaneEndpoint<TestHub> _endpoint = null!;

    public async ValueTask InitializeAsync()
    {
        _environment = await HubLifetimeManagerTestEnvironment<TestHub>.StartAsync(1).ConfigureAwait(false);
        _endpoint = _environment.Endpoints[0];
    }

    public ValueTask DisposeAsync() => _environment.DisposeAsync();

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "all-local-connections")]
    public async Task SendAll_DeliversToEveryConnectedClientAsync()
    {
        await using var first = new HubConnectionTestClient();
        await using var second = new HubConnectionTestClient();
        await ConnectAsync(first, second);

        await _endpoint.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Broadcast.Consumed.AnyAsync<BroadcastMessage<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(first);
        await AssertInvocationAsync(second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "disconnected-client-excluded")]
    public async Task SendAll_DoesNotDeliverToADisconnectedClientAsync()
    {
        await using var connected = new HubConnectionTestClient();
        await using var disconnected = new HubConnectionTestClient();
        await ConnectAsync(connected, disconnected);
        await _endpoint.Manager.OnDisconnectedAsync(disconnected.HubConnection);

        await _endpoint.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Broadcast.Consumed.AnyAsync<BroadcastMessage<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(connected);
        await AssertNoInvocationAsync(disconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "only-members-receive")]
    public async Task SendGroup_DeliversOnlyToGroupMembersAsync()
    {
        await using var member = new HubConnectionTestClient();
        await using var outsider = new HubConnectionTestClient();
        await ConnectAsync(member, outsider);
        await _endpoint.Manager.AddToGroupAsync(
            member.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        await _endpoint.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Group.Consumed.AnyAsync<GroupMessage<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(member);
        await AssertNoInvocationAsync(outsider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "disconnect-removes-membership")]
    public async Task Disconnect_RemovesEveryGroupMembershipAsync()
    {
        await using var disconnected = new HubConnectionTestClient();
        await using var remaining = new HubConnectionTestClient();
        await ConnectAsync(disconnected, remaining);
        await _endpoint.Manager.AddToGroupAsync(
            disconnected.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);
        await _endpoint.Manager.AddToGroupAsync(
            remaining.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);
        await _endpoint.Manager.OnDisconnectedAsync(disconnected.HubConnection);

        Assert.Single(_endpoint.Manager.Groups.GetConnections("group"));

        await _endpoint.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Group.Consumed.AnyAsync<GroupMessage<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(remaining);
        await AssertNoInvocationAsync(disconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "local-remove-missing-is-no-op")]
    public async Task RemoveFromGroup_ForLocalNonMember_DoesNotPublishAGroupCommandAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);

        await _endpoint.Manager.RemoveFromGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        Assert.Empty(
            _endpoint.GroupCommand.Consumed
                .Snapshot<GroupCommand<TestHub>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "duplicate-local-add-is-idempotent")]
    public async Task AddToGroup_Twice_DeliversOnlyOnceAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);
        await _endpoint.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);
        await _endpoint.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        await _endpoint.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(client);
        Assert.Null(client.TryReadInvocation());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "failed-member-does-not-block-peers")]
    public async Task SendGroup_WhenOneConnectionWriteFails_ContinuesServingOtherMembersAsync()
    {
        await using var failing = new HubConnectionTestClient(failWrites: true);
        await using var healthy = new HubConnectionTestClient();
        await ConnectAsync(failing, healthy);
        await _endpoint.Manager.AddToGroupAsync(
            failing.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);
        await _endpoint.Manager.AddToGroupAsync(
            healthy.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        await _endpoint.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);
        await AssertInvocationAsync(healthy);

        var failure = await _environment.ObserveLogAsync(
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message == "A SignalR group invocation could not be written to every local member of group group.",
            TestContext.Current.CancellationToken);
        Assert.IsType<InvalidOperationException>(failure.Exception);

        await _endpoint.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);
        await AssertInvocationAsync(healthy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-USER", "all-connections-for-user")]
    public async Task SendUser_DeliversToEveryConnectionForOnlyThatUserAsync()
    {
        await using var first = new HubConnectionTestClient("user-a");
        await using var second = new HubConnectionTestClient("user-a");
        await using var other = new HubConnectionTestClient("user-b");
        await ConnectAsync(first, second, other);

        await _endpoint.Manager.SendUserAsync(
            "user-a",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.User.Consumed.AnyAsync<UserMessage<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(first);
        await AssertInvocationAsync(second);
        await AssertNoInvocationAsync(other);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CONNECTION", "exact-local-id")]
    public async Task SendConnection_DeliversToTheExactLocalConnectionAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);

        await _endpoint.Manager.SendConnectionAsync(
            client.HubConnection.ConnectionId,
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CONNECTION", "id-comparison-is-ordinal")]
    public async Task SendConnection_WithDifferentIdCasing_DoesNotReachTheLocalConnectionAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);

        await _endpoint.Manager.SendConnectionAsync(
            client.HubConnection.ConnectionId.ToUpperInvariant(),
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Connection.Consumed.AnyAsync<ConnectionMessage<TestHub>>(TestContext.Current.CancellationToken));
        await AssertNoInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "exact-exclusion")]
    public async Task SendAllExcept_ExcludesTheExactConnectionIdAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);

        await _endpoint.Manager.SendAllExceptAsync(
            "Hello",
            ["World"],
            [client.HubConnection.ConnectionId],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Broadcast.Consumed.AnyAsync<BroadcastMessage<TestHub>>(TestContext.Current.CancellationToken));
        await AssertNoInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "exclusion-comparison-is-ordinal")]
    public async Task SendAllExcept_WithDifferentIdCasing_DoesNotExcludeTheConnectionAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);

        await _endpoint.Manager.SendAllExceptAsync(
            "Hello",
            ["World"],
            [client.HubConnection.ConnectionId.ToUpperInvariant()],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "exact-member-exclusion")]
    public async Task SendGroupExcept_ExcludesTheExactConnectionIdAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);
        await _endpoint.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        await _endpoint.Manager.SendGroupExceptAsync(
            "group",
            "Hello",
            ["World"],
            [client.HubConnection.ConnectionId],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Group.Consumed.AnyAsync<GroupMessage<TestHub>>(TestContext.Current.CancellationToken));
        await AssertNoInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "member-exclusion-comparison-is-ordinal")]
    public async Task SendGroupExcept_WithDifferentIdCasing_DoesNotExcludeTheConnectionAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);
        await _endpoint.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        await _endpoint.Manager.SendGroupExceptAsync(
            "group",
            "Hello",
            ["World"],
            [client.HubConnection.ConnectionId.ToUpperInvariant()],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-USER", "remaining-connection-stays-subscribed")]
    public async Task DisconnectingOneUserConnection_PreservesTheOtherSubscriptionAsync()
    {
        await using var disconnected = new HubConnectionTestClient("user-a");
        await using var remaining = new HubConnectionTestClient("user-a");
        await ConnectAsync(disconnected, remaining);
        await _endpoint.Manager.OnDisconnectedAsync(disconnected.HubConnection);

        await _endpoint.Manager.SendUserAsync(
            "user-a",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(remaining);
        await AssertNoInvocationAsync(disconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "empty-node-is-no-op")]
    public async Task SendAll_WithoutLocalConnections_CompletesWithoutWritesAsync()
    {
        await _endpoint.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Broadcast.Consumed.AnyAsync<BroadcastMessage<TestHub>>(
            TestContext.Current.CancellationToken));
        Assert.Equal(0, _endpoint.Manager.Connections.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "failed-connection-does-not-block-peers")]
    public async Task SendAll_WhenOneConnectionWriteFails_ContinuesServingOtherConnectionsAsync()
    {
        await using var failing = new HubConnectionTestClient(failWrites: true);
        await using var healthy = new HubConnectionTestClient();
        await ConnectAsync(failing, healthy);

        await _endpoint.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(healthy);
        LogEntry failure = await _environment.ObserveLogAsync(
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message == "A SignalR broadcast could not be written to every local connection.",
            TestContext.Current.CancellationToken);
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-USER", "unknown-user-is-no-op")]
    public async Task SendUser_WithoutLocalSubscriptions_CompletesWithoutWritesAsync()
    {
        await _endpoint.Manager.SendUserAsync(
            "unknown-user",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.User.Consumed.AnyAsync<UserMessage<TestHub>>(
            TestContext.Current.CancellationToken));
        Assert.Equal(0, _endpoint.Manager.Users.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-USER", "failed-connection-does-not-block-peers")]
    public async Task SendUser_WhenOneConnectionWriteFails_ContinuesServingOtherConnectionsAsync()
    {
        await using var failing = new HubConnectionTestClient("user", failWrites: true);
        await using var healthy = new HubConnectionTestClient("user");
        await ConnectAsync(failing, healthy);

        await _endpoint.Manager.SendUserAsync(
            "user",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(healthy);
        LogEntry failure = await _environment.ObserveLogAsync(
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message ==
                     "A SignalR user invocation could not be written to every local connection for user user.",
            TestContext.Current.CancellationToken);
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "group-name-comparison-is-ordinal")]
    public async Task GroupNames_AreCaseSensitiveAndEmptyIndexesAreRemovedAsync()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);
        await _endpoint.Manager.AddToGroupAsync(
            client.HubConnection.ConnectionId,
            "Group",
            TestContext.Current.CancellationToken);

        await _endpoint.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertNoInvocationAsync(client);
        Assert.Equal(1, _endpoint.Manager.Groups.Count);

        await _endpoint.Manager.RemoveFromGroupAsync(
            client.HubConnection.ConnectionId,
            "Group",
            TestContext.Current.CancellationToken);

        Assert.Equal(0, _endpoint.Manager.Groups.Count);
        Assert.Empty(_endpoint.Manager.Groups.GetConnections("Group"));
    }

    private async Task ConnectAsync(params HubConnectionTestClient[] clients)
    {
        foreach (var client in clients)
        {
            await _endpoint.Manager.OnConnectedAsync(client.HubConnection);
        }
    }

    private static async Task AssertInvocationAsync(HubConnectionTestClient client)
    {
        var message = await client.ReadInvocationAsync(
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
