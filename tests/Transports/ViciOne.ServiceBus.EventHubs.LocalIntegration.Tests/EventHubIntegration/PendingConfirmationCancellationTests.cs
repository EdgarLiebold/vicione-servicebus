using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.EventHubs.Checkpoints;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class PendingConfirmationCancellationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "individual-cancellation-preserves-neighbors-and-replacement")]
    public async Task IndividualCancellation_PreservesOtherPartitionsOffsetsAndReplacementAsync()
    {
        using var lifetime = new CancellationTokenSource();
        using var delivery = new CancellationTokenSource();
        using var confirmations = new PendingConfirmationCollection(lifetime.Token);
        ProcessEventArgs targetEvent = Event("0", "100");
        ProcessEventArgs otherPartitionEvent = Event("1", "100");
        ProcessEventArgs otherOffsetEvent = Event("0", "101");
        IPendingConfirmation target = confirmations.Add(targetEvent);
        IPendingConfirmation otherPartition = confirmations.Add(otherPartitionEvent);
        IPendingConfirmation otherOffset = confirmations.Add(otherOffsetEvent);
        delivery.Cancel();

        confirmations.Canceled(targetEvent, delivery.Token);

        await AssertCanceledAsync(target, delivery.Token);
        Assert.False(otherPartition.Confirmed.IsCompleted);
        Assert.False(otherOffset.Confirmed.IsCompleted);
        confirmations.Complete(targetEvent);
        confirmations.Faulted(targetEvent, new InvalidOperationException("late failure"));
        confirmations.Canceled(targetEvent, lifetime.Token);
        await AssertCanceledAsync(target, delivery.Token);

        IPendingConfirmation replacement = confirmations.Add(targetEvent);
        Assert.NotSame(target, replacement);
        Assert.False(replacement.Confirmed.IsCompleted);
        confirmations.Complete(targetEvent);
        confirmations.Complete(otherPartitionEvent);
        confirmations.Complete(otherOffsetEvent);
        await Task.WhenAll(replacement.Confirmed, otherPartition.Confirmed, otherOffset.Confirmed)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        Assert.True(replacement.Confirmed.IsCompletedSuccessfully);
        Assert.True(otherPartition.Confirmed.IsCompletedSuccessfully);
        Assert.True(otherOffset.Confirmed.IsCompletedSuccessfully);
        await AssertCanceledAsync(target, delivery.Token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "processor-cancellation-preserves-terminal-results-and-rejects-new-events")]
    public async Task ProcessorCancellation_CancelsEveryOpenConfirmationAndPreservesTerminalResultsAsync()
    {
        using var lifetime = new CancellationTokenSource();
        using var confirmations = new PendingConfirmationCollection(lifetime.Token);
        ProcessEventArgs completedEvent = Event("0", "100");
        ProcessEventArgs faultedEvent = Event("1", "100");
        IPendingConfirmation completed = confirmations.Add(completedEvent);
        IPendingConfirmation faulted = confirmations.Add(faultedEvent);
        var expected = new InvalidOperationException("consumer failure");
        confirmations.Complete(completedEvent);
        confirmations.Faulted(faultedEvent, expected);
        MessageNotConsumedException original = await Assert.ThrowsAsync<MessageNotConsumedException>(() =>
            faulted.Confirmed.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.Same(expected, original.InnerException);
        ProcessEventArgs[] pendingEvents = [Event("0", "101"), Event("1", "101"), Event("2", "100")];
        IPendingConfirmation[] pending = pendingEvents.Select(confirmations.Add).ToArray();
        Assert.All(pending, confirmation => Assert.False(confirmation.Confirmed.IsCompleted));

        lifetime.Cancel();

        foreach (IPendingConfirmation confirmation in pending)
            await AssertCanceledAsync(confirmation, lifetime.Token);
        foreach (ProcessEventArgs eventArgs in pendingEvents)
        {
            confirmations.Complete(eventArgs);
            confirmations.Faulted(eventArgs, new InvalidOperationException("late failure"));
        }
        foreach (IPendingConfirmation confirmation in pending)
            await AssertCanceledAsync(confirmation, lifetime.Token);
        Assert.True(completed.Confirmed.IsCompletedSuccessfully);
        Assert.Same(original, await Assert.ThrowsAsync<MessageNotConsumedException>(() =>
            faulted.Confirmed.WaitAsync(Timeout, TestContext.Current.CancellationToken)));
        OperationCanceledException rejected = Assert.Throws<OperationCanceledException>(() => confirmations.Add(Event("3", "102")));
        Assert.Equal(lifetime.Token, rejected.CancellationToken);
    }

    private static async Task AssertCanceledAsync(IPendingConfirmation confirmation, CancellationToken expected)
    {
        TaskCanceledException error = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            confirmation.Confirmed.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.Equal(expected, error.CancellationToken);
        Assert.True(confirmation.Confirmed.IsCanceled);
    }

    private static ProcessEventArgs Event(string partition, string offset) => new(
        EventHubsModelFactory.PartitionContext("tests.servicebus.windows.net", "orders", "group", partition),
        EventHubsModelFactory.EventData(eventBody: BinaryData.FromString("payload"), offsetString: offset),
        _ => throw new InvalidOperationException("Confirmation transitions must not update a checkpoint."),
        CancellationToken.None);
}
