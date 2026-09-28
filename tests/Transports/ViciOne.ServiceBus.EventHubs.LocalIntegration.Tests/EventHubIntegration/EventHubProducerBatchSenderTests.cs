using System.Diagnostics;
using System.Net.Mime;
using System.Runtime.Serialization;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubProducerBatchSenderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "empty-input-rejected-before-provider-use")]
    public async Task EmptyInput_IsRejectedBeforeProviderUseAsync()
    {
        var producer = new RecordingProducerContext();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            EventHubProducerBatchSender.SendAsync<TestMessage>(
                producer,
                [],
                TestContext.Current.CancellationToken,
                producer.DisposeBatch));

        Assert.Equal("sendContexts", exception.ParamName);
        Assert.Empty(producer.CreatedRoutes);
        Assert.Empty(producer.SentBatchSizes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "mixed-routes-preserved-in-input-order")]
    public async Task MixedRoutes_ArePreservedInInputOrderAsync()
    {
        var producer = new RecordingProducerContext();
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionId: "0", cancellationToken: TestContext.Current.CancellationToken),
            CreateContext(2, partitionId: "1", cancellationToken: TestContext.Current.CancellationToken),
            CreateContext(3, partitionKey: "customer-3", cancellationToken: TestContext.Current.CancellationToken)
        ];

        await EventHubProducerBatchSender.SendAsync(
            producer,
            contexts,
            TestContext.Current.CancellationToken,
            producer.DisposeBatch);

        Assert.Equal(
            [new Route("0", null), new Route("1", null), new Route(null, "customer-3")],
            producer.CreatedRoutes);
        Assert.Equal([1, 1, 1], producer.SentBatchSizes);
        producer.AssertEveryBatchDisposed();
    }

    [Theory]
    [InlineData("0", null, "PartitionId", "PartitionKey")]
    [InlineData(null, "customer-7", "PartitionKey", "PartitionId")]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "uniform-route-tags-only-its-own-partition-dimension")]
    public async Task UniformRoute_TagsOnlyTheActualPartitionDimensionAsync(
        string? partitionId,
        string? partitionKey,
        string expectedTag,
        string otherTag)
    {
        using var activity = new Activity("eventhub-uniform-route") { IsAllDataRequested = true };
        activity.Start();
        var producer = new RecordingProducerContext();
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionId, partitionKey, TestContext.Current.CancellationToken),
            CreateContext(2, partitionId, partitionKey, TestContext.Current.CancellationToken)
        ];

        await EventHubProducerBatchSender.SendAsync(
            producer,
            contexts,
            TestContext.Current.CancellationToken,
            producer.DisposeBatch);

        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(partitionId ?? partitionKey, activity.GetTagItem(expectedTag));
        Assert.Null(activity.GetTagItem(otherTag));
        Assert.Equal([2], producer.SentBatchSizes);
        producer.AssertEveryBatchDisposed();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "mixed-routes-do-not-advertise-a-false-single-route")]
    public async Task MixedRoutes_DoNotTagTheActivityWithTheFirstRouteAsync()
    {
        using var activity = new Activity("eventhub-mixed-route") { IsAllDataRequested = true };
        activity.Start();
        var producer = new RecordingProducerContext();
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionId: "0", cancellationToken: TestContext.Current.CancellationToken),
            CreateContext(2, partitionId: "1", cancellationToken: TestContext.Current.CancellationToken)
        ];

        await EventHubProducerBatchSender.SendAsync(
            producer,
            contexts,
            TestContext.Current.CancellationToken,
            producer.DisposeBatch);

        Assert.True(activity.IsAllDataRequested);
        Assert.Null(activity.GetTagItem("PartitionId"));
        Assert.Null(activity.GetTagItem("PartitionKey"));
        Assert.Equal([new Route("0", null), new Route("1", null)], producer.CreatedRoutes);
        Assert.Equal([1, 1], producer.SentBatchSizes);
        producer.AssertEveryBatchDisposed();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "unrecorded-activity-does-not-gain-route-tags")]
    public async Task UnrecordedActivity_DoesNotGainRouteTagsAsync()
    {
        using var activity = new Activity("eventhub-unrecorded-route") { IsAllDataRequested = false };
        activity.Start();
        var producer = new RecordingProducerContext();
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionKey: "private-route", cancellationToken: TestContext.Current.CancellationToken)
        ];

        await EventHubProducerBatchSender.SendAsync(
            producer,
            contexts,
            TestContext.Current.CancellationToken,
            producer.DisposeBatch);

        Assert.False(activity.IsAllDataRequested);
        Assert.Null(activity.GetTagItem("PartitionId"));
        Assert.Null(activity.GetTagItem("PartitionKey"));
        Assert.Equal([new Route(null, "private-route")], producer.CreatedRoutes);
        Assert.Equal([1], producer.SentBatchSizes);
        producer.AssertEveryBatchDisposed();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "size-rollover-preserves-cardinality-and-disposes-batches")]
    public async Task SizeRollover_PreservesCardinalityAndDisposesEveryBatchAsync()
    {
        var producer = new RecordingProducerContext { MaximumEntriesPerBatch = 1 };
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionKey: "ordered", cancellationToken: TestContext.Current.CancellationToken),
            CreateContext(2, partitionKey: "ordered", cancellationToken: TestContext.Current.CancellationToken)
        ];

        await EventHubProducerBatchSender.SendAsync(
            producer,
            contexts,
            TestContext.Current.CancellationToken,
            producer.DisposeBatch);

        Assert.Equal([1, 1], producer.SentBatchSizes);
        Assert.Equal(2, producer.CreatedRoutes.Count);
        Assert.All(producer.CreatedRoutes, route => Assert.Equal(new Route(null, "ordered"), route));
        producer.AssertEveryBatchDisposed();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "oversized-event-rejected-without-empty-send-or-batch-leak")]
    public async Task OversizedEvent_IsRejectedWithoutSendingAnEmptyBatchOrLeakingTheBatchAsync()
    {
        var producer = new RecordingProducerContext { MaximumEntriesPerBatch = 0 };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EventHubProducerBatchSender.SendAsync(
                producer,
                new[] { CreateContext(1, partitionId: "0", cancellationToken: TestContext.Current.CancellationToken) },
                TestContext.Current.CancellationToken,
                producer.DisposeBatch));

        Assert.Contains("maximum Event Hubs batch size", exception.Message, StringComparison.Ordinal);
        Assert.Empty(producer.SentBatchSizes);
        Assert.Single(producer.CreatedRoutes);
        producer.AssertEveryBatchDisposed();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "late-invalid-context-preflight-prevents-partial-send")]
    public async Task InvalidLaterContext_IsRejectedBeforeAnyProviderOperationAsync()
    {
        var producer = new RecordingProducerContext();
        EventHubMessageSendContext<TestMessage> invalid =
            CreateContext(2, partitionId: "0", cancellationToken: TestContext.Current.CancellationToken);
        invalid.Serializer = new RejectingMessageSerializer();
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionId: "0", cancellationToken: TestContext.Current.CancellationToken),
            invalid
        ];

        await Assert.ThrowsAsync<SerializationException>(() =>
            EventHubProducerBatchSender.SendAsync(
                producer,
                contexts,
                TestContext.Current.CancellationToken,
                producer.DisposeBatch));

        Assert.Empty(producer.CreatedRoutes);
        Assert.Empty(producer.SentBatchSizes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "provider-failure-disposes-current-batch")]
    public async Task ProviderFailure_DisposesTheCurrentBatchAsync()
    {
        var expected = new InvalidOperationException("provider failed");
        var producer = new RecordingProducerContext { ProduceException = expected };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EventHubProducerBatchSender.SendAsync(
                producer,
                new[] { CreateContext(1, partitionKey: "customer-1", cancellationToken: TestContext.Current.CancellationToken) },
                TestContext.Current.CancellationToken,
                producer.DisposeBatch));

        Assert.Same(expected, actual);
        Assert.Equal([1], producer.SentBatchSizes);
        producer.AssertEveryBatchDisposed();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "confirmed-earlier-route-or-size-batch-survives-later-provider-failure")]
    public async Task LaterProviderFailure_PreservesOnlyEarlierConfirmedContextAsync(bool splitBySize)
    {
        var expected = new InvalidOperationException("second provider batch failed");
        var producer = new RecordingProducerContext
        {
            MaximumEntriesPerBatch = splitBySize ? 2 : int.MaxValue,
            ProduceException = expected,
            FailProduceCall = 2,
        };
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionKey: "first", cancellationToken: TestContext.Current.CancellationToken),
            CreateContext(2, partitionKey: "first", cancellationToken: TestContext.Current.CancellationToken),
            CreateContext(3, partitionKey: splitBySize ? "first" : "second", cancellationToken: TestContext.Current.CancellationToken),
        ];

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EventHubProducerBatchSender.SendAsync(producer, contexts,
                TestContext.Current.CancellationToken, producer.DisposeBatch));

        Assert.Same(expected, actual);
        Assert.Equal([2, 1], producer.SentBatchSizes);
        Assert.True(contexts[0].IsProviderConfirmed);
        Assert.True(contexts[1].IsProviderConfirmed);
        Assert.False(contexts[2].IsProviderConfirmed);
        producer.AssertEveryBatchDisposed();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "cleanup-failure-after-confirmed-batch-does-not-abort-later-batch")]
    public async Task DisposeFailure_AfterConfirmedBatch_DoesNotResendOrAbortPendingBatchAsync()
    {
        var producer = new RecordingProducerContext
        {
            MaximumEntriesPerBatch = 1,
            DisposeException = new InvalidOperationException("cleanup failed"),
            FailDisposeCall = 1,
        };
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionKey: "same", cancellationToken: TestContext.Current.CancellationToken),
            CreateContext(2, partitionKey: "same", cancellationToken: TestContext.Current.CancellationToken),
        ];

        await EventHubProducerBatchSender.SendAsync(producer, contexts,
            TestContext.Current.CancellationToken, producer.DisposeBatch);

        Assert.Equal([1, 1], producer.SentBatchSizes);
        Assert.Equal(2, producer.DisposeCalls);
        Assert.All(contexts, context => Assert.True(context.IsProviderConfirmed));
        producer.AssertEveryBatchDisposed();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "cleanup-failure-cannot-mask-provider-batch-failure")]
    public async Task DisposeFailure_AfterProviderFailure_PreservesPrimaryExceptionAsync()
    {
        var providerFailure = new InvalidOperationException("provider failed");
        var producer = new RecordingProducerContext
        {
            ProduceException = providerFailure,
            DisposeException = new InvalidOperationException("cleanup failed"),
            FailDisposeCall = 1,
        };
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionKey: "same", cancellationToken: TestContext.Current.CancellationToken),
        ];

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            EventHubProducerBatchSender.SendAsync(producer, contexts,
                TestContext.Current.CancellationToken, producer.DisposeBatch));

        Assert.Same(providerFailure, actual);
        Assert.Equal(1, producer.DisposeCalls);
        Assert.False(contexts[0].IsProviderConfirmed);
        producer.AssertEveryBatchDisposed();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-BATCH-SEND", "every-context-cancellation-governs-shared-provider-operation")]
    public async Task EveryContextCancellation_GovernsTheSharedProviderOperationAsync()
    {
        using var secondContextCancellation = new CancellationTokenSource();
        var producer = new RecordingProducerContext { WaitForProduceCancellation = true };
        EventHubMessageSendContext<TestMessage>[] contexts =
        [
            CreateContext(1, partitionKey: "customer", cancellationToken: TestContext.Current.CancellationToken),
            CreateContext(2, partitionKey: "customer", cancellationToken: secondContextCancellation.Token)
        ];

        Task send = EventHubProducerBatchSender.SendAsync(
            producer,
            contexts,
            TestContext.Current.CancellationToken,
            producer.DisposeBatch);
        await producer.ProduceStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        secondContextCancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);

        Assert.True(exception.CancellationToken.IsCancellationRequested);
        producer.AssertEveryBatchDisposed();
    }

    private static EventHubMessageSendContext<TestMessage> CreateContext(
        int value,
        string? partitionId = null,
        string? partitionKey = null,
        CancellationToken cancellationToken = default)
    {
        return new EventHubMessageSendContext<TestMessage>(new TestMessage(value), cancellationToken)
        {
            Serializer = ServiceBusMetadataJson.MessageSerializer,
            PartitionId = partitionId,
            PartitionKey = partitionKey
        };
    }

    private sealed record TestMessage(int Value);

    private sealed record Route(string? PartitionId, string? PartitionKey);

    private sealed class RejectingMessageSerializer : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/vnd.vicione.rejected-event");

        public MessageBody GetMessageBody<T>(SendContext<T> context)
            where T : class =>
            throw new SerializationException("The Event Hubs message could not be serialized.");
    }

    private sealed class RecordingProducerContext :
        BasePipeContext,
        ProducerContext
    {
        private readonly List<EventDataBatch> _createdBatches = [];
        private readonly HashSet<EventDataBatch> _disposedBatches = [];
        private readonly TaskCompletionSource _providerCancellationGate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingProducerContext()
            : base(CancellationToken.None)
        {
        }

        public List<Route> CreatedRoutes { get; } = [];
        public int MaximumEntriesPerBatch { get; set; } = int.MaxValue;
        public Exception? ProduceException { get; set; }
        public int? FailProduceCall { get; set; }
        public Exception? DisposeException { get; set; }
        public int? FailDisposeCall { get; set; }
        public int DisposeCalls { get; private set; }
        public TaskCompletionSource ProduceStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<int> SentBatchSizes { get; } = [];
        public bool WaitForProduceCancellation { get; set; }

        public ValueTask DisposeAsync() => default;

        public ValueTask<EventDataBatch> CreateBatchAsync(
            CreateBatchOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var accepted = 0;
            EventDataBatch batch = EventHubsModelFactory.EventDataBatch(
                1_000_000,
                [],
                options,
                _ => accepted++ < MaximumEntriesPerBatch);

            _createdBatches.Add(batch);
            CreatedRoutes.Add(new Route(options.PartitionId, options.PartitionKey));
            return ValueTask.FromResult(batch);
        }

        public Task ProduceAsync(
            IEnumerable<EventData> eventData,
            SendEventOptions options,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public async Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken)
        {
            SentBatchSizes.Add(eventDataBatch.Count);
            ProduceStarted.TrySetResult();

            if (ProduceException is not null && (FailProduceCall is null || SentBatchSizes.Count == FailProduceCall))
                throw ProduceException;

            if (WaitForProduceCancellation)
                await _providerCancellationGate.Task.WaitAsync(cancellationToken);
        }

        public void AssertEveryBatchDisposed()
        {
            Assert.Equal(_createdBatches.Count, _disposedBatches.Count);
            Assert.All(_createdBatches, batch => Assert.Contains(batch, _disposedBatches));
        }

        public void DisposeBatch(EventDataBatch batch)
        {
            DisposeCalls++;
            batch.Dispose();
            Assert.True(_disposedBatches.Add(batch), "A provider batch was disposed more than once.");
            if (DisposeException is not null && (FailDisposeCall is null || DisposeCalls == FailDisposeCall))
                throw DisposeException;
        }
    }
}
