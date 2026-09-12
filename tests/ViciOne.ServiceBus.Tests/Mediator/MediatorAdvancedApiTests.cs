using System.Collections.Concurrent;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorAdvancedApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ADVANCED-SEND", "all-message-pipe-and-initializer-forms")]
    public async Task AdvancedSendApi_DispatchesEveryFormWithItsExactContractAndPipelineAsync()
    {
        var consumed = new ConcurrentQueue<ConsumedSnapshot>();
        await using IMediator mediator = CreateMediator(consumed);
        IAdvancedSendEndpoint endpoint = ((ISendEndpoint)mediator).Advanced();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var pipeInvocations = 0;
        var optionsMessage = NewMessage("options");
        var typedMessage = NewMessage("typed");
        var typedPipeMessage = NewMessage("typed-pipe");
        var untypedPipeMessage = NewMessage("untyped-pipe");
        var runtimeMessage = NewMessage("runtime");
        var declaredMessage = NewMessage("declared");
        var runtimePipeMessage = NewMessage("runtime-pipe");
        var declaredPipeMessage = NewMessage("declared-pipe");
        Guid optionsCorrelationId = NewId.NextGuid();

        await mediator.SendAsync(optionsMessage, new SendOptions
        {
            CorrelationId = optionsCorrelationId,
            Headers = new Dictionary<string, object?> { ["source"] = "options" },
        }, cancellationToken);
        await ((ISendEndpoint)endpoint).SendAsync(typedMessage, cancellationToken);
        await endpoint.SendAsync(typedPipeMessage,
            new RecordingPipe<SendContext<AdvancedMessage>>(context =>
            {
                Assert.Same(typedPipeMessage, context.Message);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.SendAsync(untypedPipeMessage,
            new RecordingPipe<SendContext>(context =>
            {
                Assert.Equal(new Uri("loopback://localhost/mediator"), context.DestinationAddress);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.SendAsync((object)runtimeMessage, cancellationToken);
        await endpoint.SendAsync(declaredMessage, typeof(AdvancedMessage), cancellationToken);
        await endpoint.SendAsync(runtimePipeMessage,
            new RecordingPipe<SendContext>(context =>
            {
                Assert.Equal(new Uri("loopback://localhost/mediator"), context.DestinationAddress);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.SendAsync(declaredPipeMessage, typeof(AdvancedMessage),
            new RecordingPipe<SendContext>(context =>
            {
                Assert.Equal(new Uri("loopback://localhost/mediator"), context.DestinationAddress);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.SendAsync<AdvancedMessage>(new { Value = "initialized" }, cancellationToken);
        await endpoint.SendAsync<AdvancedMessage>(new { Value = "initialized-typed-pipe" },
            new RecordingPipe<SendContext<AdvancedMessage>>(context =>
            {
                Assert.Equal("initialized-typed-pipe", context.Message.Value);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.SendAsync<AdvancedMessage>(new { Value = "initialized-untyped-pipe" },
            new RecordingPipe<SendContext>(context =>
            {
                Assert.Equal(new Uri("loopback://localhost/mediator"), context.DestinationAddress);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);

        ConsumedSnapshot[] snapshots = consumed.ToArray();
        Assert.Equal(11, snapshots.Length);
        Assert.Equal(
            [
                "options", "typed", "typed-pipe", "untyped-pipe", "runtime", "declared", "runtime-pipe",
                "declared-pipe", "initialized", "initialized-typed-pipe", "initialized-untyped-pipe",
            ],
            snapshots.Select(x => x.Message.Value));
        Assert.Equal(6, Volatile.Read(ref pipeInvocations));
        Assert.Equal(optionsCorrelationId, snapshots[0].CorrelationId);
        Assert.Equal("options", snapshots[0].Headers.Get<string>("source"));
        Assert.Same(optionsMessage, snapshots[0].Message);
        Assert.Same(typedMessage, snapshots[1].Message);
        Assert.Same(declaredPipeMessage, snapshots[7].Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ADVANCED-PUBLISH", "all-message-pipe-and-initializer-forms")]
    public async Task AdvancedPublishApi_DispatchesEveryFormWithItsExactContractAndPipelineAsync()
    {
        var consumed = new ConcurrentQueue<ConsumedSnapshot>();
        await using IMediator mediator = CreateMediator(consumed);
        IAdvancedPublishEndpoint endpoint = ((IPublishEndpoint)mediator).Advanced();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var pipeInvocations = 0;
        var optionsMessage = NewMessage("options");
        var typedMessage = NewMessage("typed");
        var typedPipeMessage = NewMessage("typed-pipe");
        var untypedPipeMessage = NewMessage("untyped-pipe");
        var runtimeMessage = NewMessage("runtime");
        var runtimePipeMessage = NewMessage("runtime-pipe");
        var declaredMessage = NewMessage("declared");
        var declaredPipeMessage = NewMessage("declared-pipe");
        Guid optionsCorrelationId = NewId.NextGuid();

        await mediator.PublishAsync(optionsMessage, new PublishOptions
        {
            CorrelationId = optionsCorrelationId,
            Headers = new Dictionary<string, object?> { ["source"] = "options" },
        }, cancellationToken);
        await ((IPublishEndpoint)endpoint).PublishAsync(typedMessage, cancellationToken);
        await endpoint.PublishAsync(typedPipeMessage,
            new RecordingPipe<PublishContext<AdvancedMessage>>(context =>
            {
                Assert.Same(typedPipeMessage, context.Message);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.PublishAsync(untypedPipeMessage,
            new RecordingPipe<PublishContext>(context =>
            {
                Assert.Equal(new Uri("loopback://localhost/mediator"), context.DestinationAddress);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.PublishAsync((object)runtimeMessage, cancellationToken);
        await endpoint.PublishAsync(runtimePipeMessage,
            new RecordingPipe<PublishContext>(context =>
            {
                Assert.Equal(new Uri("loopback://localhost/mediator"), context.DestinationAddress);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.PublishAsync(declaredMessage, typeof(AdvancedMessage), cancellationToken);
        await endpoint.PublishAsync(declaredPipeMessage, typeof(AdvancedMessage),
            new RecordingPipe<PublishContext>(context =>
            {
                Assert.Equal(new Uri("loopback://localhost/mediator"), context.DestinationAddress);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.PublishAsync<AdvancedMessage>(new { Value = "initialized" }, cancellationToken);
        await endpoint.PublishAsync<AdvancedMessage>(new { Value = "initialized-typed-pipe" },
            new RecordingPipe<PublishContext<AdvancedMessage>>(context =>
            {
                Assert.Equal("initialized-typed-pipe", context.Message.Value);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);
        await endpoint.PublishAsync<AdvancedMessage>(new { Value = "initialized-untyped-pipe" },
            new RecordingPipe<PublishContext>(context =>
            {
                Assert.Equal(new Uri("loopback://localhost/mediator"), context.DestinationAddress);
                Interlocked.Increment(ref pipeInvocations);
            }), cancellationToken);

        ConsumedSnapshot[] snapshots = consumed.ToArray();
        Assert.Equal(11, snapshots.Length);
        Assert.Equal(
            [
                "options", "typed", "typed-pipe", "untyped-pipe", "runtime", "runtime-pipe", "declared",
                "declared-pipe", "initialized", "initialized-typed-pipe", "initialized-untyped-pipe",
            ],
            snapshots.Select(x => x.Message.Value));
        Assert.Equal(6, Volatile.Read(ref pipeInvocations));
        Assert.All(snapshots, snapshot => Assert.True(snapshot.IsPublish));
        Assert.Equal(optionsCorrelationId, snapshots[0].CorrelationId);
        Assert.Equal("options", snapshots[0].Headers.Get<string>("source"));
        Assert.Same(optionsMessage, snapshots[0].Message);
        Assert.Same(declaredPipeMessage, snapshots[7].Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ADVANCED-BOUNDARIES", "null-arguments")]
    public async Task AdvancedApis_RejectEveryNullRequiredArgumentWithItsExactParameterNameAsync()
    {
        await using IMediator mediator = CreateMediator(new ConcurrentQueue<ConsumedSnapshot>());
        IAdvancedSendEndpoint send = ((ISendEndpoint)mediator).Advanced();
        IAdvancedPublishEndpoint publish = ((IPublishEndpoint)mediator).Advanced();
        CancellationToken token = TestContext.Current.CancellationToken;
        var message = NewMessage("valid");
        IPipe<SendContext> sendPipe = Pipe.Empty<SendContext>();
        IPipe<PublishContext> publishPipe = Pipe.Empty<PublishContext>();

        await AssertParameterAsync("message", () => ((ISendEndpoint)send).SendAsync<AdvancedMessage>(null!, token));
        await AssertParameterAsync("options", () => mediator.SendAsync(message, null!, token));
        await AssertParameterAsync("pipe", () => send.SendAsync(message, (IPipe<SendContext<AdvancedMessage>>)null!, token));
        await AssertParameterAsync("pipe", () => send.SendAsync(message, (IPipe<SendContext>)null!, token));
        await AssertParameterAsync("message", () => send.SendAsync(null!, token));
        await AssertParameterAsync("message", () => send.SendAsync(null!, typeof(AdvancedMessage), token));
        await AssertParameterAsync("messageType", () => send.SendAsync(message, (Type)null!, token));
        await AssertParameterAsync("pipe", () => send.SendAsync(message, (IPipe<SendContext>)null!, token));
        await AssertParameterAsync("messageType", () => send.SendAsync(message, null!, sendPipe, token));
        await AssertParameterAsync("pipe", () => send.SendAsync(message, typeof(AdvancedMessage), null!, token));
        await AssertParameterAsync("values", () => send.SendAsync<AdvancedMessage>(null!, token));
        await AssertParameterAsync("pipe", () => send.SendAsync<AdvancedMessage>(new { Value = "valid" },
            (IPipe<SendContext<AdvancedMessage>>)null!, token));
        await AssertParameterAsync("pipe", () => send.SendAsync<AdvancedMessage>(new { Value = "valid" },
            (IPipe<SendContext>)null!, token));

        await AssertParameterAsync("message", () => ((IPublishEndpoint)publish).PublishAsync<AdvancedMessage>(null!, token));
        await AssertParameterAsync("options", () => mediator.PublishAsync(message, null!, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync(message,
            (IPipe<PublishContext<AdvancedMessage>>)null!, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync(message,
            (IPipe<PublishContext>)null!, token));
        await AssertParameterAsync("message", () => publish.PublishAsync(null!, token));
        await AssertParameterAsync("message", () => publish.PublishAsync(null!, typeof(AdvancedMessage), token));
        await AssertParameterAsync("messageType", () => publish.PublishAsync(message, (Type)null!, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync(message, (IPipe<PublishContext>)null!, token));
        await AssertParameterAsync("messageType", () => publish.PublishAsync(message, null!, publishPipe, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync(message, typeof(AdvancedMessage), null!, token));
        await AssertParameterAsync("values", () => publish.PublishAsync<AdvancedMessage>(null!, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync<AdvancedMessage>(new { Value = "valid" },
            (IPipe<PublishContext<AdvancedMessage>>)null!, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync<AdvancedMessage>(new { Value = "valid" },
            (IPipe<PublishContext>)null!, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ENDPOINT-RESOLUTION", "cancellation-and-observer-boundaries")]
    public async Task EndpointAndObserverConnectors_HonorCancellationAndRejectNullObserversAsync()
    {
        await using IMediator mediator = CreateMediator(new ConcurrentQueue<ConsumedSnapshot>());
        using var source = new CancellationTokenSource();
        source.Cancel();

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mediator.GetPublishSendEndpointAsync<AdvancedMessage>(source.Token));
        ArgumentNullException sendObserver = Assert.Throws<ArgumentNullException>(() =>
            mediator.ConnectSendObserver(null!));
        ArgumentNullException publishObserver = Assert.Throws<ArgumentNullException>(() =>
            mediator.ConnectPublishObserver(null!));

        Assert.Equal(source.Token, canceled.CancellationToken);
        Assert.Equal("observer", sendObserver.ParamName);
        Assert.Equal("observer", publishObserver.ParamName);
    }

    private static IMediator CreateMediator(ConcurrentQueue<ConsumedSnapshot> consumed) =>
        MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<AdvancedMessage>(context =>
            {
                consumed.Enqueue(new ConsumedSnapshot(
                    context.Message,
                    context.CorrelationId,
                    context.Headers,
                    context.Advanced().ReceiveContext.IsDelivered));
                return Task.CompletedTask;
            });
        });

    private static AdvancedMessage NewMessage(string value) => new() { Value = value };

    private static async Task AssertParameterAsync(string expected, Func<Task> action)
    {
        ArgumentNullException failure = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(expected, failure.ParamName);
    }

    private sealed class RecordingPipe<TContext>(Action<TContext> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context)
        {
            callback(context);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateScope("recordingPipe");
    }

    private sealed class AdvancedMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed record ConsumedSnapshot(
        AdvancedMessage Message,
        Guid? CorrelationId,
        Headers Headers,
        bool IsPublish);
}
