using Amazon.SQS.Model;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.AmazonSqsTransport.Configuration;
using ViciOne.ServiceBus.AmazonSqsTransport.Middleware;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests;

public sealed class AmazonSqsReceiveLifecycleTests
{
    private static readonly DateTimeOffset StartTime = new(2038, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "provider-cancellation-is-not-silently-converted")]
    public async Task ReceiveLoop_PropagatesUnrequestedProviderCancellationAsync()
    {
        var expected = new OperationCanceledException("provider canceled independently");

        OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(
            () => AmazonSqsMessageReceiver.ReceiveMessagesAsync(
                _ => Task.FromException<IList<Message>>(expected),
                CancellationToken.None));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "monotonic-renewal-bounded-by-total-duration")]
    public async Task VisibilityRenewal_UsesMonotonicTimeAndNeverExtendsBeyondTheTotalLimitAsync()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        QueueReceiveSettings settings = CreateSettings();
        settings.VisibilityTimeout = 10;
        settings.MaxVisibilityTimeoutRenewal = 60;
        settings.MaxVisibilityTimeout = TimeSpan.FromSeconds(25);

        var renewed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var renewalCalls = 0;
        var deleted = 0;
        var receiveLock = new AmazonSqsReceiveLockContext(
            new Uri("amazonsqs://eu-central-1/orders"),
            new Message { ReceiptHandle = "receipt" },
            settings,
            CancellationToken.None,
            timeProvider,
            (_, _, seconds, _) =>
            {
                Interlocked.Increment(ref renewalCalls);
                renewed.TrySetResult(seconds);
                return Task.CompletedTask;
            },
            (_, _, _) =>
            {
                Interlocked.Increment(ref deleted);
                return Task.CompletedTask;
            },
            () => false);

        // Seventy percent of the original ten-second lease is the first renewal boundary.
        timeProvider.Advance(TimeSpan.FromSeconds(7));
        int renewalSeconds = await renewed.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(18, renewalSeconds);
        Assert.Equal(1, Volatile.Read(ref renewalCalls));

        await receiveLock.CompleteAsync(TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromHours(1));

        Assert.Equal(1, Volatile.Read(ref renewalCalls));
        Assert.Equal(1, Volatile.Read(ref deleted));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "unrequested-renewal-cancellation-loses-lock")]
    public async Task UnrequestedProviderCancellationDuringRenewal_MarksTheReceiveLockLostAsync()
    {
        QueueReceiveSettings settings = CreateSettings();
        settings.VisibilityTimeout = 0;
        var expected = new OperationCanceledException("provider cancellation");

        var receiveLock = new AmazonSqsReceiveLockContext(
            new Uri("amazonsqs://eu-central-1/orders"),
            new Message { ReceiptHandle = "receipt" },
            settings,
            CancellationToken.None,
            new FakeTimeProvider(StartTime),
            (_, _, _, _) => Task.FromException(expected),
            (_, _, _) => Task.CompletedTask,
            () => false);

        await Assert.ThrowsAsync<TransportException>(() => receiveLock.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
        await receiveLock.FaultedAsync(new InvalidOperationException("business failure"), TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "maximum-renewal-window-expiry-loses-lock")]
    public async Task MaximumRenewalWindowExpiry_MarksTheReceiveLockLostAsync()
    {
        QueueReceiveSettings settings = CreateSettings();
        settings.VisibilityTimeout = 0;
        settings.MaxVisibilityTimeout = TimeSpan.FromSeconds(30);
        var renewalCalls = 0;
        var timeProvider = new SequenceTimeProvider(
            StartTime,
            TimeSpan.Zero,
            settings.MaxVisibilityTimeout + TimeSpan.FromTicks(1));

        var receiveLock = new AmazonSqsReceiveLockContext(
            new Uri("amazonsqs://eu-central-1/orders"),
            new Message { ReceiptHandle = "receipt" },
            settings,
            CancellationToken.None,
            timeProvider,
            (_, _, _, _) =>
            {
                renewalCalls++;
                return Task.CompletedTask;
            },
            (_, _, _) => Task.CompletedTask,
            () => false);

        await Assert.ThrowsAsync<TransportException>(
            () => receiveLock.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, renewalCalls);
        await receiveLock.FaultedAsync(new InvalidOperationException("business failure"), TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "settings-snapshot-and-settlement-order")]
    public async Task Complete_CancelsTheSnapshottedRenewalBeforeDeletingTheMessageAsync()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        QueueReceiveSettings settings = CreateSettings();
        settings.VisibilityTimeout = 30;
        var renewalCalls = 0;
        var deleteObservedRenewalCount = -1;

        var receiveLock = new AmazonSqsReceiveLockContext(
            new Uri("amazonsqs://eu-central-1/orders"),
            new Message { ReceiptHandle = "receipt" },
            settings,
            CancellationToken.None,
            timeProvider,
            (_, _, _, _) =>
            {
                Interlocked.Increment(ref renewalCalls);
                return Task.CompletedTask;
            },
            (_, _, _) =>
            {
                deleteObservedRenewalCount = Volatile.Read(ref renewalCalls);
                return Task.CompletedTask;
            },
            () => false);

        settings.VisibilityTimeout = 0;
        settings.QueueUrl = "https://mutated.example.invalid";
        await receiveLock.CompleteAsync(TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromHours(1));

        Assert.Equal(0, deleteObservedRenewalCount);
        Assert.Equal(0, Volatile.Read(ref renewalCalls));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "failed-or-cancelled-completion-preserves-error-and-loses-lock")]
    public async Task FailedOrCancelledComplete_PreservesTheOriginalFailureAndMarksTheLockLostAsync(bool callerCancellation)
    {
        using var cancellationSource = new CancellationTokenSource();
        if (callerCancellation)
            cancellationSource.Cancel();

        Exception expected = callerCancellation
            ? new OperationCanceledException("caller cancellation observed by provider", CancellationToken.None)
            : new InvalidOperationException("provider settlement failed");
        QueueReceiveSettings settings = CreateSettings();
        settings.VisibilityTimeout = 30;
        var receiveLock = new AmazonSqsReceiveLockContext(
            new Uri("amazonsqs://eu-central-1/orders"),
            new Message { ReceiptHandle = "receipt" },
            settings,
            cancellationSource.Token,
            new FakeTimeProvider(StartTime),
            (_, _, _, _) => Task.CompletedTask,
            (_, _, _) => Task.FromException(expected),
            () => false);

        Exception actual = await Assert.ThrowsAnyAsync<Exception>(
            () => receiveLock.CompleteAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        await Assert.ThrowsAsync<TransportException>(
            () => receiveLock.ValidateLockStatusAsync(TestContext.Current.CancellationToken));
    }

    private static QueueReceiveSettings CreateSettings()
    {
        var topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var parent = new AmazonSqsEndpointConfiguration(topology);
        var settings = new QueueReceiveSettings(parent.CreateEndpointConfiguration(false), "orders", true, false)
        {
            QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders"
        };
        return settings;
    }

    private sealed class SequenceTimeProvider(DateTimeOffset utcNow, params TimeSpan[] elapsed) : TimeProvider
    {
        private readonly long[] _timestamps = elapsed.Select(value => value.Ticks).ToArray();
        private int _index = -1;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public override long GetTimestamp()
        {
            int index = Math.Min(Interlocked.Increment(ref _index), _timestamps.Length - 1);
            return _timestamps[index];
        }
    }
}
