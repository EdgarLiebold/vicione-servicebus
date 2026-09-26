using System.Collections.Concurrent;
using System.Threading.Channels;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubs.Checkpoints;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class BatchCheckpointBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CHECKPOINT-BATCH", "newest-checkpoint-falls-back-in-order-without-redundant-writes")]
    public async Task CompletedBatch_CheckpointsNewestAcceptedOffsetWithoutRedundantWritesAsync(int rejectedNewest)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var attempts = new ConcurrentQueue<(string Offset, CancellationToken Token)>();
        string? persisted = null;
        PendingConfirmation[] confirmations = Enumerable.Range(101, 3).Select(offset => Confirmation(offset.ToString(), token =>
        {
            attempts.Enqueue((offset.ToString(), token));
            if (offset > 103 - rejectedNewest)
                return Task.FromException(new InvalidOperationException($"Rejected checkpoint {offset}"));
            persisted = offset.ToString();
            return Task.CompletedTask;
        })).ToArray();
        var worker = new BatchCheckpointer(new Settings(3, 3), lifetime.Token);
        try
        {
            foreach (PendingConfirmation confirmation in confirmations)
                await worker.PendingAsync(confirmation, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Empty(attempts);
            foreach (PendingConfirmation confirmation in confirmations)
                confirmation.Complete();

            await worker.DisposeAsync().AsTask().WaitAsync(Timeout, TestContext.Current.CancellationToken);

            string[] expected = rejectedNewest switch
            {
                0 => ["103"],
                1 => ["103", "102"],
                _ => ["103", "102", "101"]
            };
            Assert.Equal(expected, attempts.Select(attempt => attempt.Offset));
            Assert.All(attempts, attempt => Assert.Equal(lifetime.Token, attempt.Token));
            Assert.Equal((103 - rejectedNewest).ToString(), persisted);
            Assert.All(confirmations, confirmation => Assert.True(confirmation.Confirmed.IsCompletedSuccessfully));
        }
        finally
        {
            foreach (PendingConfirmation confirmation in confirmations)
                confirmation.Complete();
            await lifetime.CancelAsync();
            await worker.DisposeAsync().AsTask().WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CHECKPOINT-BATCH", "exhausted-provider-fallback-preserves-worker-for-next-batch")]
    public async Task FailedCheckpointBatch_AllowsTheNextBatchToPersistAsync()
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var attempts = new ConcurrentQueue<(string Offset, CancellationToken Token)>();
        var oldestAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string? persisted = null;
        PendingConfirmation[] failedBatch = Enumerable.Range(101, 3).Select(offset => Confirmation(offset.ToString(), token =>
        {
            attempts.Enqueue((offset.ToString(), token));
            if (offset == 101)
                oldestAttempted.TrySetResult();
            return Task.FromException(new InvalidOperationException($"Rejected checkpoint {offset}"));
        })).ToArray();
        PendingConfirmation successor = Confirmation("104", token =>
        {
            attempts.Enqueue(("104", token));
            persisted = "104";
            return Task.CompletedTask;
        });
        var worker = new BatchCheckpointer(new Settings(3, 3), lifetime.Token);
        try
        {
            foreach (PendingConfirmation confirmation in failedBatch)
                await worker.PendingAsync(confirmation, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            foreach (PendingConfirmation confirmation in failedBatch)
                confirmation.Complete();
            await oldestAttempted.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Null(persisted);
            Assert.Equal(["103", "102", "101"], attempts.Select(attempt => attempt.Offset));

            successor.Complete();
            await worker.PendingAsync(successor, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            await worker.DisposeAsync().AsTask().WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.Equal(["103", "102", "101", "104"], attempts.Select(attempt => attempt.Offset));
            Assert.All(attempts, attempt => Assert.Equal(lifetime.Token, attempt.Token));
            Assert.Equal("104", persisted);
            Assert.True(successor.Confirmed.IsCompletedSuccessfully);
        }
        finally
        {
            foreach (PendingConfirmation confirmation in failedBatch)
                confirmation.Complete();
            successor.Complete();
            await lifetime.CancelAsync();
            await worker.DisposeAsync().AsTask().WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CHECKPOINT-BATCH", "canceled-full-queue-admission-never-updates-provider")]
    public async Task FullQueue_CancelsOnlyWaitingAdmissionAndDrainsAcceptedEventsAsync()
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var admission = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = new ConcurrentQueue<(string Offset, CancellationToken Token)>();
        string? persisted = null;
        PendingConfirmation first = Confirmation("101", async token =>
        {
            attempts.Enqueue(("101", token));
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            persisted = "101";
        });
        PendingConfirmation second = Confirmation("102", token =>
        {
            attempts.Enqueue(("102", token));
            persisted = "102";
            return Task.CompletedTask;
        });
        PendingConfirmation canceled = Confirmation("103", token =>
        {
            attempts.Enqueue(("103", token));
            persisted = "103";
            return Task.CompletedTask;
        });
        PendingConfirmation[] confirmations = [first, second, canceled];
        foreach (PendingConfirmation confirmation in confirmations)
            confirmation.Complete();
        var worker = new BatchCheckpointer(new Settings(1, 1), lifetime.Token);
        Task? waiting = null;
        try
        {
            await worker.PendingAsync(first, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            await entered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            await worker.PendingAsync(second, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            waiting = worker.PendingAsync(canceled, admission.Token);
            Assert.False(waiting.IsCompleted);
            Assert.Null(persisted);
            Assert.Equal(["101"], attempts.Select(attempt => attempt.Offset));

            await admission.CancelAsync();
            OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                waiting.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal(admission.Token, error.CancellationToken);
            Assert.True(waiting.IsCanceled);
            Assert.False(lifetime.IsCancellationRequested);
            release.SetResult();
            await worker.DisposeAsync().AsTask().WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.Equal(["101", "102"], attempts.Select(attempt => attempt.Offset));
            Assert.All(attempts, attempt => Assert.Equal(lifetime.Token, attempt.Token));
            Assert.Equal("102", persisted);
        }
        finally
        {
            release.TrySetResult();
            foreach (PendingConfirmation confirmation in confirmations)
                confirmation.Complete();
            await lifetime.CancelAsync();
            await worker.DisposeAsync().AsTask().WaitAsync(Timeout, CancellationToken.None);
            if (waiting is not null)
            {
                try
                {
                    await waiting.WaitAsync(Timeout, CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                }
                catch (ChannelClosedException)
                {
                }
            }
        }
    }

    private static PendingConfirmation Confirmation(string offset, Func<CancellationToken, Task> checkpoint) => new(new ProcessEventArgs(
        EventHubsModelFactory.PartitionContext("tests.servicebus.windows.net", "orders", "group", "0"),
        EventHubsModelFactory.EventData(eventBody: BinaryData.FromString($"payload-{offset}"), offsetString: offset),
        checkpoint, CancellationToken.None));

    private sealed class Settings(ushort count, ushort limit) : ReceiveSettings
    {
        public string ConsumerGroup => "group";
        public string ContainerName => "checkpoints";
        public string EventHubName => "orders";
        public ushort CheckpointMessageLimit => limit;
        public ushort CheckpointMessageCount => count;
        public int PrefetchCount => 3;
        public TimeSpan CheckpointInterval => TimeSpan.FromMinutes(5);
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
    }
}
