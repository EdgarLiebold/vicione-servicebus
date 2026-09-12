using System.Collections.Concurrent;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using ServiceBusMediator = ViciOne.ServiceBus.Mediator.IMediator;

namespace ViciOne.ServiceBus.Tests.Mediator.Contexts;

public sealed class MediatorSendObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "one-way-and-request-response")]
    public async Task Observer_ReportsOneSendForOneWayAndBothSendsForRequestResponseAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var oneWayConsumed = new TaskCompletionSource<ConsumeContext<OneWayMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ServiceBusMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<OneWayMessage>(context =>
            {
                oneWayConsumed.TrySetResult(context);
                return Task.CompletedTask;
            });
            configurator.Handler<MediatorRequest>(context =>
                context.RespondAsync(new MediatorResponse(context.Message.CorrelationId, $"reply:{context.Message.Value}")));
        });
        var observer = new RecordingSendObserver();
        using ConnectHandle observerHandle = mediator.ConnectSendObserver(observer);
        var oneWay = new OneWayMessage(NewId.NextGuid());

        await mediator.SendAsync(oneWay, cancellationToken);
        ConsumeContext<OneWayMessage> oneWayContext = await oneWayConsumed.Task.WaitAsync(timeout, cancellationToken);

        Assert.Equal(oneWay, oneWayContext.Message);
        Assert.Equal(["Pre", "Post"], observer.Events.Select(observation => observation.Stage));
        Assert.All(observer.Events, observation =>
        {
            Assert.Equal(typeof(OneWayMessage), observation.MessageType);
            Assert.Same(oneWay, observation.Message);
            Assert.Null(observation.Exception);
        });

        IRequestClient<MediatorRequest> client = mediator.CreateRequestClient<MediatorRequest>(new RequestTimeout(timeout));
        var request = new MediatorRequest(NewId.NextGuid(), "request");
        Response<MediatorResponse> response = await client.GetResponseAsync<MediatorResponse>(request, cancellationToken);

        Assert.Equal(new MediatorResponse(request.CorrelationId, "reply:request"), response.Message);
        Assert.Equal(["Pre", "Post", "Pre", "Pre", "Post", "Post"],
            observer.Events.Select(observation => observation.Stage));
        Assert.Equal(
            [
                typeof(OneWayMessage),
                typeof(OneWayMessage),
                typeof(MediatorRequest),
                typeof(MediatorResponse),
                typeof(MediatorResponse),
                typeof(MediatorRequest),
            ],
            observer.Events.Select(observation => observation.MessageType));
        Assert.All(observer.Events, observation => Assert.Null(observation.Exception));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "dispatch-fault-without-post")]
    public async Task HandlerFailure_ReportsTheExactSendFaultWithoutPostSendAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("mediator handler failed");
        ServiceBusMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<FaultingMediatorMessage>(_ => Task.FromException(expected));
        });
        var observer = new RecordingSendObserver();
        using ConnectHandle observerHandle = mediator.ConnectSendObserver(observer);
        var message = new FaultingMediatorMessage(NewId.NextGuid());

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(message, cancellationToken));

        Assert.Same(expected, failure);
        Assert.Equal(["Pre", "Fault"], observer.Events.Select(observation => observation.Stage));
        Assert.All(observer.Events, observation =>
        {
            Assert.Equal(typeof(FaultingMediatorMessage), observation.MessageType);
            Assert.Same(message, observation.Message);
        });
        Assert.Null(observer.Events[0].Exception);
        Assert.Same(expected, observer.Events[1].Exception);
        Assert.Same(observer.Events[0].Context, observer.Events[1].Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "serialization-fault-without-dispatch")]
    public async Task SerializationFailure_ReportsPreAndExactFaultWithoutDispatchOrPostAsync()
    {
        var handled = 0;
        var expected = new InvalidOperationException("property getter failed");
        ServiceBusMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<FaultingSerializationMessage>(_ =>
            {
                Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        var observer = new RecordingSendObserver();
        using ConnectHandle observerHandle = mediator.ConnectSendObserver(observer);
        var message = new FaultingSerializationMessage(expected);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(message, TestContext.Current.CancellationToken));

        Assert.Same(expected, failure);
        Assert.Equal(0, Volatile.Read(ref handled));
        Assert.Equal(["Pre", "Fault"], observer.Events.Select(observation => observation.Stage));
        Assert.Same(expected, observer.Events[1].Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "body-limit-fault-after-pre-send")]
    public async Task BodyLimitFailure_ReportsPreAndFaultWithoutDispatchOrPostAsync()
    {
        var handled = 0;
        ServiceBusMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(new MessageLimits { MaxBodyBytes = 32, MaxEnvelopeBytes = 32, MaxJsonDepth = 16 });
            configurator.Handler<MutableObserverMessage>(_ =>
            {
                Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        var observer = new RecordingSendObserver();
        using ConnectHandle observerHandle = mediator.ConnectSendObserver(observer);

        MessageTooLargeException failure = await Assert.ThrowsAsync<MessageTooLargeException>(() =>
            mediator.SendAsync(
                new MutableObserverMessage { Value = new string('x', 128) },
                TestContext.Current.CancellationToken));

        Assert.Equal(0, Volatile.Read(ref handled));
        Assert.Equal(["Pre", "Fault"], observer.Events.Select(observation => observation.Stage));
        Assert.Same(failure, observer.Events[1].Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "serialization-cancellation-after-pre-send")]
    public async Task SerializationCancellation_ReportsPreAndFaultWithoutDispatchOrPostAsync()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var handled = 0;
        ServiceBusMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<MutableObserverMessage>(_ =>
            {
                Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        var observer = new RecordingSendObserver();
        using ConnectHandle recordingHandle = mediator.ConnectSendObserver(observer);
        using ConnectHandle cancellationHandle = mediator.ConnectSendObserver(new PreSendActionObserver(_ => source.Cancel()));

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mediator.SendAsync(new MutableObserverMessage { Value = "cancel" }, source.Token));

        Assert.Equal(source.Token, failure.CancellationToken);
        Assert.Equal(0, Volatile.Read(ref handled));
        Assert.Equal(["Pre", "Fault"], observer.Events.Select(observation => observation.Stage));
        Assert.Same(failure, observer.Events[1].Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "pre-send-mutation-body-and-json-content-type-consistency")]
    public async Task PreSendMutation_IsReflectedInTheHandlerMessageAndReadableBodyAsync()
    {
        string? handledValue = null;
        byte[]? handledBody = null;
        string? handledContentType = null;
        ServiceBusMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<MutableObserverMessage>(context =>
            {
                handledValue = context.Message.Value;
                handledBody = context.Advanced().ReceiveContext.Body.ToArray();
                handledContentType = context.Advanced().ReceiveContext.ContentType.MediaType;
                return Task.CompletedTask;
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        using ConnectHandle observerHandle = mediator.ConnectSendObserver(new PreSendActionObserver(message =>
        {
            Assert.IsType<MutableObserverMessage>(message).Value = "after-pre-send";
        }));
        var message = new MutableObserverMessage { Value = "before-pre-send" };

        await mediator.SendAsync(message, TestContext.Current.CancellationToken);

        Assert.Equal("after-pre-send", handledValue);
        Assert.Equal("application/json", handledContentType);
        using JsonDocument document = JsonDocument.Parse(Assert.IsType<byte[]>(handledBody));
        Assert.Equal("after-pre-send", document.RootElement.GetProperty("value").GetString());
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class RecordingSendObserver : ISendObserver
    {
        private readonly ConcurrentQueue<SendObservation> _events = new();

        public SendObservation[] Events => _events.ToArray();

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            _events.Enqueue(new SendObservation("Pre", typeof(T), context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            _events.Enqueue(new SendObservation("Post", typeof(T), context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new SendObservation("Fault", typeof(T), context.Message, context, exception));
            return Task.CompletedTask;
        }
    }

    private sealed class PreSendActionObserver(Action<object> action) : ISendObserver
    {
        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            action(context.Message);
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    private sealed record SendObservation(
        string Stage,
        Type MessageType,
        object Message,
        SendContext Context,
        Exception? Exception);

    private sealed record OneWayMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record MediatorRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record MediatorResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record FaultingMediatorMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class FaultingSerializationMessage(Exception exception)
    {
        public string Value => throw exception;
    }

    private sealed class MutableObserverMessage
    {
        public string Value { get; set; } = string.Empty;
    }
}
