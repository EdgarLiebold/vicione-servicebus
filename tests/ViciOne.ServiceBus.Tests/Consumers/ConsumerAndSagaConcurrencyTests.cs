using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class ConsumerAndSagaConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONCURRENCY", "two-message-types-third-waits")]
    public async Task ConsumerConcurrencyLimit_HoldsTheThirdAdmittedDeliveryUntilASlotIsReleasedAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var probe = new ConcurrencyProbe(3);
        var endpointAdmittedAll = NewSignal();
        var endpointArrivals = 0;
        using var harness = CreateHarness("consumer-concurrency", timeout);
        var consumer = new LimitedConsumer(probe);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.ConcurrentMessageLimit = 3;
            endpoint.UseExecute(_ =>
            {
                if (Interlocked.Increment(ref endpointArrivals) == 3)
                    endpointAdmittedAll.TrySetResult();
            });
            endpoint.Consumer(() => consumer, consumerConfiguration =>
                consumerConfiguration.ConcurrencyPolicy = ConsumerConcurrencyPolicy.Parallel(2));
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Task[] sends =
            [
                harness.InputQueueSendEndpoint.SendAsync(new ConsumerMessageA(1), cancellationToken),
                harness.InputQueueSendEndpoint.SendAsync(new ConsumerMessageB(2), cancellationToken),
                harness.InputQueueSendEndpoint.SendAsync(new ConsumerMessageA(3), cancellationToken),
            ];

            await Task.WhenAll(sends).WaitAsync(timeout, cancellationToken);
            await probe.LimitReached.Task.WaitAsync(timeout, cancellationToken);
            await endpointAdmittedAll.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(2, probe.Entered);
            Assert.Equal(2, probe.Active);
            Assert.False(probe.BeyondLimitEntered.Task.IsCompleted);

            probe.Release.Release();
            await probe.BeyondLimitEntered.Task.WaitAsync(timeout, cancellationToken);
            probe.Release.Release(2);
            await probe.AllCompleted.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(2, probe.Maximum);
            Assert.Equal(3, probe.Entered);
            Assert.Equal(3, probe.Completed);
            Assert.Equal(0, probe.Active);
        }
        finally
        {
            probe.Release.Release(3);
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY-RUNTIME", "typed-partition-hook-in-real-consume-pipeline")]
    public async Task PartitionedConsumerConcurrency_SeparatesDifferentKeysAndExcludesTheSameKeyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("partitioned-consumer-concurrency", timeout);
        var consumer = new PartitionedConsumer();
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.ConcurrentMessageLimit = 3;
            endpoint.Consumer(() => consumer, configuration =>
                configuration.UsePartitionedConcurrency<PartitionedMessage, int>(4, static message => message.Key));
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new PartitionedMessage(1, 1), cancellationToken);
            await consumer.FirstEntered.Task.WaitAsync(timeout, cancellationToken);

            await Task.WhenAll(
                harness.InputQueueSendEndpoint.SendAsync(new PartitionedMessage(1, 2), cancellationToken),
                harness.InputQueueSendEndpoint.SendAsync(new PartitionedMessage(2, 3), cancellationToken))
                .WaitAsync(timeout, cancellationToken);

            await consumer.DifferentKeyEntered.Task.WaitAsync(timeout, cancellationToken);
            Assert.False(consumer.SameKeyEntered.Task.IsCompleted);

            consumer.ReleaseFirst.TrySetResult();
            await consumer.SameKeyEntered.Task.WaitAsync(timeout, cancellationToken);
            await consumer.AllCompleted.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            consumer.ReleaseFirst.TrySetResult();
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONCURRENCY", "two-message-types-third-waits")]
    public async Task SagaConcurrencyLimit_HoldsTheThirdAdmittedDeliveryUntilASlotIsReleasedAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var probe = new ConcurrencyProbe(3);
        var endpointAdmittedAll = NewSignal();
        var endpointArrivals = 0;
        using var harness = CreateHarness("saga-concurrency", timeout);
        LimitedSaga.Probe = probe;
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.ConcurrentMessageLimit = 3;
            endpoint.UseExecute(_ =>
            {
                if (Interlocked.Increment(ref endpointArrivals) == 3)
                    endpointAdmittedAll.TrySetResult();
            });
            endpoint.Saga(new InMemorySagaRepository<LimitedSaga>(), sagaConfiguration =>
                sagaConfiguration.ConcurrentMessageLimit = 2);
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Task[] publishes =
            [
                harness.Bus.PublishAsync(new SagaMessageA(NewId.NextGuid()), cancellationToken),
                harness.Bus.PublishAsync(new SagaMessageB(NewId.NextGuid()), cancellationToken),
                harness.Bus.PublishAsync(new SagaMessageA(NewId.NextGuid()), cancellationToken),
            ];

            await Task.WhenAll(publishes).WaitAsync(timeout, cancellationToken);
            await probe.LimitReached.Task.WaitAsync(timeout, cancellationToken);
            await endpointAdmittedAll.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(2, probe.Entered);
            Assert.Equal(2, probe.Active);
            Assert.False(probe.BeyondLimitEntered.Task.IsCompleted);

            probe.Release.Release();
            await probe.BeyondLimitEntered.Task.WaitAsync(timeout, cancellationToken);
            probe.Release.Release(2);
            await probe.AllCompleted.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(2, probe.Maximum);
            Assert.Equal(3, probe.Entered);
            Assert.Equal(3, probe.Completed);
            Assert.Equal(0, probe.Active);
        }
        finally
        {
            probe.Release.Release(3);
            LimitedSaga.Probe = null;
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static InMemoryTestHarness CreateHarness(string name, TimeSpan timeout) =>
        new($"{name}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class ConcurrencyProbe(int expected)
    {
        private int _active;
        private int _completed;
        private int _entered;
        private int _maximum;

        public SemaphoreSlim Release { get; } = new(0);

        public TaskCompletionSource LimitReached { get; } = NewSignal();

        public TaskCompletionSource BeyondLimitEntered { get; } = NewSignal();

        public TaskCompletionSource AllCompleted { get; } = NewSignal();

        public int Active => Volatile.Read(ref _active);

        public int Completed => Volatile.Read(ref _completed);

        public int Entered => Volatile.Read(ref _entered);

        public int Maximum => Volatile.Read(ref _maximum);

        public async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            int active = Interlocked.Increment(ref _active);
            UpdateMaximum(ref _maximum, active);
            int entered = Interlocked.Increment(ref _entered);
            if (entered == 2)
                LimitReached.TrySetResult();
            else if (entered > 2)
                BeyondLimitEntered.TrySetResult();

            try
            {
                await Release.WaitAsync(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
                if (Interlocked.Increment(ref _completed) == expected)
                    AllCompleted.TrySetResult();
            }
        }

        private static void UpdateMaximum(ref int maximum, int candidate)
        {
            int observed;
            while (candidate > (observed = Volatile.Read(ref maximum)))
            {
                if (Interlocked.CompareExchange(ref maximum, candidate, observed) == observed)
                    return;
            }
        }
    }

    private sealed class LimitedConsumer(ConcurrencyProbe probe) :
        IConsumer<ConsumerMessageA>,
        IConsumer<ConsumerMessageB>
    {
        public Task ConsumeAsync(ConsumeContext<ConsumerMessageA> context) => probe.ConsumeAsync(context.CancellationToken);

        public Task ConsumeAsync(ConsumeContext<ConsumerMessageB> context) => probe.ConsumeAsync(context.CancellationToken);
    }

    private sealed record ConsumerMessageA(int Index);

    private sealed record ConsumerMessageB(int Index);

    private sealed record PartitionedMessage(int Key, int Sequence);

    private sealed class PartitionedConsumer : IConsumer<PartitionedMessage>
    {
        private int _completed;

        public TaskCompletionSource FirstEntered { get; } = NewSignal();

        public TaskCompletionSource SameKeyEntered { get; } = NewSignal();

        public TaskCompletionSource DifferentKeyEntered { get; } = NewSignal();

        public TaskCompletionSource ReleaseFirst { get; } = NewSignal();

        public TaskCompletionSource AllCompleted { get; } = NewSignal();

        public async Task ConsumeAsync(ConsumeContext<PartitionedMessage> context)
        {
            switch (context.Message.Sequence)
            {
                case 1:
                    FirstEntered.TrySetResult();
                    await ReleaseFirst.Task.WaitAsync(context.CancellationToken);
                    break;
                case 2:
                    SameKeyEntered.TrySetResult();
                    break;
                case 3:
                    DifferentKeyEntered.TrySetResult();
                    break;
            }

            if (Interlocked.Increment(ref _completed) == 3)
                AllCompleted.TrySetResult();
        }
    }

    private sealed class LimitedSaga :
        InitiatedByOrOrchestrates<SagaMessageA>,
        InitiatedByOrOrchestrates<SagaMessageB>,
        ISaga
    {
        public static ConcurrencyProbe? Probe { get; set; }

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<SagaMessageA> context) =>
            (Probe ?? throw new InvalidOperationException("Saga probe is not configured."))
            .ConsumeAsync(context.CancellationToken);

        public Task ConsumeAsync(ConsumeContext<SagaMessageB> context) =>
            (Probe ?? throw new InvalidOperationException("Saga probe is not configured."))
            .ConsumeAsync(context.CancellationToken);
    }

    private sealed record SagaMessageA(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record SagaMessageB(Guid CorrelationId) : CorrelatedBy<Guid>;
}
