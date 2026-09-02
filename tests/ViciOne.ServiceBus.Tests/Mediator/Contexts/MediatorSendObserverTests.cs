using System.Collections.Concurrent;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using ServiceBusMediator = ViciOne.ServiceBus.Mediator.IMediator;

namespace ViciOne.ServiceBus.Tests.Mediator.Contexts;

public sealed class MediatorSendObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "one-way-and-request-response")]
    public async Task Observer_ReportsOneSendForOneWayAndBothSendsForRequestResponse()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var oneWayConsumed = new TaskCompletionSource<ConsumeContext<OneWayMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ServiceBusMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
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

        await mediator.Send(oneWay, cancellationToken);
        ConsumeContext<OneWayMessage> oneWayContext = await oneWayConsumed.Task.WaitAsync(timeout, cancellationToken);

        Assert.Equal(oneWay, oneWayContext.Message);
        Assert.Equal(["Pre", "Post"], observer.Events.Select(observation => observation.Stage));
        Assert.All(observer.Events, observation =>
        {
            Assert.Equal(typeof(OneWayMessage), observation.MessageType);
            Assert.Same(oneWay, observation.Message);
            Assert.Null(observation.Exception);
        });

        IRequestClient<MediatorRequest> client = mediator.CreateRequestClient<MediatorRequest>(timeout);
        var request = new MediatorRequest(NewId.NextGuid(), "request");
        Response<MediatorResponse> response = await client.GetResponse<MediatorResponse>(request, cancellationToken);

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
    public async Task HandlerFailure_ReportsTheExactSendFaultWithoutPostSend()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("mediator handler failed");
        ServiceBusMediator mediator = Bus.Factory.CreateMediator(configurator =>
            configurator.Handler<FaultingMediatorMessage>(_ => Task.FromException(expected)));
        var observer = new RecordingSendObserver();
        using ConnectHandle observerHandle = mediator.ConnectSendObserver(observer);
        var message = new FaultingMediatorMessage(NewId.NextGuid());

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.Send(message, cancellationToken));

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

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class RecordingSendObserver : ISendObserver
    {
        private readonly ConcurrentQueue<SendObservation> _events = new();

        public SendObservation[] Events => _events.ToArray();

        public Task PreSend<T>(SendContext<T> context)
            where T : class
        {
            _events.Enqueue(new SendObservation("Pre", typeof(T), context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task PostSend<T>(SendContext<T> context)
            where T : class
        {
            _events.Enqueue(new SendObservation("Post", typeof(T), context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new SendObservation("Fault", typeof(T), context.Message, context, exception));
            return Task.CompletedTask;
        }
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
}
