using ViciOne.ServiceBus.Batching.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Batching;

public sealed class BatchConsumerFactoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONSUMER-FACTORY", "operation-and-probe-boundaries")]
    public async Task Operations_RejectEveryMissingRequiredInputAsync()
    {
        BatchOptions options = CreateOptions();
        await using var executor = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<FactoryMessage>(
            new BatchRuntimeSettings(options),
            executor,
            dispatcher,
            new RejectBatchPipe(),
            TimeProvider.System);
        var collector = new RecordingCollector(consumer);
        var factory = new BatchConsumerFactory<FactoryMessage>(options, collector);
        ConsumeContext<FactoryMessage> context = InMemoryOutboxTestContextFactory.Create(
            new FactoryMessage("value"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            "context",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                factory.SendAsync<FactoryMessage>(null!, new RecordingConsumerPipe()))).ParamName);
        Assert.Equal(
            "next",
            (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                factory.SendAsync(context, null!))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => factory.Probe(null!)).ParamName);

        await consumer.ForceCompleteAsync(TestContext.Current.CancellationToken);
        await factory.DisposeAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-BATCH-CONSUMER-FACTORY", "completed-consumer-cleanup-after-next-outcome")]
    public async Task SendAsync_RemovesACompletedConsumerAfterEveryNextPipeOutcomeAsync(bool nextFails)
    {
        BatchOptions options = CreateOptions();
        await using var executor = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<FactoryMessage>(
            new BatchRuntimeSettings(options),
            executor,
            dispatcher,
            new RejectBatchPipe(),
            TimeProvider.System);
        await consumer.ForceCompleteAsync(TestContext.Current.CancellationToken);
        var collector = new RecordingCollector(consumer);
        var nextFailure = new InvalidOperationException("next failed");
        var next = new RecordingConsumerPipe(nextFails ? nextFailure : null);
        var factory = new BatchConsumerFactory<FactoryMessage>(options, collector);
        ConsumeContext<FactoryMessage> context = InMemoryOutboxTestContextFactory.Create(
            new FactoryMessage("value"),
            TestContext.Current.CancellationToken);

        if (nextFails)
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                factory.SendAsync(context, next));
            Assert.Same(nextFailure, exception);
        }
        else
            await factory.SendAsync(context, next);

        Assert.Equal(1, collector.CollectCount);
        Assert.Equal(1, collector.CompleteCount);
        Assert.Same(consumer, next.ObservedConsumer);
        Assert.Same(context.Message, next.ObservedMessage);

        await factory.DisposeAsync();
        Assert.Equal(1, collector.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONSUMER-FACTORY", "active-consumer-retained")]
    public async Task SendAsync_DoesNotRemoveAConsumerThatStillAcceptsMessagesAsync()
    {
        BatchOptions options = CreateOptions();
        await using var executor = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<FactoryMessage>(
            new BatchRuntimeSettings(options),
            executor,
            dispatcher,
            new RejectBatchPipe(),
            TimeProvider.System);
        var collector = new RecordingCollector(consumer);
        var factory = new BatchConsumerFactory<FactoryMessage>(options, collector);
        ConsumeContext<FactoryMessage> context = InMemoryOutboxTestContextFactory.Create(
            new FactoryMessage("value"),
            TestContext.Current.CancellationToken);

        await factory.SendAsync(context, new RecordingConsumerPipe());

        Assert.Equal(1, collector.CollectCount);
        Assert.Equal(0, collector.CompleteCount);

        await consumer.ForceCompleteAsync(TestContext.Current.CancellationToken);
        await factory.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONSUMER-FACTORY", "effective-settings-and-collector-probe")]
    public async Task Probe_ReportsTheCapturedSettingsAndDelegatesToTheCollectorAsync()
    {
        var options = new BatchOptions
        {
            MessageLimit = 17,
            ConcurrencyLimit = 4,
            TimeLimit = TimeSpan.FromMinutes(3),
            TimeLimitStart = BatchTimeLimitStart.FromLast,
        };
        await using var executor = new TaskExecutor();
        await using var dispatcher = new TaskExecutor();
        var consumer = new BatchConsumer<FactoryMessage>(
            new BatchRuntimeSettings(options),
            executor,
            dispatcher,
            new RejectBatchPipe(),
            TimeProvider.System);
        var collector = new RecordingCollector(consumer);
        var factory = new BatchConsumerFactory<FactoryMessage>(options, collector);

        IProbeResult result = factory.GetProbeResult(TestContext.Current.CancellationToken);

        IReadOnlyDictionary<string, object> scope = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            Assert.Contains("consumerFactory", result.Results));
        Assert.Equal(TimeSpan.FromMinutes(3), Assert.Contains("timeLimit", scope));
        Assert.Equal(BatchTimeLimitStart.FromLast, Assert.Contains("timeLimitStart", scope));
        Assert.Equal(17, Assert.Contains("messageLimit", scope));
        Assert.Equal(4, Assert.Contains("concurrencyLimit", scope));
        Assert.Equal(true, Assert.Contains("collectorVisited", scope));
        Assert.Equal(1, collector.ProbeCount);

        await consumer.ForceCompleteAsync(TestContext.Current.CancellationToken);
        await factory.DisposeAsync();
    }

    private static BatchOptions CreateOptions() => new()
    {
        MessageLimit = 2,
        ConcurrencyLimit = 1,
        TimeLimit = TimeSpan.FromMinutes(1),
        TimeLimitStart = BatchTimeLimitStart.FromFirst,
    };

    private sealed class RecordingCollector(BatchConsumer<FactoryMessage> consumer) : IBatchCollector<FactoryMessage>
    {
        public int CollectCount { get; private set; }

        public int CompleteCount { get; private set; }

        public int DisposeCount { get; private set; }

        public int ProbeCount { get; private set; }

        public Task<BatchConsumer<FactoryMessage>> CollectAsync(
            ConsumeContext<FactoryMessage> context,
            CancellationToken cancellationToken = default)
        {
            CollectCount++;
            return Task.FromResult(consumer);
        }

        public Task CompleteAsync(
            BatchConsumer<FactoryMessage> completedConsumer,
            CancellationToken cancellationToken = default)
        {
            Assert.Same(consumer, completedConsumer);
            CompleteCount++;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            ProbeCount++;
            context.Add("collectorVisited", true);
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingConsumerPipe(Exception? failure = null) :
        IPipe<ConsumerConsumeContext<BatchConsumer<FactoryMessage>, FactoryMessage>>
    {
        public BatchConsumer<FactoryMessage>? ObservedConsumer { get; private set; }

        public FactoryMessage? ObservedMessage { get; private set; }

        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumerConsumeContext<BatchConsumer<FactoryMessage>, FactoryMessage> context)
        {
            ObservedConsumer = context.Consumer;
            ObservedMessage = context.Message;
            return failure == null ? Task.CompletedTask : Task.FromException(failure);
        }
    }

    private sealed class RejectBatchPipe : IPipe<ConsumeContext<IMessageBatch<FactoryMessage>>>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<IMessageBatch<FactoryMessage>> context) =>
            throw new InvalidOperationException("An empty batch must not be delivered.");
    }

    public sealed record FactoryMessage(string Value);
}
