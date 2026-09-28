using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;
using PersistentOutboxSendEndpoint = ViciOne.ServiceBus.Middleware.Outbox.OutboxSendEndpoint;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class PersistentOutboxCancellationHandoffTests
{
    [Theory]
    [InlineData(SendShape.Typed)]
    [InlineData(SendShape.TypedPipe)]
    [InlineData(SendShape.UntypedPipe)]
    [InlineData(SendShape.RuntimeContract)]
    [InlineData(SendShape.Initialized)]
    [InlineData(SendShape.InitializedTypedPipe)]
    [InlineData(SendShape.InitializedUntypedPipe)]
    [RequirementCoverage("REQ-VSB-OUTBOX-SEND-ENDPOINT", "t98-persistent-admission-preserves-caller-token-and-send-context")]
    public async Task Send_PreservesTheCallerTokenAndConfiguredContextAtPersistentAdmissionAsync(SendShape shape)
    {
        using var cancellation = new CancellationTokenSource();
        var message = new OutgoingMessage { Id = Guid.NewGuid() };
        ITransportSendEndpoint transport = DispatchProxy.Create<ITransportSendEndpoint, TransportProxy>();
        var transportProxy = (TransportProxy)(object)transport;
        var outbox = new RecordingOutbox();
        var endpoint = new PersistentOutboxSendEndpoint(outbox, transport);

        await SendAsync(endpoint, shape, message, cancellation.Token);

        Assert.Equal(1, transportProxy.ContextCreations);
        Assert.Equal(cancellation.Token, transportProxy.CreationToken);
        Assert.Equal(1, outbox.Attempts);
        Assert.Equal(1, outbox.Accepted);
        Assert.Equal(cancellation.Token, outbox.AdmissionToken);
        Assert.Same(transportProxy.CreatedContext, outbox.AdmissionContext);
        OutgoingMessage admitted = Assert.IsAssignableFrom<SendContext<OutgoingMessage>>(outbox.AdmissionContext).Message;
        if (shape is SendShape.Initialized or SendShape.InitializedTypedPipe or SendShape.InitializedUntypedPipe)
            Assert.Equal(message.Id, admitted.Id);
        else
            Assert.Same(message, admitted);
        if (shape is SendShape.TypedPipe or SendShape.UntypedPipe or SendShape.InitializedTypedPipe or SendShape.InitializedUntypedPipe)
            Assert.Equal("configured", outbox.AdmissionContext!.Headers.Get<string>("outbox-pipe"));
    }

    [Theory]
    [InlineData(SendShape.Typed)]
    [InlineData(SendShape.TypedPipe)]
    [InlineData(SendShape.UntypedPipe)]
    [InlineData(SendShape.RuntimeContract)]
    [InlineData(SendShape.Initialized)]
    [InlineData(SendShape.InitializedTypedPipe)]
    [InlineData(SendShape.InitializedUntypedPipe)]
    [RequirementCoverage("REQ-VSB-OUTBOX-SEND-ENDPOINT", "t98-persistent-admission-rejects-cancellation-after-context-creation")]
    public async Task Send_CanceledAfterContextCreationNeverAdmitsTheMessageAsync(SendShape shape)
    {
        using var cancellation = new CancellationTokenSource();
        var message = new OutgoingMessage { Id = Guid.NewGuid() };
        ITransportSendEndpoint transport = DispatchProxy.Create<ITransportSendEndpoint, TransportProxy>();
        var transportProxy = (TransportProxy)(object)transport;
        transportProxy.AfterContextCreation = cancellation.Cancel;
        var outbox = new RecordingOutbox();
        var endpoint = new PersistentOutboxSendEndpoint(outbox, transport);

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SendAsync(endpoint, shape, message, cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(1, transportProxy.ContextCreations);
        Assert.Equal(1, outbox.Attempts);
        Assert.Equal(cancellation.Token, outbox.AdmissionToken);
        Assert.Same(transportProxy.CreatedContext, outbox.AdmissionContext);
        Assert.Equal(0, outbox.Accepted);
    }

    [Theory]
    [InlineData(SendShape.Typed)]
    [InlineData(SendShape.TypedPipe)]
    [InlineData(SendShape.UntypedPipe)]
    [InlineData(SendShape.RuntimeContract)]
    [InlineData(SendShape.Initialized)]
    [InlineData(SendShape.InitializedTypedPipe)]
    [InlineData(SendShape.InitializedUntypedPipe)]
    [RequirementCoverage("REQ-VSB-OUTBOX-SEND-ENDPOINT", "t98-persistent-admission-awaits-delayed-storage-failure")]
    public async Task Send_RemainsPendingAndPropagatesTheExactDelayedAdmissionFailureAsync(SendShape shape)
    {
        using var cancellation = new CancellationTokenSource();
        var message = new OutgoingMessage { Id = Guid.NewGuid() };
        ITransportSendEndpoint transport = DispatchProxy.Create<ITransportSendEndpoint, TransportProxy>();
        var admission = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outbox = new RecordingOutbox { AdmissionTask = admission.Task };
        var endpoint = new PersistentOutboxSendEndpoint(outbox, transport);

        Task send = SendAsync(endpoint, shape, message, cancellation.Token);

        Assert.False(send.IsCompleted);
        Assert.Equal(1, outbox.Attempts);
        Assert.Equal(cancellation.Token, outbox.AdmissionToken);
        var storageFailure = new InvalidOperationException("Outbox storage rejected the send.");
        admission.SetException(storageFailure);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => send);
        Assert.Same(storageFailure, actual);
        Assert.Equal(0, outbox.Accepted);
    }

    private static Task SendAsync(PersistentOutboxSendEndpoint endpoint, SendShape shape,
        OutgoingMessage message, CancellationToken cancellationToken) => shape switch
    {
        SendShape.Typed => endpoint.SendAsync(message, cancellationToken),
        SendShape.TypedPipe => endpoint.SendAsync(message, new HeaderPipe(), cancellationToken),
        SendShape.UntypedPipe => endpoint.SendAsync(message, new UntypedHeaderPipe(), cancellationToken),
        SendShape.RuntimeContract => endpoint.SendAsync((object)message, typeof(OutgoingMessage), cancellationToken),
        SendShape.Initialized => endpoint.SendAsync<OutgoingMessage>(new { message.Id }, cancellationToken),
        SendShape.InitializedTypedPipe => endpoint.SendAsync<OutgoingMessage>(new { message.Id }, new HeaderPipe(), cancellationToken),
        SendShape.InitializedUntypedPipe => endpoint.SendAsync<OutgoingMessage>(new { message.Id }, new UntypedHeaderPipe(), cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };

    public enum SendShape { Typed, TypedPipe, UntypedPipe, RuntimeContract, Initialized, InitializedTypedPipe, InitializedUntypedPipe }
    public sealed class OutgoingMessage { public Guid Id { get; set; } }

    private sealed class HeaderPipe : IPipe<SendContext<OutgoingMessage>>
    {
        public Task SendAsync(SendContext<OutgoingMessage> context)
        {
            context.Headers.Set("outbox-pipe", "configured");
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class UntypedHeaderPipe : IPipe<SendContext>
    {
        public Task SendAsync(SendContext context)
        {
            context.Headers.Set("outbox-pipe", "configured");
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class RecordingOutbox : OutboxSendContext
    {
        public int Attempts { get; private set; }
        public int Accepted { get; private set; }
        public CancellationToken AdmissionToken { get; private set; }
        public SendContext<OutgoingMessage>? AdmissionContext { get; private set; }
        public Task? AdmissionTask { get; init; }

        public Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
            where T : class
        {
            Attempts++;
            AdmissionToken = cancellationToken;
            AdmissionContext = Assert.IsAssignableFrom<SendContext<OutgoingMessage>>(context);
            cancellationToken.ThrowIfCancellationRequested();
            if (AdmissionTask is not null)
                return AdmissionTask;
            Accepted++;
            return Task.CompletedTask;
        }

        public object? GetService(Type serviceType) => null;
    }

    public class TransportProxy : DispatchProxy
    {
        public Action? AfterContextCreation { get; set; }
        public int ContextCreations { get; private set; }
        public CancellationToken CreationToken { get; private set; }
        public SendContext<OutgoingMessage>? CreatedContext { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(ITransportSendEndpoint.CreateSendContextAsync)
                || targetMethod.GetGenericArguments().SingleOrDefault() != typeof(OutgoingMessage)
                || args is not { Length: 3 })
                throw new InvalidOperationException($"Unexpected transport operation: {targetMethod?.Name}.");

            return CreateAsync((OutgoingMessage)args[0]!,
                (IPipe<SendContext<OutgoingMessage>>)args[1]!, (CancellationToken)args[2]!);
        }

        private async Task<SendContext<OutgoingMessage>> CreateAsync(OutgoingMessage message,
            IPipe<SendContext<OutgoingMessage>> pipe, CancellationToken cancellationToken)
        {
            ContextCreations++;
            CreationToken = cancellationToken;
            var context = new MessageSendContext<OutgoingMessage>(message, cancellationToken);
            await pipe.SendAsync(context);
            CreatedContext = context;
            AfterContextCreation?.Invoke();
            return context;
        }
    }
}
