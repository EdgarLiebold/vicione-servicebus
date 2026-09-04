using System.Collections.Concurrent;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Observers;

public sealed class PublishObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-OBSERVER", "publish-request-isolation-and-disconnect")]
    public async Task PublishObserver_SeesPublishedEventsAndRequestsButNeverReportsThemAsSendsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<PublishedEvent> eventHandler = harness.Handler<PublishedEvent>();
        harness.Handler<PublishedRequest>(context =>
            context.RespondAsync(new PublishedResponse(context.Message.CorrelationId, $"reply:{context.Message.Value}")));

        await harness.StartAsync(cancellationToken);
        try
        {
            var publishObserver = new RecordingPublishObserver();
            var sendObserver = new RecordingSendObserver();
            using ConnectHandle publishHandle = harness.Bus.ConnectPublishObserver(publishObserver);
            using ConnectHandle sendHandle = harness.Bus.ConnectSendObserver(sendObserver);
            var published = new PublishedEvent(NewId.NextGuid(), "event");

            await harness.Bus.PublishAsync(published, cancellationToken);
            IReceivedMessage<PublishedEvent> consumed = await eventHandler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(published, consumed.Context.Message);
            Assert.Equal(["Pre", "Post"], publishObserver.Events.Select(observation => observation.Stage));
            Assert.All(publishObserver.Events, observation =>
            {
                Assert.Same(published, observation.Message);
                Assert.Equal(typeof(PublishedEvent), observation.MessageType);
                Assert.Null(observation.Exception);
            });
            Assert.Empty(sendObserver.Events);

            IRequestClient<PublishedRequest> client = harness.Bus.CreateRequestClient<PublishedRequest>(timeout);
            var request = new PublishedRequest(NewId.NextGuid(), "request");
            Response<PublishedResponse> response = await client.GetResponseAsync<PublishedResponse>(request, cancellationToken);

            Assert.Equal(new PublishedResponse(request.CorrelationId, "reply:request"), response.Message);
            Assert.Equal(["Pre", "Post", "Pre", "Post"], publishObserver.Events.Select(observation => observation.Stage));
            Assert.Equal([typeof(PublishedEvent), typeof(PublishedEvent), typeof(PublishedRequest), typeof(PublishedRequest)],
                publishObserver.Events.Select(observation => observation.MessageType));
            Assert.DoesNotContain(sendObserver.Events, observation => observation.MessageType == typeof(PublishedEvent));
            Assert.DoesNotContain(sendObserver.Events, observation => observation.MessageType == typeof(PublishedRequest));
            Assert.Contains(sendObserver.Events, observation => observation.MessageType == typeof(PublishedResponse));

            publishHandle.Dispose();
            publishHandle.Dispose();
            await harness.Bus.PublishAsync(new PublishedEvent(NewId.NextGuid(), "after-disconnect"), cancellationToken);

            Assert.Equal(4, publishObserver.Events.Length);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-OBSERVER", "fault-without-post")]
    public async Task PublishObserver_SeesTheExactPublishFailureWithoutPostPublishOrSendObservationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        harness.Handler<PublishedEvent>();

        await harness.StartAsync(cancellationToken);
        try
        {
            var publishObserver = new RecordingPublishObserver();
            var sendObserver = new RecordingSendObserver();
            using ConnectHandle publishHandle = harness.Bus.ConnectPublishObserver(publishObserver);
            using ConnectHandle sendHandle = harness.Bus.ConnectSendObserver(sendObserver);
            var message = new PublishedEvent(NewId.NextGuid(), "cannot-serialize");

            SerializationException failure = await Assert.ThrowsAsync<SerializationException>(() =>
                harness.Bus.PublishAsync(
                    message,
                    context => context.Serializer = null!,
                    cancellationToken));

            PublishObservation[] events = publishObserver.Events;
            Assert.Equal(["Pre", "Fault"], events.Select(observation => observation.Stage));
            Assert.All(events, observation =>
            {
                Assert.Same(message, observation.Message);
                Assert.Equal(typeof(PublishedEvent), observation.MessageType);
            });
            Assert.Null(events[0].Exception);
            Assert.Same(failure, events[1].Exception);
            Assert.Same(events[0].Context, events[1].Context);
            Assert.Empty(sendObserver.Events);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"publish-observer-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed class RecordingPublishObserver : IPublishObserver
    {
        private readonly ConcurrentQueue<PublishObservation> _events = new();

        public PublishObservation[] Events => _events.ToArray();

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Pre", typeof(T), context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Post", typeof(T), context.Message, context, null));
            return Task.CompletedTask;
        }

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Fault", typeof(T), context.Message, context, exception));
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
            _events.Enqueue(new SendObservation("Pre", typeof(T), context.Message));
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            _events.Enqueue(new SendObservation("Post", typeof(T), context.Message));
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new SendObservation("Fault", typeof(T), context.Message));
            return Task.CompletedTask;
        }
    }

    private sealed record PublishObservation(
        string Stage,
        Type MessageType,
        object Message,
        PublishContext Context,
        Exception? Exception);

    private sealed record SendObservation(string Stage, Type MessageType, object Message);

    private sealed record PublishedEvent(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record PublishedRequest(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record PublishedResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
}
