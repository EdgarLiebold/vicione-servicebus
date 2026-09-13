using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class PartitionMessageConfigurationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-PARTITION-CONVENTION", "explicit-and-convention-deliver-every-message")]
    public async Task MessagePartitioner_DeliversEveryMessageWithExplicitAndConventionBasedKeysAsync(bool explicitKey)
    {
        const int messageCount = 16;
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var consumer = new PartitionedConsumer(messageCount);
        using var harness = new InMemoryTestHarness($"partition-convention-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            if (explicitKey)
            {
                endpoint.Instance(consumer, instance =>
                    instance.Message<PartitionedMessage>(message =>
                        message.UsePartitioner(4, context => context.Message.CorrelationId)));
            }
            else
            {
                endpoint.UseMessagePartitioner(4);
                endpoint.Instance(consumer);
            }
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid[] correlationIds = Enumerable.Range(0, messageCount)
                .Select(_ => NewId.NextGuid())
                .ToArray();

            await Task.WhenAll(correlationIds.Select(correlationId =>
                    harness.InputQueueSendEndpoint.SendAsync(new PartitionedMessage(correlationId), cancellationToken)))
                .WaitAsync(timeout, cancellationToken);
            await consumer.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(messageCount, consumer.ConsumedCorrelationIds.Length);
            Assert.Equal(correlationIds.Order(), consumer.ConsumedCorrelationIds.Order());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-PARTITION-CONVENTION", "same-correlation-is-serialized-other-partition-overlaps")]
    public async Task MessagePartitioner_SerializesEqualCorrelationsWhileAnotherPartitionCanOverlapAsync(bool explicitKey)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var hashGenerator = new Murmur3PartitionHashGenerator();
        Guid serializedKey = FindKeyForPartition(hashGenerator, partitionCount: 4, partitionIndex: 0);
        Guid overlappingKey = FindKeyForPartition(hashGenerator, partitionCount: 4, partitionIndex: 1);
        var consumer = new OrderingProbeConsumer(serializedKey, overlappingKey, timeout, cancellationToken);
        using var harness = new InMemoryTestHarness($"partition-order-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            if (explicitKey)
            {
                endpoint.Instance(consumer, instance =>
                    instance.Message<PartitionedMessage>(message =>
                        message.UsePartitioner(4, context => context.Message.CorrelationId)));
            }
            else
            {
                endpoint.UseMessagePartitioner(4);
                endpoint.Instance(consumer);
            }
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Task first = harness.InputQueueSendEndpoint.SendAsync(
                new PartitionedMessage(serializedKey, 1),
                cancellationToken);
            await consumer.FirstSerializedEntered.Task.WaitAsync(timeout, cancellationToken);
            Task second = harness.InputQueueSendEndpoint.SendAsync(
                new PartitionedMessage(serializedKey, 2),
                cancellationToken);
            Task other = harness.InputQueueSendEndpoint.SendAsync(
                new PartitionedMessage(overlappingKey, 3),
                cancellationToken);

            try
            {
                await consumer.OtherPartitionEntered.Task.WaitAsync(timeout, cancellationToken);
                Assert.False(consumer.SecondSerializedEntered.Task.IsCompleted);
            }
            finally
            {
                consumer.ReleaseFirst.TrySetResult();
            }

            await Task.WhenAll(first, second, other, consumer.SecondSerializedEntered.Task)
                .WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            consumer.ReleaseFirst.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PARTITION-CONVENTION", "typed-guid-key-uses-the-guid-binary-contract")]
    public async Task TypedMessagePartitioner_UsesTheExactGuidBinaryRepresentationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var key = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        var hashGenerator = new CaptureHashGenerator();
        await using var partitioner = new PipePartitioner(2, hashGenerator);
        var consumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"typed-guid-partition-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.UsePartitioner<PartitionedMessage>(partitioner, context => context.Message.CorrelationId);
            endpoint.Handler<PartitionedMessage>(_ =>
            {
                consumed.TrySetResult();
                return Task.CompletedTask;
            });
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new PartitionedMessage(key), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await consumed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(key.ToByteArray(), hashGenerator.CapturedKey);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PARTITION-CONVENTION", "missing-runtime-correlation-is-rejected-before-consumer")]
    public async Task MessagePartitioner_RejectsAMissingRuntimeCorrelationBeforeInvokingTheConsumerAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var consumerInvocationCount = 0;
        var consumerInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"missing-runtime-partition-key-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.UseMessagePartitioner(4);
            endpoint.Handler<PartitionedMessage>(_ =>
            {
                Interlocked.Increment(ref consumerInvocationCount);
                consumerInvoked.TrySetResult();
                return Task.CompletedTask;
            });
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var observer = new ReceiveFaultProbe();
            using ConnectHandle observerHandle = harness.Bus.ConnectReceiveObserver(observer);

            await harness.InputQueueSendEndpoint.SendAsync(
                    new PartitionedMessage(Guid.Empty),
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Task firstOutcome = await Task.WhenAny(observer.Fault.Task, consumerInvoked.Task)
                .WaitAsync(timeout, cancellationToken);

            Assert.Same(observer.Fault.Task, firstOutcome);
            Exception exception = await observer.Fault.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(0, Volatile.Read(ref consumerInvocationCount));
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("does not contain a correlation identifier", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-PARTITION-CONVENTION", "partitioners-survive-bus-stop-and-restart")]
    public async Task MessagePartitioner_RemainsUsableAcrossBusStopAndRestartAsync(bool sharedPartitioner)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string queueName = $"partition-restart-{NewId.NextGuid():N}";
        var consumer = new RestartProbeConsumer();
        PipePartitioner? partitioner = sharedPartitioner ? new PipePartitioner(4) : null;
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ReceiveEndpoint(queueName, endpoint =>
        {
            if (partitioner == null)
                endpoint.UseMessagePartitioner(4);
            else
                endpoint.UsePartitioner<PartitionedMessage>(partitioner, context => context.Message.CorrelationId);

            endpoint.Instance(consumer);
        }));

        try
        {
            for (var generation = 1; generation <= 2; generation++)
            {
                await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
                try
                {
                    ISendEndpoint endpoint = await bus.GetSendEndpointAsync(
                            new Uri($"queue:{queueName}"),
                            cancellationToken)
                        .WaitAsync(timeout, cancellationToken);
                    await endpoint.SendAsync(
                            new PartitionedMessage(NewId.NextGuid(), generation),
                            cancellationToken)
                        .WaitAsync(timeout, cancellationToken);
                    await consumer.WaitForGenerationAsync(generation).WaitAsync(timeout, cancellationToken);
                }
                finally
                {
                    await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
                }
            }

            Assert.Equal([1, 2], consumer.Generations);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            if (partitioner != null)
                await partitioner.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PARTITION-CONVENTION", "missing-correlation-id-is-actionable")]
    public void MessagePartitioner_RejectsAMessageWithoutACorrelationConvention()
    {
        ConfigurationException actual = Assert.Throws<ConfigurationException>(() =>
            Bus.Factory.CreateUsingInMemory(bus => bus.ReceiveEndpoint("missing-partition-convention", endpoint =>
            {
                endpoint.UseMessagePartitioner(4);
                endpoint.Consumer<UnpartitionedConsumer>();
            })));

        Assert.Contains("A CorrelationId convention for this message type was not found", actual.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(UnpartitionedMessage), actual.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PARTITION-CONVENTION", "public-entry-points-reject-invalid-arguments")]
    public async Task MessagePartitionerEntryPoints_RejectEveryInvalidRequiredArgumentAsync()
    {
        ArgumentOutOfRangeException count = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Bus.Factory.CreateUsingInMemory(bus => bus.ReceiveEndpoint(
                $"invalid-message-partition-count-{NewId.NextGuid():N}",
                endpoint => endpoint.UseMessagePartitioner(0))));
        await using var partitioner = new PipePartitioner(2);
        ArgumentNullException missingPartitioner = Assert.Throws<ArgumentNullException>(() =>
            Bus.Factory.CreateUsingInMemory(bus => bus.ReceiveEndpoint(
                $"missing-message-partitioner-{NewId.NextGuid():N}",
                endpoint => endpoint.UsePartitioner<PartitionedMessage>(
                    null!,
                    context => context.Message.CorrelationId))));
        ArgumentNullException missingKeyProvider = Assert.Throws<ArgumentNullException>(() =>
            Bus.Factory.CreateUsingInMemory(bus => bus.ReceiveEndpoint(
                $"missing-message-partition-key-{NewId.NextGuid():N}",
                endpoint => endpoint.UsePartitioner<PartitionedMessage>(
                    partitioner,
                    null!))));

        Assert.Equal("partitionCount", count.ParamName);
        Assert.Equal("partitioner", missingPartitioner.ParamName);
        Assert.Equal("keyProvider", missingKeyProvider.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PARTITION-CONVENTION", "batch-with-correlated-elements-has-no-singular-partition-key")]
    public async Task MessagePartitioner_AllowsBatchesWhoseElementsHaveIndependentCorrelationIdsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var consumed = new TaskCompletionSource<IMessageBatch<PartitionedMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"partitioned-batch-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.UseMessagePartitioner(4);
            endpoint.Batch<PartitionedMessage>(batch =>
            {
                batch.MessageLimit = 2;
                batch.TimeLimit = timeout;
                batch.Consumer(() => new PartitionedBatchConsumer(consumed));
            });
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            PartitionedMessage[] messages =
            [
                new PartitionedMessage(NewId.NextGuid()),
                new PartitionedMessage(NewId.NextGuid()),
            ];
            await Task.WhenAll(messages.Select(message =>
                    harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken)))
                .WaitAsync(timeout, cancellationToken);
            IMessageBatch<PartitionedMessage> batch = await consumed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(2, batch.Count);
            Assert.Equal(messages.Select(message => message.CorrelationId).Order(),
                batch.Select(context => context.Message.CorrelationId).Order());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static Guid FindKeyForPartition(
        IPartitionHashGenerator hashGenerator,
        int partitionCount,
        int partitionIndex)
    {
        for (var value = 1; value < int.MaxValue; value++)
        {
            var key = new Guid(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            if (hashGenerator.ComputeHash(key.ToByteArray()) % (uint)partitionCount == partitionIndex)
                return key;
        }

        throw new InvalidOperationException($"No key mapped to partition {partitionIndex}.");
    }

    private sealed record PartitionedMessage(Guid CorrelationId, int Sequence = 0) : ICorrelatedBy<Guid>;

    private sealed class PartitionedConsumer(int expectedCount) : IConsumer<PartitionedMessage>
    {
        private readonly List<Guid> _correlationIds = [];
        private readonly object _lock = new();

        public TaskCompletionSource<bool> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Guid[] ConsumedCorrelationIds
        {
            get
            {
                lock (_lock)
                    return _correlationIds.ToArray();
            }
        }

        public Task ConsumeAsync(ConsumeContext<PartitionedMessage> context)
        {
            lock (_lock)
            {
                _correlationIds.Add(context.Message.CorrelationId);
                if (_correlationIds.Count == expectedCount)
                    Completed.TrySetResult(true);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class PartitionedBatchConsumer(TaskCompletionSource<IMessageBatch<PartitionedMessage>> consumed) :
        IConsumer<IMessageBatch<PartitionedMessage>>
    {
        public Task ConsumeAsync(ConsumeContext<IMessageBatch<PartitionedMessage>> context)
        {
            consumed.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class OrderingProbeConsumer(
        Guid serializedKey,
        Guid overlappingKey,
        TimeSpan timeout,
        CancellationToken cancellationToken) : IConsumer<PartitionedMessage>
    {
        public TaskCompletionSource FirstSerializedEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondSerializedEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource OtherPartitionEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirst { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ConsumeAsync(ConsumeContext<PartitionedMessage> context)
        {
            if (context.Message.CorrelationId == overlappingKey)
            {
                OtherPartitionEntered.TrySetResult();
                return;
            }

            Assert.Equal(serializedKey, context.Message.CorrelationId);
            if (context.Message.Sequence == 1)
            {
                FirstSerializedEntered.TrySetResult();
                await ReleaseFirst.Task.WaitAsync(timeout, cancellationToken);
            }
            else
                SecondSerializedEntered.TrySetResult();
        }
    }

    private sealed class RestartProbeConsumer : IConsumer<PartitionedMessage>
    {
        private readonly List<int> _generations = [];
        private readonly Dictionary<int, TaskCompletionSource> _signals = [];
        private readonly object _lock = new();

        public int[] Generations
        {
            get
            {
                lock (_lock)
                    return _generations.ToArray();
            }
        }

        public Task WaitForGenerationAsync(int generation)
        {
            lock (_lock)
            {
                if (_generations.Contains(generation))
                    return Task.CompletedTask;

                if (!_signals.TryGetValue(generation, out TaskCompletionSource? signal))
                {
                    signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _signals.Add(generation, signal);
                }

                return signal.Task;
            }
        }

        public Task ConsumeAsync(ConsumeContext<PartitionedMessage> context)
        {
            TaskCompletionSource? signal;
            lock (_lock)
            {
                _generations.Add(context.Message.Sequence);
                _signals.TryGetValue(context.Message.Sequence, out signal);
            }

            signal?.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class CaptureHashGenerator : IPartitionHashGenerator
    {
        public byte[]? CapturedKey { get; private set; }

        public uint ComputeHash(ReadOnlySpan<byte> partitionKey)
        {
            CapturedKey = partitionKey.ToArray();
            return 0;
        }
    }

    private sealed class ReceiveFaultProbe : IReceiveObserver
    {
        public TaskCompletionSource<Exception> Fault { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(
            ConsumeContext<T> context,
            TimeSpan duration,
            string consumerType,
            Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            Fault.TrySetResult(exception);
            return Task.CompletedTask;
        }
    }

    private sealed record UnpartitionedMessage(Guid OtherId);

    private sealed class UnpartitionedConsumer : IConsumer<UnpartitionedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<UnpartitionedMessage> context) => Task.CompletedTask;
    }
}
