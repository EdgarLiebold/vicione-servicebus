using System.Collections.Concurrent;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EventHubIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubIntegration.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubSagaAndLifecycleTests
{
    private const string SagaEventHub = "saga-eh";
    private const string LifecycleEventHub = "lifecycle-eh";

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0153", "saga-transition-produces-state-derived-event-with-correlation-as-initiator")]
    public async Task SagaTransition_ProducesStateDerivedEventWithTheSagaCorrelationAsInitiator()
    {
        var state = new SagaDeliveryState(NewId.NextGuid(), expected: 1);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("saga-produce");
        await using ServiceProvider provider = BuildSagaProvider(fixture, state, includeFaultMachines: false);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid correlationId = NewId.NextGuid();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
                .Publish(new SagaStart(correlationId, state.RunId, "ABC123"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<SagaProduced> actual = (await state.Completed.Task
                    .WaitAsync(fixture.OperationTimeout, cancellationToken))
                .Single();

            Assert.Equal(state.RunId, actual.Message.RunId);
            Assert.Equal("Key: ABC123", actual.Message.Text);
            Assert.Equal(correlationId, actual.InitiatorId);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0263", "both-faulted-produce-arities-carry-exception-data-and-saga-initiator")]
    public async Task FaultedSagaActivities_BothProduceExceptionDerivedEventsWithTheSagaInitiator()
    {
        var state = new SagaDeliveryState(NewId.NextGuid(), expected: 2);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("saga-faulted");
        await using ServiceProvider provider = BuildSagaProvider(fixture, state, includeFaultMachines: true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid dataCorrelationId = NewId.NextGuid();
            Guid plainCorrelationId = NewId.NextGuid();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publishEndpoint.Publish(new DataFaultStart(dataCorrelationId, state.RunId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await publishEndpoint.Publish(new PlainFaultStart(plainCorrelationId, state.RunId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<SagaProduced>[] deliveries = await state.Completed.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(2, deliveries.Length);
            ConsumeContext<SagaProduced> data = Assert.Single(deliveries, context => context.Message.Mode == "data");
            ConsumeContext<SagaProduced> plain = Assert.Single(deliveries, context => context.Message.Mode == "plain");
            Assert.Equal("data-failure", data.Message.Text);
            Assert.Equal("plain-failure", plain.Message.Text);
            Assert.Equal(dataCorrelationId, data.InitiatorId);
            Assert.Equal(plainCorrelationId, plain.InitiatorId);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0260", "partition-initialize-precedes-delivery-and-clean-close-follows-for-every-partition")]
    public async Task PartitionCallbacks_BracketDeliveryForEveryPartitionAndReportCleanShutdown()
    {
        var state = new LifecycleState(NewId.NextGuid(), expectedDeliveries: 1);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("partition-callbacks");
        await using ServiceProvider provider = BuildLifecycleProvider(fixture, state);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await state.FirstInitialized.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await ProduceLifecycleMessage(provider, state.RunId, index: 0, fixture.OperationTimeout, cancellationToken);
            await state.DeliveriesCompleted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;
            await state.FirstClosed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            LifecycleSnapshot snapshot = state.Snapshot();
            Assert.Equal(["0", "1", "2", "3"], snapshot.FirstInitializedPartitions);
            Assert.Equal(["0", "1", "2", "3"], snapshot.FirstClosedPartitions);
            Assert.All(snapshot.FirstCloseReasons, reason => Assert.Equal(ProcessingStoppedReason.Shutdown, reason));
            LifecycleDelivery delivery = Assert.Single(snapshot.Deliveries);
            Assert.True(snapshot.FirstInitializeSequence[delivery.PartitionId] < delivery.Sequence);
            Assert.True(delivery.Sequence < snapshot.FirstCloseSequence[delivery.PartitionId]);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0159", "consumer-reclaims-all-partitions-after-stop-start-and-delivers-once")]
    public async Task ConsumerRecycle_ReleasesAndReclaimsEveryPartitionBeforeDeliveringExactlyOnce()
    {
        var state = new LifecycleState(NewId.NextGuid(), expectedDeliveries: 1);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("consumer-recycle");
        await using ServiceProvider provider = BuildLifecycleProvider(fixture, state);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await state.FirstInitialized.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;
            await state.FirstClosed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await state.SecondInitialized.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await ProduceLifecycleMessage(provider, state.RunId, index: 41, fixture.OperationTimeout, cancellationToken);
            await state.DeliveriesCompleted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            LifecycleSnapshot snapshot = state.Snapshot();
            Assert.Equal(8, snapshot.InitializedCount);
            Assert.Equal(8, snapshot.ClosedCount);
            Assert.Equal(["0", "1", "2", "3"], snapshot.SecondInitializedPartitions);
            LifecycleDelivery delivery = Assert.Single(snapshot.Deliveries);
            Assert.Equal(41, delivery.Index);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0160", "fresh-producer-scope-after-recycle-delivers-after-the-pre-recycle-producer")]
    public async Task ProducerRecycle_DeliversFromFreshScopesOnBothSidesOfTheRestart()
    {
        var state = new LifecycleState(NewId.NextGuid(), expectedDeliveries: 2);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("producer-recycle");
        await using ServiceProvider provider = BuildLifecycleProvider(fixture, state);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await state.FirstInitialized.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await ProduceLifecycleMessage(provider, state.RunId, index: 0, fixture.OperationTimeout, cancellationToken);
            await state.FirstDelivery.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;
            await state.FirstClosed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await state.SecondInitialized.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await ProduceLifecycleMessage(provider, state.RunId, index: 1, fixture.OperationTimeout, cancellationToken);
            await state.DeliveriesCompleted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;
            await state.SecondClosed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            LifecycleSnapshot snapshot = state.Snapshot();
            Assert.Equal(8, snapshot.InitializedCount);
            Assert.Equal(8, snapshot.ClosedCount);
            LifecycleDelivery[] deliveries = snapshot.Deliveries.OrderBy(delivery => delivery.Index).ToArray();
            Assert.Equal([0, 1], deliveries.Select(delivery => delivery.Index));
            Assert.Equal(2, deliveries.Select(delivery => delivery.Index).Distinct().Count());
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static ServiceProvider BuildSagaProvider(
        EventHubLocalFixture fixture,
        SagaDeliveryState state,
        bool includeFaultMachines)
    {
        return new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.AddSagaStateMachine<ProducingSagaMachine, ProducingSagaState>().InMemoryRepository();
                if (includeFaultMachines)
                {
                    configuration.AddSagaStateMachine<DataFaultSagaMachine, DataFaultSagaState>().InMemoryRepository();
                    configuration.AddSagaStateMachine<PlainFaultSagaMachine, PlainFaultSagaState>().InMemoryRepository();
                }

                configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<SagaProducedConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(SagaEventHub, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("saga");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<SagaProducedConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
    }

    private static ServiceProvider BuildLifecycleProvider(EventHubLocalFixture fixture, LifecycleState state)
    {
        return new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<LifecycleConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(LifecycleEventHub, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("lifecycle");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConcurrentDeliveryLimit = 1;
                            endpoint.OnPartitionInitializing(state.OnInitializing);
                            endpoint.OnPartitionClosing(state.OnClosing);
                            endpoint.ConfigureConsumer<LifecycleConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
    }

    private static async Task ProduceLifecycleMessage(
        ServiceProvider provider,
        Guid runId,
        int index,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
            .GetProducer(LifecycleEventHub).WaitAsync(timeout, cancellationToken);
        await producer.Produce(new LifecycleMessage(runId, index), cancellationToken)
            .WaitAsync(timeout, cancellationToken);
    }

    public sealed record SagaStart(Guid CorrelationId, Guid RunId, string Key) : CorrelatedBy<Guid>;
    public sealed record DataFaultStart(Guid CorrelationId, Guid RunId) : CorrelatedBy<Guid>;
    public sealed record PlainFaultStart(Guid CorrelationId, Guid RunId) : CorrelatedBy<Guid>;
    public sealed record SagaProduced(Guid RunId, string Mode, string Text);

    public sealed class ProducingSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public Guid RunId { get; set; }
        public string Key { get; set; } = string.Empty;
    }

    public sealed class ProducingSagaMachine : ViciOneServiceBusStateMachine<ProducingSagaState>
    {
        public ProducingSagaMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(Start)
                    .Then(context =>
                    {
                        context.Saga.RunId = context.Message.RunId;
                        context.Saga.Key = context.Message.Key;
                    })
                    .Produce(
                        _ => SagaEventHub,
                        context => Task.FromResult(new SagaProduced(
                            context.Saga.RunId,
                            "success",
                            $"Key: {context.Saga.Key}")))
                    .TransitionTo(Active));
        }

        public State Active { get; } = null!;
        public Event<SagaStart> Start { get; } = null!;
    }

    public sealed class DataFaultSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public Guid RunId { get; set; }
    }

    public sealed class DataFaultSagaMachine : ViciOneServiceBusStateMachine<DataFaultSagaState>
    {
        public DataFaultSagaMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(Start)
                    .Then(context =>
                    {
                        context.Saga.RunId = context.Message.RunId;
                        throw new ExpectedSagaFailure("data-failure");
                    })
                    .Catch<ExpectedSagaFailure>(caught => caught
                        .Produce(
                            _ => SagaEventHub,
                            context => Task.FromResult(new SagaProduced(
                                context.Saga.RunId,
                                "data",
                                context.Exception.Message))))
                    .TransitionTo(Active));
        }

        public State Active { get; } = null!;
        public Event<DataFaultStart> Start { get; } = null!;
    }

    public sealed class PlainFaultSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public Guid RunId { get; set; }
    }

    public sealed class PlainFaultSagaMachine : ViciOneServiceBusStateMachine<PlainFaultSagaState>
    {
        public PlainFaultSagaMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(Start)
                    .Then(context => context.Saga.RunId = context.Message.RunId)
                    .TransitionTo(Active));
            WhenEnter(
                Active,
                entering => entering
                    .Then(_ => throw new ExpectedSagaFailure("plain-failure"))
                    .Catch<ExpectedSagaFailure>(caught => caught
                        .Produce(
                            _ => SagaEventHub,
                            context => Task.FromResult(new SagaProduced(
                                context.Saga.RunId,
                                "plain",
                                context.Exception.Message)))));
        }

        public State Active { get; } = null!;
        public Event<PlainFaultStart> Start { get; } = null!;
    }

    public sealed class ExpectedSagaFailure(string message) : Exception(message);

    private sealed class SagaDeliveryState(Guid runId, int expected)
    {
        private readonly ConcurrentQueue<ConsumeContext<SagaProduced>> _deliveries = [];

        public Guid RunId { get; } = runId;
        public TaskCompletionSource<ConsumeContext<SagaProduced>[]> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(ConsumeContext<SagaProduced> context)
        {
            if (context.Message.RunId != RunId)
                return;
            _deliveries.Enqueue(context);
            if (_deliveries.Count == expected)
                Completed.TrySetResult(_deliveries.ToArray());
        }
    }

    private sealed class SagaProducedConsumer(SagaDeliveryState state) : IConsumer<SagaProduced>
    {
        public Task Consume(ConsumeContext<SagaProduced> context)
        {
            state.Record(context);
            return Task.CompletedTask;
        }
    }

    public sealed record LifecycleMessage(Guid RunId, int Index);
    private sealed record LifecycleDelivery(int Index, string PartitionId, long Sequence);
    private sealed record LifecycleCallback(string PartitionId, int Generation, long Sequence, ProcessingStoppedReason? Reason);

    private sealed record LifecycleSnapshot(
        int InitializedCount,
        int ClosedCount,
        string[] FirstInitializedPartitions,
        string[] SecondInitializedPartitions,
        string[] FirstClosedPartitions,
        ProcessingStoppedReason[] FirstCloseReasons,
        IReadOnlyDictionary<string, long> FirstInitializeSequence,
        IReadOnlyDictionary<string, long> FirstCloseSequence,
        LifecycleDelivery[] Deliveries);

    private sealed class LifecycleState(Guid runId, int expectedDeliveries)
    {
        private readonly ConcurrentQueue<LifecycleCallback> _initialized = [];
        private readonly ConcurrentQueue<LifecycleCallback> _closed = [];
        private readonly ConcurrentQueue<LifecycleDelivery> _deliveries = [];
        private int _closedCount;
        private int _initializedCount;
        private long _sequence;

        public Guid RunId { get; } = runId;
        public TaskCompletionSource FirstInitialized { get; } = NewSignal();
        public TaskCompletionSource FirstClosed { get; } = NewSignal();
        public TaskCompletionSource SecondInitialized { get; } = NewSignal();
        public TaskCompletionSource SecondClosed { get; } = NewSignal();
        public TaskCompletionSource FirstDelivery { get; } = NewSignal();
        public TaskCompletionSource DeliveriesCompleted { get; } = NewSignal();

        public Task OnInitializing(PartitionInitializingEventArgs args)
        {
            args.DefaultStartingPosition = EventPosition.Earliest;
            int count = Interlocked.Increment(ref _initializedCount);
            int generation = ((count - 1) / 4) + 1;
            _initialized.Enqueue(new LifecycleCallback(args.PartitionId, generation, NextSequence(), null));
            if (count == 4)
                FirstInitialized.TrySetResult();
            else if (count == 8)
                SecondInitialized.TrySetResult();
            return Task.CompletedTask;
        }

        public Task OnClosing(PartitionClosingEventArgs args)
        {
            int count = Interlocked.Increment(ref _closedCount);
            int generation = ((count - 1) / 4) + 1;
            _closed.Enqueue(new LifecycleCallback(args.PartitionId, generation, NextSequence(), args.Reason));
            if (count == 4)
                FirstClosed.TrySetResult();
            else if (count == 8)
                SecondClosed.TrySetResult();
            return Task.CompletedTask;
        }

        public void Record(ConsumeContext<LifecycleMessage> context, EventHubConsumeContext payload)
        {
            if (context.Message.RunId != RunId)
                return;
            _deliveries.Enqueue(new LifecycleDelivery(context.Message.Index, payload.PartitionId, NextSequence()));
            if (_deliveries.Count == 1)
                FirstDelivery.TrySetResult();
            if (_deliveries.Count == expectedDeliveries)
                DeliveriesCompleted.TrySetResult();
        }

        public LifecycleSnapshot Snapshot()
        {
            LifecycleCallback[] initialized = _initialized.ToArray();
            LifecycleCallback[] closed = _closed.ToArray();
            return new LifecycleSnapshot(
                initialized.Length,
                closed.Length,
                initialized.Where(callback => callback.Generation == 1).Select(callback => callback.PartitionId).Order().ToArray(),
                initialized.Where(callback => callback.Generation == 2).Select(callback => callback.PartitionId).Order().ToArray(),
                closed.Where(callback => callback.Generation == 1).Select(callback => callback.PartitionId).Order().ToArray(),
                closed.Where(callback => callback.Generation == 1).Select(callback => callback.Reason!.Value).ToArray(),
                initialized.Where(callback => callback.Generation == 1).ToDictionary(callback => callback.PartitionId, callback => callback.Sequence),
                closed.Where(callback => callback.Generation == 1).ToDictionary(callback => callback.PartitionId, callback => callback.Sequence),
                _deliveries.ToArray());
        }

        private long NextSequence() => Interlocked.Increment(ref _sequence);
    }

    private sealed class LifecycleConsumer(LifecycleState state) : IConsumer<LifecycleMessage>
    {
        public Task Consume(ConsumeContext<LifecycleMessage> context)
        {
            Assert.True(context.TryGetPayload(out EventHubConsumeContext? payload));
            Assert.NotNull(payload);
            state.Record(context, payload);
            return Task.CompletedTask;
        }
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
