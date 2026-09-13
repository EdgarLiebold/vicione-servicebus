using System.Collections.Concurrent;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorObserverContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-OBSERVERS", "configuration-observers-bind-to-runtime")]
    public async Task ConfigurationObservers_ObserveTheMaterializedMediatorRuntimeAsync()
    {
        var consumeObserver = new RecordingConsumeObserver();
        var sendObserver = new RecordingSendObserver();
        var publishObserver = new RecordingPublishObserver();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.ConnectConsumeObserver(consumeObserver);
            configuration.ConnectSendObserver(sendObserver);
            configuration.ConnectPublishObserver(publishObserver);
            configuration.Handler<ObservedMessage>(_ => Task.CompletedTask);
        });
        var sent = new ObservedMessage(NewId.NextGuid(), "send");
        var published = new ObservedMessage(NewId.NextGuid(), "publish");

        await mediator.SendAsync(sent, TestContext.Current.CancellationToken);
        await mediator.PublishAsync(published, TestContext.Current.CancellationToken);

        Assert.Equal(["Pre", "Post", "Pre", "Post"], consumeObserver.Events.Select(x => x.Stage));
        Assert.Equal([sent, sent, published, published], consumeObserver.Events.Select(x => x.Message));
        Assert.Equal(["Pre", "Post"], sendObserver.Events.Select(x => x.Stage));
        Assert.All(sendObserver.Events, observation => Assert.Same(sent, observation.Message));
        Assert.Equal(["Pre", "Post"], publishObserver.Events.Select(x => x.Stage));
        Assert.All(publishObserver.Events, observation => Assert.Same(published, observation.Message));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-OBSERVERS", "consume-observer-covers-request-and-response")]
    public async Task ConfigurationConsumeObserver_ObservesBothRequestEndpointsAsync()
    {
        var observer = new RecordingConsumeObserver();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.ConnectConsumeObserver(observer);
            configuration.Handler<ObservedRequest>(context =>
                context.RespondAsync(new ObservedResponse(context.Message.CorrelationId)));
        });
        var request = new ObservedRequest(NewId.NextGuid());

        Response<ObservedResponse> response = await mediator.CreateRequestClient<ObservedRequest>(
                new RequestTimeout(OperationTimeout()))
            .GetResponseAsync<ObservedResponse>(request, TestContext.Current.CancellationToken);

        Assert.Equal(request.CorrelationId, response.Message.CorrelationId);
        Assert.Equal(["Pre", "Pre", "Post", "Post"], observer.Events.Select(x => x.Stage));
        Assert.Equal(
            [typeof(ObservedRequest), typeof(ObservedResponse), typeof(ObservedResponse), typeof(ObservedRequest)],
            observer.Events.Select(x => x.MessageType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-PUBLISH-OBSERVER", "publish-send-isolation-and-disconnect")]
    public async Task RuntimePublishObserver_SeesOnlyPublicationsAndDisconnectsIdempotentlyAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<ObservedMessage>(_ => Task.CompletedTask);
        });
        var sendObserver = new RecordingSendObserver();
        var publishObserver = new RecordingPublishObserver();
        using ConnectHandle sendHandle = mediator.ConnectSendObserver(sendObserver);
        using ConnectHandle publishHandle = mediator.ConnectPublishObserver(publishObserver);
        var sent = new ObservedMessage(NewId.NextGuid(), "send");
        var published = new ObservedMessage(NewId.NextGuid(), "publish");

        await mediator.SendAsync(sent, TestContext.Current.CancellationToken);
        await mediator.PublishAsync(published, TestContext.Current.CancellationToken);
        publishHandle.Dispose();
        publishHandle.Dispose();
        await mediator.PublishAsync(new ObservedMessage(NewId.NextGuid(), "after-disconnect"), TestContext.Current.CancellationToken);

        Assert.Equal(["Pre", "Post"], sendObserver.Events.Select(x => x.Stage));
        Assert.All(sendObserver.Events, observation => Assert.Same(sent, observation.Message));
        Assert.Equal(["Pre", "Post"], publishObserver.Events.Select(x => x.Stage));
        Assert.All(publishObserver.Events, observation => Assert.Same(published, observation.Message));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-PUBLISH-OBSERVER", "exact-fault-without-send-or-post")]
    public async Task PublishFailure_ReportsTheExactFaultOnlyToPublishObserversAsync()
    {
        var expected = new InvalidOperationException("published mediator message failed");
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<ObservedMessage>(_ => Task.FromException(expected));
        });
        var sendObserver = new RecordingSendObserver();
        var publishObserver = new RecordingPublishObserver();
        using ConnectHandle sendHandle = mediator.ConnectSendObserver(sendObserver);
        using ConnectHandle publishHandle = mediator.ConnectPublishObserver(publishObserver);
        var published = new ObservedMessage(NewId.NextGuid(), "fault");

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.PublishAsync(published, TestContext.Current.CancellationToken));

        Assert.Same(expected, failure);
        Assert.Empty(sendObserver.Events);
        Assert.Equal(["Pre", "Fault"], publishObserver.Events.Select(x => x.Stage));
        Assert.Same(expected, publishObserver.Events[1].Exception);
        Assert.Same(publishObserver.Events[0].Context, publishObserver.Events[1].Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-PUBLISH-OBSERVER", "observer-fault-does-not-mask-dispatch-fault")]
    public async Task PublishFaultObserverFailure_DoesNotReplaceTheDispatchFailureAsync()
    {
        var dispatchFailure = new InvalidOperationException("dispatch failed");
        var observerFailure = new ApplicationException("observer failed");
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<ObservedMessage>(_ => Task.FromException(dispatchFailure));
        });
        using ConnectHandle handle = mediator.ConnectPublishObserver(new FaultingPublishObserver(observerFailure));

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.PublishAsync(
                new ObservedMessage(NewId.NextGuid(), "fault-observer"),
                TestContext.Current.CancellationToken));

        Assert.Same(dispatchFailure, failure);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private sealed class RecordingConsumeObserver : IConsumeObserver
    {
        private readonly ConcurrentQueue<ConsumeObservation> _events = new();

        public ConsumeObservation[] Events => _events.ToArray();

        public Task PreConsumeAsync<T>(ConsumeContext<T> context)
            where T : class
        {
            _events.Enqueue(new ConsumeObservation("Pre", typeof(T), context.Message, null));
            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context)
            where T : class
        {
            _events.Enqueue(new ConsumeObservation("Post", typeof(T), context.Message, null));
            return Task.CompletedTask;
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new ConsumeObservation("Fault", typeof(T), context.Message, exception));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSendObserver : ISendObserver
    {
        private readonly ConcurrentQueue<SendObservation> _events = new();

        public SendObservation[] Events => _events.ToArray();

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            _events.Enqueue(new SendObservation("Pre", context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            _events.Enqueue(new SendObservation("Post", context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new SendObservation("Fault", context.Message, context, exception));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPublishObserver : IPublishObserver
    {
        private readonly ConcurrentQueue<PublishObservation> _events = new();

        public PublishObservation[] Events => _events.ToArray();

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Pre", context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Post", context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Fault", context.Message, context, exception));
            return Task.CompletedTask;
        }
    }

    private sealed class FaultingPublishObserver(Exception failure) : IPublishObserver
    {
        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class => Task.FromException(failure);
    }

    private sealed record ConsumeObservation(string Stage, Type MessageType, object Message, Exception? Exception);
    private sealed record SendObservation(string Stage, object Message, SendContext Context, Exception? Exception);
    private sealed record PublishObservation(string Stage, object Message, PublishContext Context, Exception? Exception);
    private sealed record ObservedMessage(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;
    private sealed record ObservedRequest(Guid CorrelationId) : ICorrelatedBy<Guid>;
    private sealed record ObservedResponse(Guid CorrelationId) : ICorrelatedBy<Guid>;
}
