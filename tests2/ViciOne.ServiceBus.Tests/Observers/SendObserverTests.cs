using System.Collections.Concurrent;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Observers;

public sealed class SendObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-OBSERVER", "bus-endpoint-success-and-disconnect")]
    public async Task BusAndEndpointObservers_SeeTheSameSuccessfulSendAndDisconnectIndependently()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<ObservedSend> handler = harness.Handler<ObservedSend>();

        await harness.Start(cancellationToken);
        try
        {
            var busObserver = new RecordingSendObserver();
            var endpointObserver = new RecordingSendObserver();
            using ConnectHandle busHandle = harness.Bus.ConnectSendObserver(busObserver);
            using ConnectHandle endpointHandle = harness.InputQueueSendEndpoint.ConnectSendObserver(endpointObserver);
            var first = new ObservedSend(NewId.NextGuid(), "first");

            await harness.InputQueueSendEndpoint.Send(first, cancellationToken);
            IReceivedMessage<ObservedSend> firstConsumed = await handler.Consumed
                .SelectAsync(cancellationToken)
                .First();

            Assert.Equal(first, firstConsumed.Context.Message);
            AssertSuccessfulSend(busObserver.Events, endpointObserver.Events, first, harness.InputQueueAddress);

            endpointHandle.Dispose();
            endpointHandle.Dispose();
            var second = new ObservedSend(NewId.NextGuid(), "second");

            await harness.InputQueueSendEndpoint.Send(second, cancellationToken);
            IReceivedMessage<ObservedSend> secondConsumed = await handler.Consumed
                .SelectAsync(observation => observation.Context.Message.CorrelationId == second.CorrelationId, cancellationToken)
                .First();

            Assert.Equal(second, secondConsumed.Context.Message);
            Assert.Equal(["Pre", "Post", "Pre", "Post"], busObserver.Events.Select(observation => observation.Stage));
            Assert.Equal(["Pre", "Post"], endpointObserver.Events.Select(observation => observation.Stage));
            Assert.Equal(second, busObserver.Events[^1].Message);
            Assert.DoesNotContain(endpointObserver.Events, observation => Equals(observation.Message, second));
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-OBSERVER", "bus-endpoint-fault-without-post")]
    public async Task BusAndEndpointObservers_SeeTheExactSendFailureWithoutPostSend()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        harness.Handler<ObservedSend>();

        await harness.Start(cancellationToken);
        try
        {
            var busObserver = new RecordingSendObserver();
            var endpointObserver = new RecordingSendObserver();
            using ConnectHandle busHandle = harness.Bus.ConnectSendObserver(busObserver);
            using ConnectHandle endpointHandle = harness.InputQueueSendEndpoint.ConnectSendObserver(endpointObserver);
            var message = new ObservedSend(NewId.NextGuid(), "cannot-serialize");

            SerializationException failure = await Assert.ThrowsAsync<SerializationException>(() =>
                harness.InputQueueSendEndpoint.Send(
                    message,
                    context => context.Serializer = null!,
                    cancellationToken));

            AssertFaultedSend(busObserver.Events, message, harness.InputQueueAddress, failure);
            AssertFaultedSend(endpointObserver.Events, message, harness.InputQueueAddress, failure);
            Assert.Same(busObserver.Events[0].Context, endpointObserver.Events[0].Context);
            Assert.Same(busObserver.Events[1].Context, endpointObserver.Events[1].Context);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-OBSERVER", "response-success-and-fault-boundaries")]
    public async Task BusObserver_DistinguishesTheOriginalSendFromAFaultedResponseSend()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var responseFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        harness.Handler<ResponseRequest>(async context =>
        {
            try
            {
                await context.RespondAsync(
                    new ResponseMessage(context.Message.CorrelationId),
                    sendContext => sendContext.Serializer = null!);
            }
            catch (Exception exception)
            {
                responseFailure.TrySetResult(exception);
            }
        });

        await harness.Start(cancellationToken);
        try
        {
            var observer = new RecordingSendObserver();
            using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(observer);
            var request = new ResponseRequest(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.Send(
                request,
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);

            Exception failure = await responseFailure.Task.WaitAsync(timeout, cancellationToken);
            await observer.Faulted.WaitAsync(timeout, cancellationToken);

            SendObservation[] events = observer.Events;
            Assert.Equal(["Pre", "Post", "Pre", "Fault"], events.Select(observation => observation.Stage));
            Assert.Equal([typeof(ResponseRequest), typeof(ResponseRequest), typeof(ResponseMessage), typeof(ResponseMessage)],
                events.Select(observation => observation.MessageType));
            Assert.Equal(request, events[0].Message);
            Assert.Equal(request, events[1].Message);
            Assert.Equal(new ResponseMessage(request.CorrelationId), events[2].Message);
            Assert.Equal(new ResponseMessage(request.CorrelationId), events[3].Message);
            Assert.Same(failure, events[3].Exception);
            Assert.IsType<SerializationException>(failure);
            Assert.Equal(harness.InputQueueAddress, events[0].Context.DestinationAddress);
            Assert.Equal(harness.BusAddress, events[2].Context.DestinationAddress);
        }
        finally
        {
            await harness.Stop();
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

    private sealed record ObservedSend(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record ResponseRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record ResponseMessage(Guid CorrelationId) : CorrelatedBy<Guid>;
}
