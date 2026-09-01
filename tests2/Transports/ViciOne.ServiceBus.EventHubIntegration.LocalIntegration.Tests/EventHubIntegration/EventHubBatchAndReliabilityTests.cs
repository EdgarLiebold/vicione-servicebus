namespace ViciOne.ServiceBus.EventHubIntegration.LocalIntegration.Tests.EventHubIntegration;

using System.Collections.Concurrent;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.EventHubIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class EventHubBatchAndReliabilityTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0144", "hundred-item-single-partition-batch-preserves-cardinality-and-order")]
    public async Task BatchConsumer_ReceivesExactlyOneHundredOrderedItemsFromOnePartitionKey()
    {
        const string eventHubName = "batch-eh";
        const string partitionKey = "ordered-batch";
        var state = new BatchState(NewId.NextGuid());
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("batch-receive");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<OrderedBatchConsumer>(consumer => consumer.Options<BatchOptions>(options => options
                        .SetMessageLimit(100)
                        .SetTimeLimit(TimeSpan.FromMinutes(1))
                        .GroupBy<IBatchMessage, string>(context => context.PartitionKey() ?? string.Empty)));
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("batch");
                            endpoint.ConcurrentDeliveryLimit = 100;
                            endpoint.CheckpointMessageCount = 100;
                            endpoint.CheckpointInterval = TimeSpan.FromMinutes(1);
                            endpoint.ConfigureConsumer<OrderedBatchConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducer(eventHubName).WaitAsync(fixture.OperationTimeout, cancellationToken);
            BatchMessage[] messages = Enumerable.Range(0, 100)
                .Select(index => new BatchMessage(state.RunId, index))
                .ToArray();

            await producer.Produce<IBatchMessage>(
                    messages,
                    Pipe.Execute<SendContext>(context => context.SetPartitionKey(partitionKey)),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<Batch<IBatchMessage>> actual = await state.Received.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Assert.Equal(100, actual.Message.Length);
            Assert.Equal(Enumerable.Range(0, 100), actual.Message.Select(item => item.Message.Index));
            Assert.All(actual.Message, item =>
            {
                Assert.Equal(state.RunId, item.Message.RunId);
                Assert.Equal(partitionKey, item.PartitionKey());
            });
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0146", "faulted-first-event-does-not-stop-same-partition-and-fault-is-observed")]
    public async Task ConsumerFault_IsObservedAndTheNextSamePartitionEventStillCompletes()
    {
        const string eventHubName = "retry-eh";
        var state = new FaultContinuationState(NewId.NextGuid());
        var observer = new FaultConsumeObserver(state);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("fault-continue");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory((_, bus) => bus.ConnectConsumeObserver(observer));
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<FaultContinuationConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("fault");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<FaultContinuationConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducer(eventHubName).WaitAsync(fixture.OperationTimeout, cancellationToken);
            FaultContinuationMessage[] messages =
            [
                new(state.RunId, 0),
                new(state.RunId, 1),
            ];

            await producer.Produce<IFaultContinuationMessage>(
                    messages,
                    Pipe.Execute<SendContext>(context => context.SetPartitionKey($"fault-{state.RunId:N}")),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Exception fault = await state.Faulted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            int completedIndex = await state.Completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Assert.Same(state.ExpectedFailure, fault);
            Assert.Equal(1, completedIndex);
            Assert.Equal([0, 1], state.Attempts);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0147", "immediate-three-retry-budget-exposes-exact-attempt-and-count-sequences")]
    public async Task ConsumerRetry_ExecutesOneOriginalAndThreeImmediateRetriesWithExactMetadata()
    {
        const string eventHubName = "retry-eh";
        var state = new RetryState(NewId.NextGuid());
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("retry");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<RetryConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("retry");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.UseMessageRetry(retry => retry.Immediate(3));
                            endpoint.ConfigureConsumer<RetryConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducer(eventHubName).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await producer.Produce<IRetryMessage>(new RetryMessage(state.RunId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await state.Completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Assert.Equal([0, 1, 2, 3], state.Attempts);
            Assert.Equal([0, 0, 1, 2], state.RetryCounts);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0148", "health-transitions-from-unhealthy-to-healthy-after-partition-ownership")]
    public async Task RiderHealth_IsUnhealthyBeforeStartAndHealthyAfterAllDeclaredPartitionsInitialize()
    {
        const string eventHubName = "lifecycle-eh";
        var lifecycle = new PartitionLifecycleState(expectedPartitions: 4);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("health");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory();
                configuration.AddRider(rider => rider.UsingEventHub((_, eventHubs) =>
                {
                    fixture.Configure(eventHubs);
                    eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                    {
                        endpoint.ContainerName = fixture.ContainerName("health");
                        endpoint.OnPartitionInitializing(lifecycle.OnInitializing);
                    });
                }));
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        HealthCheckService healthChecks = provider.GetRequiredService<HealthCheckService>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        HealthReport beforeStart = await healthChecks.CheckHealthAsync(cancellationToken);
        Assert.Equal(HealthStatus.Unhealthy, beforeStart.Status);

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            string[] partitions = await lifecycle.AllInitialized.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            HealthReport afterOwnership = await healthChecks.CheckHealthAsync(cancellationToken);

            Assert.Equal(["0", "1", "2", "3"], partitions.Order());
            Assert.Equal(HealthStatus.Healthy, afterOwnership.Status);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0149", "checkpoint-is-written-only-after-long-running-consumer-confirms")]
    public async Task Checkpoint_DoesNotAdvancePastAnInFlightEventAndAdvancesAfterConfirmation()
    {
        const string eventHubName = "checkpoint-gate-eh";
        var state = new GatedCheckpointState(NewId.NextGuid());
        var storageProbe = new CheckpointWriteProbe();
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("checkpoint");
        string containerName = fixture.ContainerName("custom");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<GatedCheckpointConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs, options => options.AddPolicy(storageProbe, HttpPipelinePosition.PerCall));
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = containerName;
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.CheckpointInterval = TimeSpan.FromHours(1);
                            endpoint.ConcurrentDeliveryLimit = 1;
                            endpoint.ConfigureConsumer<GatedCheckpointConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducer(eventHubName).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await producer.Produce<ICheckpointMessage>(new CheckpointMessage(state.RunId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await state.Entered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(0, storageProbe.CheckpointWriteCount);

            state.Release.TrySetResult();
            await state.Completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            string checkpointPath = await storageProbe.FirstCheckpointWrite.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Contains($"/{eventHubName}/{EventHubLocalFixture.ConsumerGroup}/checkpoint/", checkpointPath, StringComparison.Ordinal);
            Assert.Equal(1, storageProbe.CheckpointWriteCount);
        }
        finally
        {
            state.Release.TrySetResult();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0264", "custom-checkpoint-container-and-producer-options-reach-provider-clients")]
    public async Task CustomContainerAndProducerOptions_AreAppliedToTheRealProviderClients()
    {
        const string eventHubName = "config-eh";
        Guid marker = NewId.NextGuid();
        var received = NewSignal<Guid>();
        var producerOptions = NewSignal<EventHubProducerClientOptions>();
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("provider-config");
        string containerName = fixture.ContainerName("named-checkpoints");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(new MarkerDelivery(marker, received))
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<MarkerConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ConfigureProducerOptions(options =>
                        {
                            options.RetryOptions.MaximumRetries = 0;
                            producerOptions.TrySetResult(options);
                        });
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = containerName;
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<MarkerConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducer(eventHubName).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await producer.Produce<IMarkerMessage>(new MarkerMessage(marker), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(marker, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            EventHubProducerClientOptions applied = await producerOptions.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            BlobContainerClient container = fixture.GetContainer(containerName);
            Assert.True(await container.ExistsAsync(cancellationToken));
            var blobs = new List<string>();
            await foreach (BlobItem blob in container.GetBlobsAsync(cancellationToken: cancellationToken))
                blobs.Add(blob.Name);

            Assert.Equal(0, applied.RetryOptions.MaximumRetries);
            Assert.Contains(blobs, name => name.Contains($"/{eventHubName}/{EventHubLocalFixture.ConsumerGroup}/checkpoint/", StringComparison.Ordinal));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public interface IBatchMessage
    {
        Guid RunId { get; }
        int Index { get; }
    }

    public interface IFaultContinuationMessage
    {
        Guid RunId { get; }
        int Index { get; }
    }

    public interface IRetryMessage
    {
        Guid RunId { get; }
    }

    public interface ICheckpointMessage
    {
        Guid RunId { get; }
    }

    public interface IMarkerMessage
    {
        Guid Marker { get; }
    }

    private sealed record BatchMessage(Guid RunId, int Index) : IBatchMessage;
    private sealed record FaultContinuationMessage(Guid RunId, int Index) : IFaultContinuationMessage;
    private sealed record RetryMessage(Guid RunId) : IRetryMessage;
    private sealed record CheckpointMessage(Guid RunId) : ICheckpointMessage;
    private sealed record MarkerMessage(Guid Marker) : IMarkerMessage;

    private sealed class BatchState(Guid runId)
    {
        public Guid RunId { get; } = runId;
        public TaskCompletionSource<ConsumeContext<Batch<IBatchMessage>>> Received { get; } = NewSignal<ConsumeContext<Batch<IBatchMessage>>>();
    }

    private sealed class OrderedBatchConsumer(BatchState state) : IConsumer<Batch<IBatchMessage>>
    {
        public Task Consume(ConsumeContext<Batch<IBatchMessage>> context)
        {
            if (context.Message.Length == 100 && context.Message.All(item => item.Message.RunId == state.RunId))
                state.Received.TrySetResult(context);
            return Task.CompletedTask;
        }
    }

    private sealed class FaultContinuationState(Guid runId)
    {
        private readonly ConcurrentQueue<int> _attempts = new();

        public Guid RunId { get; } = runId;
        public InvalidOperationException ExpectedFailure { get; } = new("expected first-event failure");
        public TaskCompletionSource<Exception> Faulted { get; } = NewSignal<Exception>();
        public TaskCompletionSource<int> Completed { get; } = NewSignal<int>();
        public int[] Attempts => _attempts.ToArray();
        public void Record(int index) => _attempts.Enqueue(index);
    }

    private sealed class FaultContinuationConsumer(FaultContinuationState state) : IConsumer<IFaultContinuationMessage>
    {
        public Task Consume(ConsumeContext<IFaultContinuationMessage> context)
        {
            if (context.Message.RunId != state.RunId)
                return Task.CompletedTask;

            state.Record(context.Message.Index);
            if (context.Message.Index == 0)
                return Task.FromException(state.ExpectedFailure);

            state.Completed.TrySetResult(context.Message.Index);
            return Task.CompletedTask;
        }
    }

    private sealed class FaultConsumeObserver(FaultContinuationState state) : IConsumeObserver
    {
        public Task PreConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

        public Task PostConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

        public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            if (context.Message is IFaultContinuationMessage message && message.RunId == state.RunId)
                state.Faulted.TrySetResult(exception);
            return Task.CompletedTask;
        }
    }

    private sealed class RetryState(Guid runId)
    {
        private readonly object _lock = new();
        private readonly List<int> _attempts = [];
        private readonly List<int> _retryCounts = [];

        public Guid RunId { get; } = runId;
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int[] Attempts { get { lock (_lock) return _attempts.ToArray(); } }
        public int[] RetryCounts { get { lock (_lock) return _retryCounts.ToArray(); } }

        public int Record(int attempt, int retryCount)
        {
            lock (_lock)
            {
                _attempts.Add(attempt);
                _retryCounts.Add(retryCount);
                return _attempts.Count;
            }
        }
    }

    private sealed class RetryConsumer(RetryState state) : IConsumer<IRetryMessage>
    {
        public Task Consume(ConsumeContext<IRetryMessage> context)
        {
            if (context.Message.RunId != state.RunId)
                return Task.CompletedTask;

            int attempt = context.GetRetryAttempt();
            state.Record(attempt, context.GetRetryCount());
            if (attempt < 3)
                return Task.FromException(new InvalidOperationException($"expected retry {attempt}"));

            state.Completed.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class PartitionLifecycleState(int expectedPartitions)
    {
        private readonly ConcurrentDictionary<string, byte> _initialized = new(StringComparer.Ordinal);
        public TaskCompletionSource<string[]> AllInitialized { get; } = NewSignal<string[]>();

        public Task OnInitializing(Azure.Messaging.EventHubs.Processor.PartitionInitializingEventArgs args)
        {
            _initialized.TryAdd(args.PartitionId, 0);
            if (_initialized.Count == expectedPartitions)
                AllInitialized.TrySetResult(_initialized.Keys.Order().ToArray());
            return Task.CompletedTask;
        }
    }

    private sealed class GatedCheckpointState(Guid runId)
    {
        public Guid RunId { get; } = runId;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class GatedCheckpointConsumer(GatedCheckpointState state) : IConsumer<ICheckpointMessage>
    {
        public async Task Consume(ConsumeContext<ICheckpointMessage> context)
        {
            if (context.Message.RunId != state.RunId)
                return;

            state.Entered.TrySetResult();
            await state.Release.Task.WaitAsync(context.CancellationToken);
            state.Completed.TrySetResult();
        }
    }

    private sealed class CheckpointWriteProbe : HttpPipelinePolicy
    {
        private int _checkpointWriteCount;

        public int CheckpointWriteCount => Volatile.Read(ref _checkpointWriteCount);
        public TaskCompletionSource<string> FirstCheckpointWrite { get; } = NewSignal<string>();

        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline) =>
            throw new InvalidOperationException("The checkpoint probe supports asynchronous requests only.");

        public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            string path = message.Request.Uri.ToUri().AbsolutePath;
            if (message.Request.Method == RequestMethod.Put && path.Contains("/checkpoint/", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _checkpointWriteCount);
                FirstCheckpointWrite.TrySetResult(path);
            }

            await ProcessNextAsync(message, pipeline).ConfigureAwait(false);
        }
    }

    private sealed record MarkerDelivery(Guid Marker, TaskCompletionSource<Guid> Received);

    private sealed class MarkerConsumer(MarkerDelivery delivery) : IConsumer<IMarkerMessage>
    {
        public Task Consume(ConsumeContext<IMarkerMessage> context)
        {
            if (context.Message.Marker == delivery.Marker)
                delivery.Received.TrySetResult(context.Message.Marker);
            return Task.CompletedTask;
        }
    }
}
