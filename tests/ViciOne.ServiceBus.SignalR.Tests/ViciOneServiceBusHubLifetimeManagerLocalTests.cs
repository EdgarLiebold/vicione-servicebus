using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class ViciOneServiceBusHubLifetimeManagerLocalTests : IAsyncLifetime
{
    private static readonly TimeSpan NoMessageWindow = TimeSpan.FromMilliseconds(250);
    private static readonly CancellationToken SnapshotOnly = new(canceled: true);
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
    public async Task SendAll_DeliversToEveryConnectedClient()
    {
        await using var first = new HubConnectionTestClient();
        await using var second = new HubConnectionTestClient();
        await ConnectAsync(first, second);

        await _endpoint.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.All.Consumed.Any<All<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(first);
        await AssertInvocationAsync(second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "disconnected-client-excluded")]
    public async Task SendAll_DoesNotDeliverToADisconnectedClient()
    {
        await using var connected = new HubConnectionTestClient();
        await using var disconnected = new HubConnectionTestClient();
        await ConnectAsync(connected, disconnected);
        await _endpoint.Manager.OnDisconnectedAsync(disconnected.HubConnection);

        await _endpoint.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.All.Consumed.Any<All<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(connected);
        await AssertNoInvocationAsync(disconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "only-members-receive")]
    public async Task SendGroup_DeliversOnlyToGroupMembers()
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

        Assert.True(await _endpoint.Group.Consumed.Any<Group<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(member);
        await AssertNoInvocationAsync(outsider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "disconnect-removes-membership")]
    public async Task Disconnect_RemovesEveryGroupMembership()
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

        Assert.Equal(1, _endpoint.Manager.Groups["group"]?.Count);

        await _endpoint.Manager.SendGroupAsync(
            "group",
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Group.Consumed.Any<Group<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(remaining);
        await AssertNoInvocationAsync(disconnected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "local-remove-missing-is-no-op")]
    public async Task RemoveFromGroup_ForLocalNonMember_DoesNotPublishGroupManagement()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);

        await _endpoint.Manager.RemoveFromGroupAsync(
            client.HubConnection.ConnectionId,
            "group",
            TestContext.Current.CancellationToken);

        Assert.Empty(
            _endpoint.GroupManagement.Consumed
                .Select<GroupManagement<TestHub>>(SnapshotOnly));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "duplicate-local-add-is-idempotent")]
    public async Task AddToGroup_Twice_DeliversOnlyOnce()
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
    public async Task SendGroup_WhenOneConnectionWriteFails_ContinuesServingOtherMembers()
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
                     entry.Message == "Failed to write message",
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
    public async Task SendUser_DeliversToEveryConnectionForOnlyThatUser()
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

        Assert.True(await _endpoint.User.Consumed.Any<User<TestHub>>(TestContext.Current.CancellationToken));
        await AssertInvocationAsync(first);
        await AssertInvocationAsync(second);
        await AssertNoInvocationAsync(other);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CONNECTION", "exact-local-id")]
    public async Task SendConnection_DeliversToTheExactLocalConnection()
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
    public async Task SendConnection_WithDifferentIdCasing_DoesNotReachTheLocalConnection()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);

        await _endpoint.Manager.SendConnectionAsync(
            client.HubConnection.ConnectionId.ToUpperInvariant(),
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.Connection.Consumed.Any<Connection<TestHub>>(TestContext.Current.CancellationToken));
        await AssertNoInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "exact-exclusion")]
    public async Task SendAllExcept_ExcludesTheExactConnectionId()
    {
        await using var client = new HubConnectionTestClient();
        await ConnectAsync(client);

        await _endpoint.Manager.SendAllExceptAsync(
            "Hello",
            ["World"],
            [client.HubConnection.ConnectionId],
            TestContext.Current.CancellationToken);

        Assert.True(await _endpoint.All.Consumed.Any<All<TestHub>>(TestContext.Current.CancellationToken));
        await AssertNoInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-BROADCAST", "exclusion-comparison-is-ordinal")]
    public async Task SendAllExcept_WithDifferentIdCasing_DoesNotExcludeTheConnection()
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
    public async Task SendGroupExcept_ExcludesTheExactConnectionId()
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

        Assert.True(await _endpoint.Group.Consumed.Any<Group<TestHub>>(TestContext.Current.CancellationToken));
        await AssertNoInvocationAsync(client);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "member-exclusion-comparison-is-ordinal")]
    public async Task SendGroupExcept_WithDifferentIdCasing_DoesNotExcludeTheConnection()
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
    public async Task DisconnectingOneUserConnection_PreservesTheOtherSubscription()
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

public sealed class TestHub : Hub;
