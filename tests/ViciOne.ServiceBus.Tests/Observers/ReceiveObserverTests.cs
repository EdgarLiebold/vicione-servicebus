using System.Collections.Concurrent;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Observers;

public sealed class ReceiveObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-OBSERVER", "handler-and-consumer-success")]
    public async Task SuccessfulHandlerAndConsumer_ReportExactReceiveAndConsumeBoundaries()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        harness.Handler<HandledMessage>();
        harness.Consumer<ObservedConsumer>();

        await harness.Start(cancellationToken);
        try
        {
            var observer = new RecordingReceiveObserver(expectedPostReceiveCount: 2);
            using ConnectHandle observerHandle = harness.Bus.ConnectReceiveObserver(observer);
            var handled = new HandledMessage(NewId.NextGuid());
            var consumed = new ConsumedMessage(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.Send(handled, cancellationToken);
            await observer.FirstPostReceive.WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(consumed, cancellationToken);
            await observer.AllExpectedPostReceives.WaitAsync(timeout, cancellationToken);

            ReceiveObservation[] events = observer.Events;
            AssertSuccessfulDelivery(
                events,
                typeof(HandledMessage),
                TypeCache<MessageHandler<HandledMessage>>.ShortName,
                handled.CorrelationId);
            AssertSuccessfulDelivery(
                events,
                typeof(ConsumedMessage),
                TypeCache<ObservedConsumer>.ShortName,
                consumed.CorrelationId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-OBSERVER", "consumer-fault-is-handled-receive")]
    public async Task ConsumerFailure_ReportsConsumeFaultThenCompletesTheHandledReceive()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("consumer failed");
        using var harness = CreateHarness(timeout);
        harness.Handler<FaultingMessage>(_ => Task.FromException(expected));

        await harness.Start(cancellationToken);
        try
        {
            var observer = new RecordingReceiveObserver(expectedPostReceiveCount: 1);
            using ConnectHandle observerHandle = harness.Bus.ConnectReceiveObserver(observer);
            var message = new FaultingMessage(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.Send(message, cancellationToken);
            await observer.FirstConsumeFault.WaitAsync(timeout, cancellationToken);
            await observer.FirstPostReceive.WaitAsync(timeout, cancellationToken);

            ReceiveObservation[] events = observer.Events;
            ReceiveObservation fault = Assert.Single(events, observation => observation.Stage == "ConsumeFault");
            ConsumeContext consumeContext = Assert.IsAssignableFrom<ConsumeContext>(fault.ConsumeContext);
            ReceiveObservation[] delivery = ForDelivery(events, consumeContext.ReceiveContext);
            Assert.Equal(["PreReceive", "ConsumeFault", "PostReceive"], delivery.Select(observation => observation.Stage));
            Assert.Equal(typeof(FaultingMessage), fault.MessageType);
            Assert.Equal(message, fault.Message);
            Assert.Equal(message.CorrelationId, consumeContext.CorrelationId);
            Assert.Equal(TypeCache<MessageHandler<FaultingMessage>>.ShortName, fault.ConsumerType);
            Assert.InRange(fault.Duration!.Value, TimeSpan.Zero, consumeContext.ReceiveContext.ElapsedTime);
            Assert.Same(expected, fault.Exception);
            Assert.Same(delivery[0].ReceiveContext, consumeContext.ReceiveContext);
            Assert.Same(delivery[0].ReceiveContext, delivery[2].ReceiveContext);
            Assert.DoesNotContain(delivery, observation => observation.Stage is "PostConsume" or "ReceiveFault");
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-OBSERVER", "post-consume-pipeline-receive-fault")]
    public async Task FailureAfterSuccessfulConsumption_ReportsReceiveFaultThenCompletesTheHandledDelivery()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("post-consume middleware failed");
        using var harness = CreateHarness(timeout);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.AddPrePipeSpecification(
                new FilterPipeSpecification<ConsumeContext>(new PostConsumeFailureFilter(expected)));
            configurator.Handler<PostPipelineMessage>(_ => Task.CompletedTask);
        };

        await harness.Start(cancellationToken);
        try
        {
            var observer = new RecordingReceiveObserver(expectedPostReceiveCount: 1);
            using ConnectHandle observerHandle = harness.Bus.ConnectReceiveObserver(observer);
            var message = new PostPipelineMessage(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.Send(message, cancellationToken);
            await observer.FirstReceiveFault.WaitAsync(timeout, cancellationToken);
            await observer.FirstPostReceive.WaitAsync(timeout, cancellationToken);

            ReceiveObservation[] events = observer.Events;
            ReceiveObservation fault = Assert.Single(events, observation =>
                observation.Stage == "ReceiveFault" && ReferenceEquals(observation.Exception, expected));
            ReceiveContext receiveContext = Assert.IsAssignableFrom<ReceiveContext>(fault.ReceiveContext);
            ReceiveObservation[] delivery = ForDelivery(events, receiveContext);
            Assert.Equal(["PreReceive", "PostConsume", "ReceiveFault", "PostReceive"], delivery.Select(observation => observation.Stage));
            ReceiveObservation consumed = delivery[1];
            ConsumeContext consumeContext = Assert.IsAssignableFrom<ConsumeContext>(consumed.ConsumeContext);
            Assert.Equal(typeof(PostPipelineMessage), consumed.MessageType);
            Assert.Equal(message, consumed.Message);
            Assert.Equal(TypeCache<MessageHandler<PostPipelineMessage>>.ShortName, consumed.ConsumerType);
            Assert.Same(expected, fault.Exception);
            Assert.Same(delivery[0].ReceiveContext, consumeContext.ReceiveContext);
            Assert.Same(delivery[0].ReceiveContext, fault.ReceiveContext);
            Assert.Same(delivery[0].ReceiveContext, delivery[3].ReceiveContext);
            Assert.DoesNotContain(delivery, observation => observation.Stage == "ConsumeFault");
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static void AssertSuccessfulDelivery(
        ReceiveObservation[] events,
        Type messageType,
        string consumerType,
        Guid correlationId)
    {
        ReceiveObservation consumed = Assert.Single(events, observation =>
            observation.Stage == "PostConsume" && observation.MessageType == messageType);
        ConsumeContext consumeContext = Assert.IsAssignableFrom<ConsumeContext>(consumed.ConsumeContext);
        ReceiveObservation[] delivery = ForDelivery(events, consumeContext.ReceiveContext);
        Assert.Equal(["PreReceive", "PostConsume", "PostReceive"], delivery.Select(observation => observation.Stage));
        Assert.Equal(messageType, consumed.MessageType);
        Assert.Equal(correlationId, consumeContext.CorrelationId);
        Assert.Equal(consumerType, consumed.ConsumerType);
        Assert.InRange(consumed.Duration!.Value, TimeSpan.Zero, consumeContext.ReceiveContext.ElapsedTime);
        Assert.Null(consumed.Exception);
        Assert.Same(delivery[0].ReceiveContext, consumeContext.ReceiveContext);
        Assert.Same(delivery[0].ReceiveContext, delivery[2].ReceiveContext);
    }

    private static ReceiveObservation[] ForDelivery(IEnumerable<ReceiveObservation> events, ReceiveContext context) =>
        events.Where(observation => ReferenceEquals(observation.DeliveryContext, context)).ToArray();

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"receive-observer-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed class RecordingReceiveObserver(int expectedPostReceiveCount) : IReceiveObserver
    {
        private readonly ConcurrentQueue<ReceiveObservation> _events = new();
        private readonly TaskCompletionSource _allExpectedPostReceives =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstConsumeFault =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstPostReceive =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstReceiveFault =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _postReceiveCount;

        public ReceiveObservation[] Events => _events.ToArray();

        public Task AllExpectedPostReceives => expectedPostReceiveCount == 0
            ? Task.CompletedTask
            : _allExpectedPostReceives.Task;

        public Task FirstConsumeFault => _firstConsumeFault.Task;

        public Task FirstPostReceive => _firstPostReceive.Task;

        public Task FirstReceiveFault => _firstReceiveFault.Task;

        public Task PreReceive(ReceiveContext context)
        {
            _events.Enqueue(new ReceiveObservation("PreReceive", context, null, null, null, null, null, null));
            return Task.CompletedTask;
        }

        public Task PostReceive(ReceiveContext context)
        {
            _events.Enqueue(new ReceiveObservation("PostReceive", context, null, null, null, null, null, null));
            _firstPostReceive.TrySetResult();
            if (Interlocked.Increment(ref _postReceiveCount) == expectedPostReceiveCount)
                _allExpectedPostReceives.TrySetResult();

            return Task.CompletedTask;
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class
        {
            _events.Enqueue(new ReceiveObservation("PostConsume", null, context, context.Message, typeof(T), duration, consumerType, null));
            return Task.CompletedTask;
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class
        {
            _events.Enqueue(new ReceiveObservation("ConsumeFault", null, context, context.Message, typeof(T), duration, consumerType, exception));
            _firstConsumeFault.TrySetResult();
            return Task.CompletedTask;
        }

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            _events.Enqueue(new ReceiveObservation("ReceiveFault", context, null, null, null, null, null, exception));
            _firstReceiveFault.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class PostConsumeFailureFilter(Exception failure) : IFilter<ConsumeContext>
    {
        public async Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            await next.Send(context);
            throw failure;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("postConsumeFailure");
        }
    }

    private sealed class ObservedConsumer : IConsumer<ConsumedMessage>
    {
        public Task Consume(ConsumeContext<ConsumedMessage> context) => Task.CompletedTask;
    }

    private sealed record ReceiveObservation(
        string Stage,
        ReceiveContext? ReceiveContext,
        ConsumeContext? ConsumeContext,
        object? Message,
        Type? MessageType,
        TimeSpan? Duration,
        string? ConsumerType,
        Exception? Exception)
    {
        public ReceiveContext? DeliveryContext => ReceiveContext ?? ConsumeContext?.ReceiveContext;
    }

    private sealed record HandledMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record ConsumedMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record FaultingMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record PostPipelineMessage(Guid CorrelationId) : CorrelatedBy<Guid>;
}
