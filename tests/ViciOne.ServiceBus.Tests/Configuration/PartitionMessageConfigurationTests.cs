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
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
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

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record PartitionedMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

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

    private sealed record UnpartitionedMessage(Guid OtherId);

    private sealed class UnpartitionedConsumer : IConsumer<UnpartitionedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<UnpartitionedMessage> context) => Task.CompletedTask;
    }
}
