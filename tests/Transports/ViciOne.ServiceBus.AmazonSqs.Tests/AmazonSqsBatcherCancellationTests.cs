using System.Collections.Concurrent;
using System.Net;
using global::Amazon.Runtime;
using global::Amazon.SimpleNotificationService;
using global::Amazon.SimpleNotificationService.Model;
using global::Amazon.SQS;
using global::Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsBatcherCancellationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "pre-canceled-entry-not-admitted")]
    public async Task ExecuteAsync_PreCanceledToken_DoesNotAdmitEntryOrCallProviderAsync()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        await using var batcher = new ControlledBatcher(messageLimit: 1);

        Task operation = batcher.ExecuteAsync(new ControlledEntry("canceled"), cancellationSource.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(0, batcher.ProviderCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "cancel-before-dispatch-filter-and-reindex")]
    public async Task CancellationAfterCollectionBeforeDispatch_FiltersEntryAndReindexesSurvivorAsync()
    {
        using var cancellationSource = new CancellationTokenSource();
        await using var batcher = new ControlledBatcher(messageLimit: 2);

        Task canceledOperation = batcher.ExecuteAsync(new ControlledEntry("canceled"), cancellationSource.Token);
        await batcher.FirstEntryMeasured.Task.WaitAsync(TestContext.Current.CancellationToken);

        cancellationSource.Cancel();
        Task survivingOperation = batcher.ExecuteAsync(
            new ControlledEntry("survivor"),
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => canceledOperation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        await survivingOperation.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, batcher.ProviderCallCount);
        CapturedEntry sentEntry = Assert.Single(Assert.Single(batcher.SentBatches));
        Assert.Equal("survivor", sentEntry.Value);
        Assert.Equal("0", sentEntry.Id);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "cancel-after-dispatch-only-cancels-caller-wait")]
    public async Task CancellationAfterDispatch_CancelsOnlyCallerWaitAndPreservesSharedProviderRequestAsync()
    {
        using var cancellationSource = new CancellationTokenSource();
        await using var batcher = new ControlledBatcher(messageLimit: 2, blockProvider: true);

        Task canceledOperation = batcher.ExecuteAsync(new ControlledEntry("canceled"), cancellationSource.Token);
        Task survivingOperation = batcher.ExecuteAsync(
            new ControlledEntry("survivor"),
            TestContext.Current.CancellationToken);

        await batcher.ProviderStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellationSource.Cancel();

        Exception? cancellationException = await Record.ExceptionAsync(
            () => canceledOperation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.False(batcher.ProviderCancellationToken.IsCancellationRequested);
        Assert.False(survivingOperation.IsCompleted);

        batcher.ReleaseProvider();
        await survivingOperation.WaitAsync(TestContext.Current.CancellationToken);

        Assert.IsAssignableFrom<OperationCanceledException>(cancellationException);
        Assert.Equal(1, batcher.ProviderCallCount);
        Assert.Equal(["0", "1"], Assert.Single(batcher.SentBatches).Select(x => x.Id));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "canceled-caller-not-overwritten-by-provider-failure")]
    public async Task CanceledCaller_RemainsCanceledWhenSharedProviderRequestFailsAsync()
    {
        using var cancellationSource = new CancellationTokenSource();
        await using var batcher = new ControlledBatcher(
            messageLimit: 2,
            blockProvider: true,
            providerException: new InvalidOperationException("provider failure"));

        Task canceledOperation = batcher.ExecuteAsync(new ControlledEntry("canceled"), cancellationSource.Token);
        Task faultedOperation = batcher.ExecuteAsync(
            new ControlledEntry("faulted"),
            TestContext.Current.CancellationToken);

        await batcher.ProviderStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellationSource.Cancel();
        Exception? cancellationException = await Record.ExceptionAsync(
            () => canceledOperation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        batcher.ReleaseProvider();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => faultedOperation);

        Assert.IsAssignableFrom<OperationCanceledException>(cancellationException);
        Assert.Equal("provider failure", exception.Message);
        Assert.Equal(TaskStatus.Canceled, canceledOperation.Status);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "empty-filtered-batch-skips-provider")]
    public async Task DisposeAsync_WithOnlyCanceledCollectedEntries_DoesNotCallProviderAsync()
    {
        using var cancellationSource = new CancellationTokenSource();
        var batcher = new ControlledBatcher(messageLimit: 2);

        Task operation = batcher.ExecuteAsync(new ControlledEntry("canceled"), cancellationSource.Token);
        await batcher.FirstEntryMeasured.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellationSource.Cancel();

        Exception? cancellationException = await Record.ExceptionAsync(
            () => operation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        await batcher.DisposeAsync();

        Assert.IsAssignableFrom<OperationCanceledException>(cancellationException);
        Assert.Equal(0, batcher.ProviderCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "provider-owner-cancellation-cancels-dispatched-entries")]
    public async Task ProviderOwnerCancellation_CancelsEveryDispatchedEntryAsync()
    {
        using var providerCancellationSource = new CancellationTokenSource();
        await using var batcher = new ControlledBatcher(
            messageLimit: 2,
            blockProvider: true,
            providerCancellationToken: providerCancellationSource.Token);

        Task first = batcher.ExecuteAsync(new ControlledEntry("first"), CancellationToken.None);
        Task second = batcher.ExecuteAsync(new ControlledEntry("second"), CancellationToken.None);
        await batcher.ProviderStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        providerCancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.WaitAsync(TestContext.Current.CancellationToken));
        Assert.Equal(TaskStatus.Canceled, first.Status);
        Assert.Equal(TaskStatus.Canceled, second.Status);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "queue-send-caller-cancellation-after-dispatch")]
    public async Task QueueInfoSendAsync_CallerCancellationAfterDispatch_DoesNotCancelProviderRequestAsync()
    {
        using var client = new BlockingSqsClient();
        await using var queue = CreateQueueInfo(client);
        using var callerCancellationSource = new CancellationTokenSource();

        Task operation = queue.SendAsync(
            new SendMessageBatchRequestEntry("", "message"),
            callerCancellationSource.Token);
        await client.SendStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        callerCancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => operation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.False(client.SendCancellationToken.IsCancellationRequested);

        client.ReleaseSend();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "queue-delete-caller-cancellation-after-dispatch")]
    public async Task QueueInfoDeleteAsync_CallerCancellationAfterDispatch_DoesNotCancelProviderRequestAsync()
    {
        using var client = new BlockingSqsClient();
        await using var queue = CreateQueueInfo(client);
        using var callerCancellationSource = new CancellationTokenSource();

        Task operation = queue.DeleteAsync("receipt-handle", callerCancellationSource.Token);
        await client.DeleteStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        callerCancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => operation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.False(client.DeleteCancellationToken.IsCancellationRequested);

        client.ReleaseDelete();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CANCELLATION", "topic-publish-caller-cancellation-after-dispatch")]
    public async Task TopicInfoPublishAsync_CallerCancellationAfterDispatch_DoesNotCancelProviderRequestAsync()
    {
        using var client = new BlockingSnsClient();
        await using var topic = new TopicInfo(
            "events",
            "arn:aws:sns:eu-central-1:123456789012:events",
            client,
            CancellationToken.None,
            existing: true);
        using var callerCancellationSource = new CancellationTokenSource();

        Task operation = topic.PublishAsync(
            new PublishBatchRequestEntry { Message = "message" },
            callerCancellationSource.Token);
        await client.PublishStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        callerCancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => operation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.False(client.PublishCancellationToken.IsCancellationRequested);

        client.ReleasePublish();
    }

    static QueueInfo CreateQueueInfo(IAmazonSQS client) => new(
        "orders",
        "https://sqs.eu-central-1.amazonaws.com/123456789012/orders",
        new Dictionary<string, string>
        {
            [QueueAttributeName.QueueArn] = "arn:aws:sqs:eu-central-1:123456789012:orders"
        },
        client,
        CancellationToken.None,
        existing: true);

    private sealed record CapturedEntry(string Value, string Id);

    private sealed class ControlledEntry(string value)
    {
        public string? Id { get; set; }
        public string Value { get; } = value;
    }

    private sealed class ControlledBatcher : Batcher<ControlledEntry>
    {
        readonly bool _blockProvider;
        readonly CancellationToken _providerCancellationToken;
        readonly Exception? _providerException;
        readonly TaskCompletionSource _releaseProvider = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ControlledBatcher(
            int messageLimit,
            bool blockProvider = false,
            Exception? providerException = null,
            CancellationToken providerCancellationToken = default)
            : base(new ControlledBatchSettings(messageLimit))
        {
            _blockProvider = blockProvider;
            _providerException = providerException;
            _providerCancellationToken = providerCancellationToken;
        }

        public TaskCompletionSource FirstEntryMeasured { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ProviderStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ProviderCancellationToken => _providerCancellationToken;
        public int ProviderCallCount { get; private set; }
        public ConcurrentQueue<IReadOnlyList<CapturedEntry>> SentBatches { get; } = new();

        public void ReleaseProvider() => _releaseProvider.TrySetResult();

        protected override void AssignEntryId(ControlledEntry entry, string entryId) => entry.Id = entryId;

        protected override int CalculateEntryLength(ControlledEntry entry)
        {
            FirstEntryMeasured.TrySetResult();
            return 1;
        }

        protected override async Task SendBatchAsync(IList<BatchEntry<ControlledEntry>> batch)
        {
            ProviderCallCount++;
            SentBatches.Enqueue(batch.Select(x => new CapturedEntry(x.Entry.Value, x.Entry.Id!)).ToArray());
            ProviderStarted.TrySetResult();

            if (_blockProvider)
                await _releaseProvider.Task.WaitAsync(_providerCancellationToken);

            if (_providerException is not null)
                throw _providerException;

            foreach (BatchEntry<ControlledEntry> entry in batch)
                entry.SetCompleted();
        }
    }

    private sealed class ControlledBatchSettings(int messageLimit) : BatchSettings
    {
        public int MessageLimit { get; } = messageLimit;
        public int BatchLimit => 1;
        public int SizeLimit => 1024;
        public TimeSpan Timeout => TimeSpan.FromSeconds(5);
    }

    private sealed class BlockingSqsClient()
        : AmazonSQSClient(
            new AnonymousAWSCredentials(),
            new AmazonSQSConfig
            {
                ServiceURL = "http://127.0.0.1:1",
                AuthenticationRegion = "eu-central-1"
            })
    {
        readonly TaskCompletionSource _releaseDelete = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _releaseSend = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource DeleteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SendStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken DeleteCancellationToken { get; private set; }
        public CancellationToken SendCancellationToken { get; private set; }

        public void ReleaseDelete() => _releaseDelete.TrySetResult();
        public void ReleaseSend() => _releaseSend.TrySetResult();

        public override async Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(
            DeleteMessageBatchRequest request,
            CancellationToken cancellationToken = default)
        {
            DeleteCancellationToken = cancellationToken;
            DeleteStarted.TrySetResult();
            await _releaseDelete.Task.WaitAsync(cancellationToken);

            return new DeleteMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = request.Entries.Select(x => new DeleteMessageBatchResultEntry { Id = x.Id }).ToList(),
                Failed = []
            };
        }

        public override async Task<SendMessageBatchResponse> SendMessageBatchAsync(
            SendMessageBatchRequest request,
            CancellationToken cancellationToken = default)
        {
            SendCancellationToken = cancellationToken;
            SendStarted.TrySetResult();
            await _releaseSend.Task.WaitAsync(cancellationToken);

            return new SendMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = request.Entries.Select(x => new SendMessageBatchResultEntry { Id = x.Id }).ToList(),
                Failed = []
            };
        }
    }

    private sealed class BlockingSnsClient()
        : AmazonSimpleNotificationServiceClient(
            new AnonymousAWSCredentials(),
            new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = "http://127.0.0.1:1",
                AuthenticationRegion = "eu-central-1"
            })
    {
        readonly TaskCompletionSource _releasePublish = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource PublishStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken PublishCancellationToken { get; private set; }

        public void ReleasePublish() => _releasePublish.TrySetResult();

        public override async Task<PublishBatchResponse> PublishBatchAsync(
            PublishBatchRequest request,
            CancellationToken cancellationToken = default)
        {
            PublishCancellationToken = cancellationToken;
            PublishStarted.TrySetResult();
            await _releasePublish.Task.WaitAsync(cancellationToken);

            return new PublishBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = request.PublishBatchRequestEntries
                    .Select(x => new PublishBatchResultEntry { Id = x.Id })
                    .ToList(),
                Failed = []
            };
        }
    }
}
