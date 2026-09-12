using Microsoft.AspNetCore.SignalR.Protocol;
using ViciOne.ServiceBus.SignalR.Contracts;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class ServiceBusHubLifetimeManagerBoundaryTests : IAsyncLifetime
{
    private HubLifetimeManagerTestEnvironment<TestHub> _environment = null!;
    private SignalRBackplaneEndpoint<TestHub> _endpoint = null!;

    public async ValueTask InitializeAsync()
    {
        _environment = await HubLifetimeManagerTestEnvironment<TestHub>.StartAsync(1).ConfigureAwait(false);
        _endpoint = _environment.Endpoints[0];
    }

    public ValueTask DisposeAsync() => _environment.DisposeAsync();

    [Fact]
    public async Task ConnectionLifecycle_RejectsMissingConnectionsAsync()
    {
        Assert.Equal(
            "connection",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.OnConnectedAsync(null!))).ParamName);
        Assert.Equal(
            "connection",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.OnDisconnectedAsync(null!))).ParamName);
    }

    [Fact]
    public async Task SendOperations_RejectMissingTargetCollectionsAsync()
    {
        Assert.Equal(
            "excludedConnectionIds",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendAllExceptAsync(
                    "Method",
                    [],
                    null!,
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "connectionIds",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendConnectionsAsync(
                    null!,
                    "Method",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "excludedConnectionIds",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendGroupExceptAsync(
                    "group",
                    "Method",
                    [],
                    null!,
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "groupNames",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendGroupsAsync(
                    null!,
                    "Method",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "userIds",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendUsersAsync(
                    null!,
                    "Method",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    public async Task SendOperations_RejectMissingTargetIdentifiersAsync()
    {
        Assert.Equal(
            "connectionId",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendConnectionAsync(
                    null!,
                    "Method",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "groupName",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendGroupAsync(
                    null!,
                    "Method",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "groupName",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendGroupExceptAsync(
                    null!,
                    "Method",
                    [],
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "userId",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendUserAsync(
                    null!,
                    "Method",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "connectionId",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.AddToGroupAsync(
                    null!,
                    "group",
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "groupName",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.RemoveFromGroupAsync(
                    "connection",
                    null!,
                    TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    public async Task SendOperations_RejectInvalidInvocationDataAsync()
    {
        Assert.Equal(
            "methodName",
            (await Assert.ThrowsAsync<ArgumentException>(() =>
                _endpoint.Manager.SendAllAsync(
                    " ",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "methodName",
            (await Assert.ThrowsAsync<ArgumentException>(() =>
                _endpoint.Manager.SendConnectionsAsync(
                    ["connection"],
                    " ",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "methodName",
            (await Assert.ThrowsAsync<ArgumentException>(() =>
                _endpoint.Manager.SendGroupAsync(
                    "group",
                    " ",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "methodName",
            (await Assert.ThrowsAsync<ArgumentException>(() =>
                _endpoint.Manager.SendUsersAsync(
                    ["user"],
                    " ",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "args",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendConnectionAsync(
                    "connection",
                    "Method",
                    null!,
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "args",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SendUserAsync(
                    "user",
                    "Method",
                    null!,
                    TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    public async Task ClientResultOperations_RejectInvalidInputsBeforeTrackingAsync()
    {
        Assert.Equal(
            "connectionId",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.InvokeConnectionAsync<int>(
                    null!,
                    "Method",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "methodName",
            (await Assert.ThrowsAsync<ArgumentException>(() =>
                _endpoint.Manager.InvokeConnectionAsync<int>(
                    "connection",
                    " ",
                    [],
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "args",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.InvokeConnectionAsync<int>(
                    "connection",
                    "Method",
                    null!,
                    TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(
            "connectionId",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SetConnectionResultAsync(
                    null!,
                    CompletionMessage.WithResult("invocation", 1)))).ParamName);
        Assert.Equal(
            "result",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _endpoint.Manager.SetConnectionResultAsync("connection", null!))).ParamName);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            _endpoint.Manager.SetConnectionResultAsync(
                "connection",
                CompletionMessage.WithResult(" ", 1)));
        Assert.Equal(
            "invocationId",
            Assert.Throws<ArgumentNullException>(() =>
                _endpoint.Manager.TryGetReturnType(null!, out _)).ParamName);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _endpoint.Manager.InvokeConnectionAsync<int>(
                "connection",
                "Method",
                [],
                cancellationSource.Token));
        Assert.Equal(0, _endpoint.Manager.PendingClientInvocationCount);
        Assert.Empty(_endpoint.Connection.Consumed.Snapshot<ConnectionMessage<TestHub>>());
    }
}
