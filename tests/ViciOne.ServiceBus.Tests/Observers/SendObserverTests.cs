using System.Collections.Concurrent;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Serialization;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Observers;

public sealed class SendObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-OBSERVER", "bus-endpoint-success-and-disconnect")]
    public async Task BusAndEndpointObservers_SeeTheSameSuccessfulSendAndDisconnectIndependentlyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<ObservedSend> handler = harness.AddHandler<ObservedSend>();

        await harness.StartAsync(cancellationToken);
        try
        {
            var busObserver = new RecordingSendObserver();
            var endpointObserver = new RecordingSendObserver();
            using ConnectHandle busHandle = harness.Bus.ConnectSendObserver(busObserver);
            using ConnectHandle endpointHandle = harness.InputQueueSendEndpoint.ConnectSendObserver(endpointObserver);
            var first = new ObservedSend(NewId.NextGuid(), "first");

            await harness.InputQueueSendEndpoint.SendAsync(first, cancellationToken);
            IConsumedMessage<ObservedSend> firstConsumed = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(first, firstConsumed.Context.Message);
            AssertSuccessfulSend(busObserver.Events, endpointObserver.Events, first, harness.InputQueueAddress);

            endpointHandle.Dispose();
            endpointHandle.Dispose();
            var second = new ObservedSend(NewId.NextGuid(), "second");

            await harness.InputQueueSendEndpoint.SendAsync(second, cancellationToken);
            IConsumedMessage<ObservedSend> secondConsumed = await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.CorrelationId == second.CorrelationId, cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(second, secondConsumed.Context.Message);
            Assert.Equal(["Pre", "Post", "Pre", "Post"], busObserver.Events.Select(observation => observation.Stage));
            Assert.Equal(["Pre", "Post"], endpointObserver.Events.Select(observation => observation.Stage));
            Assert.Equal(second, busObserver.Events[^1].Message);
            Assert.DoesNotContain(endpointObserver.Events, observation => Equals(observation.Message, second));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-OBSERVER", "bus-endpoint-fault-without-post")]
    public async Task BusAndEndpointObservers_SeeTheExactSendFailureWithoutPostSendAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        harness.AddHandler<ObservedSend>();

        await harness.StartAsync(cancellationToken);
        try
        {
            var busObserver = new RecordingSendObserver();
            var endpointObserver = new RecordingSendObserver();
            using ConnectHandle busHandle = harness.Bus.ConnectSendObserver(busObserver);
            using ConnectHandle endpointHandle = harness.InputQueueSendEndpoint.ConnectSendObserver(endpointObserver);
            var message = new ObservedSend(NewId.NextGuid(), "cannot-serialize");

            SerializationException failure = await Assert.ThrowsAsync<SerializationException>(() =>
                harness.InputQueueSendEndpoint.SendAsync(
                    message,
                    context => context.Serializer = new RejectingMessageSerializer("The sent message could not be serialized."),
                    cancellationToken));

            AssertFaultedSend(busObserver.Events, message, harness.InputQueueAddress, failure);
            AssertFaultedSend(endpointObserver.Events, message, harness.InputQueueAddress, failure);
            Assert.Same(busObserver.Events[0].Context, endpointObserver.Events[0].Context);
            Assert.Same(busObserver.Events[1].Context, endpointObserver.Events[1].Context);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-OBSERVER", "response-success-and-fault-boundaries")]
    public async Task BusObserver_DistinguishesTheOriginalSendFromAFaultedResponseSendAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var responseFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        harness.AddHandler<ResponseRequest>(async context =>
        {
            try
            {
                await context.Advanced().RespondAsync(
                    new ResponseMessage(context.Message.CorrelationId),
                    sendContext => sendContext.Serializer = new RejectingMessageSerializer("The response could not be serialized."));
            }
            catch (Exception exception)
            {
                responseFailure.TrySetResult(exception);
            }
        });

        await harness.StartAsync(cancellationToken);
        try
        {
            var observer = new RecordingSendObserver();
            using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(observer);
            var request = new ResponseRequest(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.SendAsync(
                request,
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);

            Exception failure = await responseFailure.Task.WaitAsync(timeout, cancellationToken);
            await observer.Faulted.WaitAsync(timeout, cancellationToken);

            SendObservation[] events = observer.Events;
            Assert.Equal(4, events.Length);

            // The in-memory receive pipeline may begin the response send before the original
            // transport publishes PostSend. Observer ordering is strict within one SendContext,
            // but independent sends are intentionally concurrent and have no global order.
            SendObservation[] requestEvents = events
                .Where(observation => observation.MessageType == typeof(ResponseRequest))
                .ToArray();
            SendObservation[] responseEvents = events
                .Where(observation => observation.MessageType == typeof(ResponseMessage))
                .ToArray();

            Assert.Equal(["Pre", "Post"], requestEvents.Select(observation => observation.Stage));
            Assert.Equal(["Pre", "Fault"], responseEvents.Select(observation => observation.Stage));
            Assert.All(requestEvents, observation => Assert.Equal(request, observation.Message));
            Assert.All(responseEvents, observation =>
                Assert.Equal(new ResponseMessage(request.CorrelationId), observation.Message));
            Assert.Same(requestEvents[0].Context, requestEvents[1].Context);
            Assert.Same(responseEvents[0].Context, responseEvents[1].Context);
            Assert.Null(requestEvents[0].Exception);
            Assert.Null(requestEvents[1].Exception);
            Assert.Null(responseEvents[0].Exception);
            Assert.Same(failure, responseEvents[1].Exception);
            Assert.IsType<SerializationException>(failure);
            Assert.Equal(harness.InputQueueAddress, requestEvents[0].Context.DestinationAddress);
            Assert.Equal(harness.BusAddress, responseEvents[0].Context.DestinationAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static void AssertSuccessfulSend(
        SendObservation[] busEvents,
        SendObservation[] endpointEvents,
        ObservedSend message,
        Uri destinationAddress)
    {
        Assert.Equal(["Pre", "Post"], busEvents.Select(observation => observation.Stage));
        Assert.Equal(["Pre", "Post"], endpointEvents.Select(observation => observation.Stage));
        Assert.All(busEvents, observation =>
        {
            Assert.Equal(typeof(ObservedSend), observation.MessageType);
            Assert.Same(message, observation.Message);
            Assert.Equal(destinationAddress, observation.Context.DestinationAddress);
            Assert.Null(observation.Exception);
        });
        Assert.Same(busEvents[0].Context, busEvents[1].Context);
        Assert.Same(busEvents[0].Context, endpointEvents[0].Context);
        Assert.Same(busEvents[1].Context, endpointEvents[1].Context);
    }

    private static void AssertFaultedSend(
        SendObservation[] events,
        ObservedSend message,
        Uri destinationAddress,
        Exception failure)
    {
        Assert.Equal(["Pre", "Fault"], events.Select(observation => observation.Stage));
        Assert.All(events, observation =>
        {
            Assert.Equal(typeof(ObservedSend), observation.MessageType);
            Assert.Same(message, observation.Message);
            Assert.Equal(destinationAddress, observation.Context.DestinationAddress);
        });
        Assert.Null(events[0].Exception);
        Assert.Same(failure, events[1].Exception);
        Assert.Same(events[0].Context, events[1].Context);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"send-observer-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed class RecordingSendObserver : ISendObserver
    {
        private readonly ConcurrentQueue<SendObservation> _events = new();
        private readonly TaskCompletionSource _faulted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SendObservation[] Events => _events.ToArray();

        public Task Faulted => _faulted.Task;

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
            _faulted.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed record SendObservation(
        string Stage,
        Type MessageType,
        object Message,
        SendContext Context,
        Exception? Exception);

    private sealed record ObservedSend(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed record ResponseRequest(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record ResponseMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
}
