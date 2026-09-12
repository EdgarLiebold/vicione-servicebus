using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.SignalR.Configuration;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class ServiceBusHubLifetimeManagerClientResultTests : IAsyncLifetime
{
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
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "local-result-round-trip")]
    public async Task InvokeConnection_LocalClient_ReturnsTypedResultAndClearsTrackingAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        Assert.True(_first.Manager.TryGetReturnType(invocationId, out Type? returnType));
        Assert.Equal(typeof(int), returnType);

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.WithResult(invocationId, 42));

        Assert.Equal(42, await resultTask);
        Assert.False(_first.Manager.TryGetReturnType(invocationId, out _));
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "remote-result-round-trip")]
    public async Task InvokeConnection_RemoteClient_ReturnsTypedResultToRequestingNodeAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _second.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        Assert.True(_second.Manager.TryGetReturnType(invocationId, out Type? returnType));
        Assert.Equal(typeof(int), returnType);
        Assert.True(_first.Manager.TryGetReturnType(invocationId, out Type? forwardingType));
        Assert.Equal(typeof(RawResult), forwardingType);

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.WithResult(invocationId, 42));

        Assert.Equal(42, await resultTask);
        Assert.True(await _second.ClientResult.Consumed.AnyAsync<ClientResultMessage<TestHub>>(
            TestContext.Current.CancellationToken));
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
        Assert.Equal(0, _second.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "client-error-propagates")]
    public async Task InvokeConnection_ClientError_ThrowsHubExceptionAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.WithError(invocationId, "Client rejected the invocation."));

        HubException exception = await Assert.ThrowsAsync<HubException>(() => resultTask);
        Assert.Equal("Client rejected the invocation.", exception.Message);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "missing-result-rejected")]
    public async Task InvokeConnection_ClientCompletesWithoutResult_ThrowsHubExceptionAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.Empty(invocationId));

        HubException exception = await Assert.ThrowsAsync<HubException>(() => resultTask);
        Assert.Equal("The SignalR client completed the invocation without returning a result.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "wrong-result-type-rejected")]
    public async Task InvokeConnection_ResultHasUnexpectedRuntimeType_RejectsResultAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.WithResult(invocationId, "forty-two"));

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() => resultTask);
        Assert.Contains(typeof(string).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(int).ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "wrong-connection-cannot-complete")]
    public async Task SetConnectionResult_WrongConnection_DoesNotCompleteInvocationAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        await _first.Manager.SetConnectionResultAsync(
            "different-connection",
            CompletionMessage.WithResult(invocationId, 13));

        Assert.False(resultTask.IsCompleted);
        Assert.Equal(1, _first.Manager.PendingClientInvocationCount);

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.WithResult(invocationId, 42));
        Assert.Equal(42, await resultTask);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "local-cancellation-forwarded")]
    public async Task InvokeConnection_LocalCancellation_IsForwardedToClientAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            cancellationSource.Token);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resultTask);
        CancelInvocationMessage cancellation = await client.ReadMessageAsync<CancelInvocationMessage>(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        Assert.Equal(invocationId, cancellation.InvocationId);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "null-result-preserved")]
    public async Task InvokeConnection_NullClientResult_ReturnsDefaultAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        Task<string?> resultTask = _first.Manager.InvokeConnectionAsync<string?>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.WithResult(invocationId, null));

        Assert.Null(await resultTask);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "remote-cancellation-forwarded")]
    public async Task InvokeConnection_RemoteCancellation_ReachesOwningClientAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        Task<int> resultTask = _second.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            cancellationSource.Token);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resultTask);
        CancelInvocationMessage cancellation = await client.ReadMessageAsync<CancelInvocationMessage>(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        Assert.Equal(invocationId, cancellation.InvocationId);
        Assert.True(await _first.InvocationCancellation.Consumed.AnyAsync<InvocationCancellationMessage<TestHub>>(
            TestContext.Current.CancellationToken));
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
        Assert.Equal(0, _second.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "local-disconnect-fails-waiter")]
    public async Task Disconnect_LocalPendingInvocation_FailsAndClearsWaiterAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        _ = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);

        await _first.Manager.OnDisconnectedAsync(client.HubConnection);

        IOException exception = await Assert.ThrowsAsync<IOException>(() => resultTask);
        Assert.Contains(client.HubConnection.ConnectionId, exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "remote-disconnect-fails-requester")]
    public async Task Disconnect_RemotePendingInvocation_FailsRequestingNodeAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _second.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        _ = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);

        await _first.Manager.OnDisconnectedAsync(client.HubConnection);

        HubException exception = await Assert.ThrowsAsync<HubException>(() => resultTask);
        Assert.Equal("The SignalR connection disconnected.", exception.Message);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
        Assert.Equal(0, _second.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "remote-write-failure-propagates")]
    public async Task InvokeConnection_RemoteWriteFailure_FailsRequestingNodeAsync()
    {
        await using var client = new HubConnectionTestClient(failWrites: true);
        await _first.Manager.OnConnectedAsync(client.HubConnection);

        Task<int> resultTask = _second.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);

        HubException exception = await Assert.ThrowsAsync<HubException>(() => resultTask);
        Assert.Equal(
            "The owning server could not write the invocation to the SignalR connection.",
            exception.Message);
        Assert.True(await _first.Connection.Consumed.AnyAsync<ConnectionMessage<TestHub>>(
            TestContext.Current.CancellationToken));
        Assert.True(await _second.ClientResult.Consumed.AnyAsync<ClientResultMessage<TestHub>>(
            TestContext.Current.CancellationToken));
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
        Assert.Equal(0, _second.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "stale-remote-failure-is-no-op")]
    public async Task FailRemoteInvocation_WithoutMatchingPendingInvocation_IsIdempotentAsync()
    {
        await _first.Manager.FailRemoteInvocationAsync(
            "missing-connection",
            "missing-invocation",
            "The invocation could not be delivered.");

        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "unknown-result-protocol-rejected")]
    public async Task ReceiveClientResult_UnknownProtocol_FailsPendingInvocationAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        _first.Manager.ReceiveClientResult(
            new ClientResultMessage<TestHub>(_first.Manager.NodeId, invocationId, "unknown", [1]));

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() => resultTask);
        Assert.Contains("is not registered", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "malformed-result-frame-rejected")]
    public async Task ReceiveClientResult_MalformedProtocolFrame_FailsPendingInvocationAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage invocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(invocation.InvocationId);

        _first.Manager.ReceiveClientResult(
            new ClientResultMessage<TestHub>(_first.Manager.NodeId, invocationId, "json", [1]));

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() => resultTask);
        Assert.Equal(
            "The SignalR backplane payload does not contain exactly one client completion.",
            exception.Message);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "mismatched-result-envelope-rejected")]
    public async Task ReceiveClientResult_MismatchedEnvelopeAndFrame_FailsOnlyAddressedInvocationAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        Task<int> firstResultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage firstInvocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string firstInvocationId = Assert.IsType<string>(firstInvocation.InvocationId);
        Task<int> secondResultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage secondInvocation = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string secondInvocationId = Assert.IsType<string>(secondInvocation.InvocationId);
        byte[] payload = HubMessageSerializer.SerializeCompletion(
            new JsonHubProtocol(),
            CompletionMessage.WithResult(secondInvocationId, 13));

        _first.Manager.ReceiveClientResult(
            new ClientResultMessage<TestHub>(
                _first.Manager.NodeId,
                firstInvocationId,
                "json",
                payload));

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() => firstResultTask);
        Assert.Equal(
            "The SignalR result envelope and protocol frame identify different invocations.",
            exception.Message);
        Assert.False(secondResultTask.IsCompleted);

        await _first.Manager.SetConnectionResultAsync(
            client.HubConnection.ConnectionId,
            CompletionMessage.WithResult(secondInvocationId, 42));
        Assert.Equal(42, await secondResultTask);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "local-cancellation-write-failure-contained")]
    public async Task InvokeConnection_LocalCancellationWriteFailure_PreservesCallerCancellationAsync()
    {
        await using var client = new HubConnectionTestClient(failCancellationWrites: true);
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            cancellationSource.Token);
        _ = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resultTask);
        LogEntry failure = await _environment.ObserveLogAsync(
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message.Contains(
                         "was canceled locally, but its cancellation could not be forwarded",
                         StringComparison.Ordinal),
            TestContext.Current.CancellationToken);
        Assert.IsType<InvalidOperationException>(failure.Exception);
        Assert.Equal(0, _first.Manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "remote-cancellation-publish-failure-contained")]
    public async Task InvokeConnection_RemoteCancellationPublishFailure_PreservesCallerCancellationAsync()
    {
        var logger = new RecordingLogger<ServiceBusHubLifetimeManager<TestHub>>();
        var manager = new ServiceBusHubLifetimeManager<TestHub>(
            new SignalRBackplaneSettings<TestHub>(new RequestTimeout(_environment.Timeout)),
            new CancellationFailureScopeProvider(),
            new TestHubProtocolResolver([new JsonHubProtocol()]),
            logger);
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cancellationSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            manager.InvokeConnectionAsync<int>(
                "remote-connection",
                "GetAnswer",
                [],
                cancellationSource.Token));

        LogEntry failure = await logger.WaitForAsync(
            entry => entry.Level == LogLevel.Warning &&
                     entry.Message.Contains(
                         "was canceled locally, but its cancellation could not be forwarded",
                         StringComparison.Ordinal),
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        Assert.IsType<InvalidOperationException>(failure.Exception);
        Assert.Equal(0, manager.PendingClientInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "invocation-frame-rejected-as-result")]
    public async Task ReceiveClientResult_InvocationFrame_RejectsParameterBindingAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage pending = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(pending.InvocationId);
        byte[] payload = new[] { new JsonHubProtocol() }
            .SerializeInvocation("Unexpected", [], "foreign-invocation")["json"];

        _first.Manager.ReceiveClientResult(
            new ClientResultMessage<TestHub>(
                _first.Manager.NodeId,
                invocationId,
                "json",
                payload));

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() => resultTask);
        Assert.Equal(
            "The SignalR backplane payload does not contain exactly one client completion.",
            exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-CLIENT-RESULT", "stream-frame-rejected-as-result")]
    public async Task ReceiveClientResult_StreamItemFrame_RejectsStreamBindingAsync()
    {
        await using var client = new HubConnectionTestClient();
        await _first.Manager.OnConnectedAsync(client.HubConnection);
        Task<int> resultTask = _first.Manager.InvokeConnectionAsync<int>(
            client.HubConnection.ConnectionId,
            "GetAnswer",
            [],
            TestContext.Current.CancellationToken);
        InvocationMessage pending = await client.ReadInvocationAsync(
            _environment.Timeout,
            TestContext.Current.CancellationToken);
        string invocationId = Assert.IsType<string>(pending.InvocationId);
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        new JsonHubProtocol().WriteMessage(new StreamItemMessage("stream", 42), output);

        _first.Manager.ReceiveClientResult(
            new ClientResultMessage<TestHub>(
                _first.Manager.NodeId,
                invocationId,
                "json",
                output.WrittenSpan.ToArray()));

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() => resultTask);
        Assert.Equal(
            "The SignalR backplane payload does not contain exactly one client completion.",
            exception.Message);
    }

    private sealed class CancellationFailureScopeProvider : IBackplaneScopeProvider
    {
        private readonly IPublishEndpoint _publishEndpoint = new CancellationFailingPublishEndpoint();

        public ValueTask<IBackplaneScope<THub>> CreateScopeAsync<THub>()
            where THub : Hub =>
            ValueTask.FromResult<IBackplaneScope<THub>>(new Scope<THub>(_publishEndpoint));

        private sealed class Scope<THub>(IPublishEndpoint publishEndpoint) : IBackplaneScope<THub>
            where THub : Hub
        {
            public IPublishEndpoint PublishEndpoint { get; } = publishEndpoint;

            public IRequestClient<GroupCommand<THub>> GroupCommandClient =>
                throw new NotSupportedException("Group commands are not used by this test scope.");

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class CancellationFailingPublishEndpoint : IPublishEndpoint
    {
        public Task PublishAsync<TMessage>(
            TMessage message,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            return message is InvocationCancellationMessage<TestHub>
                ? Task.FromException(new InvalidOperationException("Intentional cancellation publish failure."))
                : Task.CompletedTask;
        }

        public Task PublishAsync<TMessage>(
            TMessage message,
            PublishOptions options,
            CancellationToken cancellationToken = default)
            where TMessage : class =>
            PublishAsync(message, cancellationToken);

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException("Publish observers are not used by this test endpoint.");
    }
}
