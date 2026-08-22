using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Microsoft.AspNetCore.SignalR.Protocol;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class ViciOneServiceBusHubLifetimeManagerFanOutTests : IAsyncLifetime
{
    private static readonly TimeSpan NoMessageWindow = TimeSpan.FromMilliseconds(250);
    private static readonly CancellationToken SnapshotOnly = new(canceled: true);
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
    [RequirementCoverage("REQ-VSB-SIGNALR-CONNECTION", "multiple-targets")]
    public async Task SendConnections_DeliversToEverySelectedConnectionOnly()
    {
        await using var firstSelected = new HubConnectionTestClient();
        await using var secondSelected = new HubConnectionTestClient();
        await using var notSelected = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(firstSelected.HubConnection);
        await _second.Manager.OnConnectedAsync(secondSelected.HubConnection);
        await _first.Manager.OnConnectedAsync(notSelected.HubConnection);

        await _first.Manager.SendConnectionsAsync(
            [firstSelected.HubConnection.ConnectionId, secondSelected.HubConnection.ConnectionId],
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(firstSelected);
        await AssertInvocationAsync(secondSelected);
        await AssertNoInvocationAsync(notSelected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-GROUP", "multiple-targets-and-empty-name")]
    public async Task SendGroups_DeliversToEveryNamedGroupAndIgnoresEmptyNames()
    {
        await using var firstMember = new HubConnectionTestClient();
        await using var secondMember = new HubConnectionTestClient();
        await using var notSelected = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(firstMember.HubConnection);
        await _second.Manager.OnConnectedAsync(secondMember.HubConnection);
        await _first.Manager.OnConnectedAsync(notSelected.HubConnection);
        await _first.Manager.AddToGroupAsync(
            firstMember.HubConnection.ConnectionId,
            "group-a",
            TestContext.Current.CancellationToken);
        await _second.Manager.AddToGroupAsync(
            secondMember.HubConnection.ConnectionId,
            "group-b",
            TestContext.Current.CancellationToken);

        await _first.Manager.SendGroupsAsync(
            ["group-a", "", "group-b"],
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(firstMember);
        await AssertInvocationAsync(secondMember);
        await AssertNoInvocationAsync(notSelected);

        var publishedGroups = _first.Group.Consumed
            .Select<Group<TestHub>>(SnapshotOnly)
            .Select(received => received.Context.Message.GroupName)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["group-a", "group-b"], publishedGroups);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-USER", "multiple-targets")]
    public async Task SendUsers_DeliversToEverySelectedUserOnly()
    {
        await using var firstSelected = new HubConnectionTestClient("user-a");
        await using var secondSelected = new HubConnectionTestClient("user-b");
        await using var notSelected = new HubConnectionTestClient("user-c");
        await _first.Manager.OnConnectedAsync(firstSelected.HubConnection);
        await _second.Manager.OnConnectedAsync(secondSelected.HubConnection);
        await _first.Manager.OnConnectedAsync(notSelected.HubConnection);

        await _first.Manager.SendUsersAsync(
            ["user-a", "user-b"],
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        await AssertInvocationAsync(firstSelected);
        await AssertInvocationAsync(secondSelected);
        await AssertNoInvocationAsync(notSelected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-FANOUT", "empty-target-sets-are-no-op")]
    public async Task EmptyTargetSets_DoNotPublishToTheBackplane()
    {
        await _first.Manager.SendConnectionsAsync(
            [],
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);
        await _first.Manager.SendGroupsAsync(
            [],
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);
        await _first.Manager.SendUsersAsync(
            [],
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.Empty(
            _first.Connection.Consumed.Select<Connection<TestHub>>(SnapshotOnly));
        Assert.Empty(_first.Group.Consumed.Select<Group<TestHub>>(SnapshotOnly));
        Assert.Empty(_first.User.Consumed.Select<User<TestHub>>(SnapshotOnly));
        Assert.Empty(
            _second.Connection.Consumed.Select<Connection<TestHub>>(SnapshotOnly));
        Assert.Empty(_second.Group.Consumed.Select<Group<TestHub>>(SnapshotOnly));
        Assert.Empty(_second.User.Consumed.Select<User<TestHub>>(SnapshotOnly));
    }

    private static async Task AssertInvocationAsync(HubConnectionTestClient client)
    {
        InvocationMessage message = await client.ReadInvocationAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.Equal("Hello", message.Target);
        Assert.Equal("World", Assert.Single(message.Arguments)?.ToString());
    }

    private static async Task AssertNoInvocationAsync(HubConnectionTestClient client)
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await client.ReadInvocationAsync(NoMessageWindow, TestContext.Current.CancellationToken));
    }
}
