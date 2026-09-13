using System.Collections.Concurrent;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryFaultPublicationTests
{
    private const int ConcurrentDeliveries = 32;
    private const int StormSize = 500;

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-CONTEXT-PUBLISH", "four-distinct-events-exactly-once")]
    public async Task ConsumerPublishesAllFourEventsExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("event-fanout", timeout);
        var recorder = new EventRecorder(expectedCount: 4);
        DateTime timestamp = new(2031, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.Handler<EventCommand>(async context =>
            {
                Guid correlationId = context.Message.CorrelationId;
                await context.Advanced().PublishAsync(new FirstEvent(correlationId, timestamp), context.CancellationToken);
                await context.Advanced().PublishAsync(new SecondEvent(correlationId, timestamp.AddMinutes(1)), context.CancellationToken);
                await context.Advanced().PublishAsync(new ThirdEvent(correlationId, timestamp.AddMinutes(2)), context.CancellationToken);
                await context.Advanced().PublishAsync(new FourthEvent(correlationId, timestamp.AddMinutes(3)), context.CancellationToken);
            });
            endpoint.Handler<FirstEvent>(recorder.RecordAsync);
            endpoint.Handler<SecondEvent>(recorder.RecordAsync);
            endpoint.Handler<ThirdEvent>(recorder.RecordAsync);
            endpoint.Handler<FourthEvent>(recorder.RecordAsync);
        };
        var command = new EventCommand(Guid.Parse("e2e1bffa-a03a-43c3-ad97-82fef0e147df"));
        bool started = false;

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;
            await harness.InputQueueSendEndpoint.SendAsync(command, cancellationToken).WaitAsync(timeout, cancellationToken);
            await recorder.Completed.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            started = false;

            EventObservation[] observations = recorder.Observations;
            Assert.Equal(4, observations.Length);
            Assert.Equal(["first", "fourth", "second", "third"],
                observations.Select(observation => observation.Name).Order(StringComparer.Ordinal));
            Assert.All(observations, observation => Assert.Equal(command.CorrelationId, observation.CorrelationId));
            Assert.Equal(
                [timestamp, timestamp.AddMinutes(1), timestamp.AddMinutes(2), timestamp.AddMinutes(3)],
                observations.Select(observation => observation.Timestamp).Order());
        }
        finally
        {
            if (started)
                await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-STORM", "five-hundred-messages-produce-five-hundred-exact-faults")]
    public async Task FaultStorm_PublishesExactlyOneFaultPerMessageAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("fault-storm", timeout);
        var recorder = new FaultRecorder(StormSize);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.PrefetchCount = ConcurrentDeliveries;
            endpoint.ConcurrentMessageLimit = ConcurrentDeliveries;
            endpoint.Consumer<StormConsumer>();
            endpoint.Handler<Fault<StormMessage>>(context => recorder.RecordAsync(context.Message));
        };
        bool started = false;

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;
            Task[] sends = Enumerable.Range(0, StormSize)
                .Select(index => harness.InputQueueSendEndpoint.SendAsync(new StormMessage(index), cancellationToken))
                .ToArray();
            await Task.WhenAll(sends).WaitAsync(timeout, cancellationToken);
            await recorder.Completed.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Fault<StormMessage>[] faults = recorder.Faults;
            IPublishedMessage<Fault<StormMessage>>[] publishedFaults = harness.Published
                .Snapshot<Fault<StormMessage>>()
                .ToArray();
            Assert.Equal(StormSize, faults.Length);
            Assert.Equal(StormSize, publishedFaults.Length);
            Assert.Equal(Enumerable.Range(0, StormSize),
                faults.Select(fault => fault.Message.Sequence).Order());
            Assert.All(faults, fault =>
            {
                ExceptionInfo exception = Assert.Single(fault.Exceptions);
                Assert.Equal(TypeCache<StormFailureException>.ShortName, exception.ExceptionType);
                Assert.Equal(StormFailureException.FailureMessage, exception.Message);
            });
        }
        finally
        {
            if (started)
                await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(string purpose, TimeSpan timeout) =>
        new($"inmemory-{purpose}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private sealed record EventCommand(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private interface ConsumerEvent
    {
        string Name { get; }

        Guid CorrelationId { get; }

        DateTime Timestamp { get; }
    }

    private sealed record FirstEvent(Guid CorrelationId, DateTime Timestamp) : ConsumerEvent
    {
        public string Name => "first";
    }

    private sealed record SecondEvent(Guid CorrelationId, DateTime Timestamp) : ConsumerEvent
    {
        public string Name => "second";
    }

    private sealed record ThirdEvent(Guid CorrelationId, DateTime Timestamp) : ConsumerEvent
    {
        public string Name => "third";
    }

    private sealed record FourthEvent(Guid CorrelationId, DateTime Timestamp) : ConsumerEvent
    {
        public string Name => "fourth";
    }

    private sealed record EventObservation(string Name, Guid CorrelationId, DateTime Timestamp);

    private sealed class EventRecorder(int expectedCount)
    {
        private readonly ConcurrentQueue<EventObservation> _observations = new();
        private readonly TaskCompletionSource<bool> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _count;

        public Task Completed => _completed.Task;

        public EventObservation[] Observations => _observations.ToArray();

        public Task RecordAsync<T>(ConsumeContext<T> context)
            where T : class, ConsumerEvent
        {
            _observations.Enqueue(new EventObservation(
                context.Message.Name,
                context.Message.CorrelationId,
                context.Message.Timestamp));
            if (Interlocked.Increment(ref _count) == expectedCount)
                _completed.TrySetResult(true);

            return Task.CompletedTask;
        }
    }

    private sealed record StormMessage(int Sequence);

    private sealed class StormConsumer : IConsumer<StormMessage>
    {
        public Task ConsumeAsync(ConsumeContext<StormMessage> context) =>
            Task.FromException(new StormFailureException());
    }

    private sealed class StormFailureException : Exception
    {
        public const string FailureMessage = "intentional fault storm failure";

        public StormFailureException()
            : base(FailureMessage)
        {
        }
    }

    private sealed class FaultRecorder(int expectedCount)
    {
        private readonly ConcurrentQueue<Fault<StormMessage>> _observed = new();
        private readonly ConcurrentDictionary<int, Fault<StormMessage>> _faults = new();
        private readonly TaskCompletionSource<bool> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completed => _completed.Task;

        public Fault<StormMessage>[] Faults => _observed.ToArray();

        public Task RecordAsync(Fault<StormMessage> fault)
        {
            _observed.Enqueue(fault);
            if (_faults.TryAdd(fault.Message.Sequence, fault) && _faults.Count == expectedCount)
                _completed.TrySetResult(true);

            return Task.CompletedTask;
        }
    }
}
