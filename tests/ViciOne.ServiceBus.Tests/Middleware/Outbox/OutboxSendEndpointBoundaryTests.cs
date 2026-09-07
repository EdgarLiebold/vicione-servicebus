using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;
using PersistentOutboxSendEndpoint = ViciOne.ServiceBus.Middleware.Outbox.OutboxSendEndpoint;
using VolatileOutboxSendEndpoint = ViciOne.ServiceBus.Middleware.InMemoryOutbox.OutboxSendEndpoint;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class OutboxSendEndpointBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-OUTBOX-SEND-ENDPOINT", "persistent-endpoint-validates-every-required-input")]
    public async Task PersistentEndpoint_RejectsEveryMissingRequiredInputBeforeTransportUseAsync()
    {
        ITransportSendEndpoint transport = DispatchProxy.Create<ITransportSendEndpoint, TransportEndpointProxy>();
        var outbox = new RecordingPersistentOutbox();
        var endpoint = new PersistentOutboxSendEndpoint(outbox, transport);
        var message = new BoundaryMessage();
        IPipe<SendContext<BoundaryMessage>> typedPipe = Pipe.Empty<SendContext<BoundaryMessage>>();
        IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("outboxContext", Assert.Throws<ArgumentNullException>(() => new PersistentOutboxSendEndpoint(null!, transport)).ParamName);
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() => new PersistentOutboxSendEndpoint(outbox, null!)).ParamName);
        ISendEndpoint nonTransportEndpoint = DispatchProxy.Create<ISendEndpoint, TransportEndpointProxy>();
        Assert.Equal("endpoint", Assert.Throws<ArgumentException>(() => new PersistentOutboxSendEndpoint(outbox, nonTransportEndpoint)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => endpoint.ConnectSendObserver(null!)).ParamName);

        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.CreateSendContextAsync<BoundaryMessage>(null!, typedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.CreateSendContextAsync(message, null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<BoundaryMessage>(null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<BoundaryMessage>((BoundaryMessage)null!, typedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(message, (IPipe<SendContext<BoundaryMessage>>)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<BoundaryMessage>((BoundaryMessage)null!, untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(message, (IPipe<SendContext>)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync((object)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(null!, typeof(BoundaryMessage), cancellationToken))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(message, (Type)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(null!, untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(message, (IPipe<SendContext>)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(null!, typeof(BoundaryMessage), untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(message, null!, untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync(message, typeof(BoundaryMessage), null!, cancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<BoundaryMessage>((object)null!, cancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<BoundaryMessage>((object)null!, typedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<BoundaryMessage>(new { }, (IPipe<SendContext<BoundaryMessage>>)null!, cancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<BoundaryMessage>((object)null!, untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<BoundaryMessage>(new { }, (IPipe<SendContext>)null!, cancellationToken))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OUTBOX-SEND-ENDPOINT", "persistent-endpoint-delegates-observer-registration")]
    public void PersistentEndpoint_ReturnsTheTransportObserverHandle()
    {
        ITransportSendEndpoint transport = DispatchProxy.Create<ITransportSendEndpoint, TransportEndpointProxy>();
        var transportProxy = (TransportEndpointProxy)(object)transport;
        var endpoint = new PersistentOutboxSendEndpoint(new RecordingPersistentOutbox(), transport);
        ISendObserver observer = DispatchProxy.Create<ISendObserver, TransportEndpointProxy>();

        ConnectHandle handle = endpoint.ConnectSendObserver(observer);

        Assert.Same(transportProxy.ObserverHandle, handle);
        Assert.Same(observer, transportProxy.Observer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OUTBOX-SEND-ENDPOINT", "volatile-endpoint-validates-inputs-and-forwards-cancellation")]
    public async Task VolatileEndpoint_ValidatesEveryRequiredInputAndForwardsCancellationAsync()
    {
        ITransportSendEndpoint transport = DispatchProxy.Create<ITransportSendEndpoint, TransportEndpointProxy>();
        var outbox = new RecordingVolatileOutbox();
        var endpoint = new VolatileOutboxSendEndpoint(outbox, transport);
        ISendEndpoint applicationEndpoint = endpoint;
        IAdvancedSendEndpoint advancedEndpoint = endpoint;
        var message = new BoundaryMessage();
        IPipe<SendContext<BoundaryMessage>> typedPipe = Pipe.Empty<SendContext<BoundaryMessage>>();
        IPipe<SendContext> untypedPipe = Pipe.Empty<SendContext>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("outboxContext", Assert.Throws<ArgumentNullException>(() => new VolatileOutboxSendEndpoint(null!, transport)).ParamName);
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() => new VolatileOutboxSendEndpoint(outbox, null!)).ParamName);
        ISendEndpoint nonTransportEndpoint = DispatchProxy.Create<ISendEndpoint, TransportEndpointProxy>();
        Assert.Equal("endpoint", Assert.Throws<ArgumentException>(() => new VolatileOutboxSendEndpoint(outbox, nonTransportEndpoint)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => endpoint.ConnectSendObserver(null!)).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.CreateSendContextAsync<BoundaryMessage>(null!, typedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.CreateSendContextAsync(message, null!, cancellationToken))).ParamName);

        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => applicationEndpoint.SendAsync<BoundaryMessage>(null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync<BoundaryMessage>(null!, typedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync(message, (IPipe<SendContext<BoundaryMessage>>)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync<BoundaryMessage>(null!, untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync(message, (IPipe<SendContext>)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync((object)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync(null!, typeof(BoundaryMessage), cancellationToken))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            advancedEndpoint.SendAsync(message, (Type)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync(null!, untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync(message, (IPipe<SendContext>)null!, cancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync(null!, typeof(BoundaryMessage), untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            advancedEndpoint.SendAsync(message, (Type)null!, untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync(message, typeof(BoundaryMessage), null!, cancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync<BoundaryMessage>((object)null!, cancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync<BoundaryMessage>((object)null!, typedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync<BoundaryMessage>(new { }, (IPipe<SendContext<BoundaryMessage>>)null!, cancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync<BoundaryMessage>((object)null!, untypedPipe, cancellationToken))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => advancedEndpoint.SendAsync<BoundaryMessage>(new { }, (IPipe<SendContext>)null!, cancellationToken))).ParamName);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            applicationEndpoint.SendAsync(message, cancellation.Token));

        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.Equal(cancellation.Token, outbox.LastCancellationToken);
        Assert.Equal(0, ((TransportEndpointProxy)(object)transport).InvocationCount);
    }

    private sealed record BoundaryMessage;

    private sealed class RecordingPersistentOutbox : OutboxSendContext
    {
        public Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
            where T : class =>
            throw new InvalidOperationException("Input validation must complete before durable outbox storage.");

        public object? GetService(Type serviceType) => null;
    }

    private sealed class RecordingVolatileOutbox : OutboxContext
    {
        public CancellationToken LastCancellationToken { get; private set; }

        public Task ClearToSend => Task.CompletedTask;

        public Task AddAsync(Func<Task> method, CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
        }

        public OutboxCheckpoint CreateCheckpoint() => throw new NotSupportedException();

        public Task ExecutePendingActionsAsync(bool concurrentMessageDelivery, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DiscardPendingActionsAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DiscardPendingActionsAsync(OutboxCheckpoint checkpoint, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private class TransportEndpointProxy : DispatchProxy
    {
        public int InvocationCount { get; private set; }

        public ISendObserver? Observer { get; private set; }

        public ConnectHandle ObserverHandle { get; } = new EmptyConnectHandle();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            InvocationCount++;
            if (targetMethod?.Name == nameof(ISendObserverConnector.ConnectSendObserver))
            {
                Observer = (ISendObserver?)args?[0];
                return ObserverHandle;
            }

            throw new InvalidOperationException("Input validation must complete before transport endpoint use.");
        }
    }
}
