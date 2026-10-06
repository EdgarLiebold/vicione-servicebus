using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusScheduledSendOwnershipTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;
    private const long SequenceNumber = 9876543210123L;

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "scheduled-send-and-cancel-own-sdk-results-and-linked-token-lifetime")]
    public async Task SharedScheduledOperations_PreserveSdkInputsOutcomesAndLinkedTokenLifetimeAsync(bool schedule, int outcome)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        using var lease = new CancellationTokenSource();
        await using var sender = new RecordingSender();
        ConnectionContext connection = DispatchProxy.Create<ConnectionContext, NoCallsProxy>();
        var inner = new MessageSendEndpointContext(connection, sender);
        var shared = new SharedSendEndpointContext(inner, lease.Token);
        var message = new ServiceBusMessage(BinaryData.FromString("scheduled-operation"));
        DateTimeOffset due = new(2044, 3, 2, 1, 2, 3, TimeSpan.Zero);
        var failure = new ExpectedScheduledOperationException();
        Task? pending = null;
        Task<long>? scheduled = null;
        CancellationToken forwarded = default;
        try
        {
            Assert.Same(connection, shared.ConnectionContext);
            Assert.Equal("scheduled-entity", shared.EntityPath);
            Assert.Equal(lease.Token, shared.CancellationToken);
            if (schedule)
            {
                scheduled = shared.ScheduleSendAsync(message, due, caller.Token);
                pending = scheduled;
            }
            else
                pending = shared.CancelScheduledSendAsync(SequenceNumber, caller.Token);

            await Task.WhenAny(sender.Entered.Task, pending).WaitAsync(Timeout, TestToken);
            if (!sender.Entered.Task.IsCompleted)
                await pending.WaitAsync(Timeout, TestToken);
            Assert.True(sender.Entered.Task.IsCompleted);
            SdkCall call = Assert.Single(sender.Calls);
            forwarded = call.Token;
            Assert.True(forwarded.CanBeCanceled);
            Assert.False(forwarded.IsCancellationRequested);
            Assert.NotEqual(caller.Token, forwarded);
            Assert.NotEqual(lease.Token, forwarded);
            Assert.False(pending.IsCompleted);
            Assert.False(sender.ActualReturnedTask!.IsCompleted);
            Assert.Equal(schedule ? "schedule" : "cancel", call.Operation);
            if (schedule)
            {
                Assert.Same(message, call.Message);
                Assert.Equal(due, call.When);
                Assert.Null(call.Sequence);
            }
            else
            {
                Assert.Null(call.Message);
                Assert.Null(call.When);
                Assert.Equal(SequenceNumber, call.Sequence);
            }

            if (outcome == 0)
            {
                sender.ReleaseSuccess();
                await pending.WaitAsync(Timeout, TestToken);
                Assert.True(pending.IsCompletedSuccessfully);
                if (schedule)
                    Assert.Equal(SequenceNumber, await scheduled!.WaitAsync(Timeout, TestToken));
            }
            else if (outcome == 1)
            {
                sender.ReleaseFault(failure);
                ExpectedScheduledOperationException observed = await Assert.ThrowsAsync<ExpectedScheduledOperationException>(() =>
                    pending.WaitAsync(Timeout, TestToken));
                Assert.Same(failure, observed);
                Assert.True(pending.IsFaulted);
            }
            else
            {
                Assert.True(outcome is 2 or 3);
                await (outcome == 2 ? caller : lease).CancelAsync();
                // Test the actual forwarded token; the recording SDK then ends its own actual returned task.
                Assert.True(forwarded.IsCancellationRequested);
                sender.ReleaseCanceled(forwarded);
                OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    pending.WaitAsync(Timeout, TestToken));
                Assert.Equal(forwarded, observed.CancellationToken);
                Assert.True(pending.IsCanceled);
            }
            Assert.Single(sender.Calls);

            if (outcome is 0 or 1)
            {
                Assert.False(forwarded.IsCancellationRequested);
                await caller.CancelAsync();
                await lease.CancelAsync();
                // A completed success OR fault must dispose the operation's link to both parent tokens.
                Assert.False(forwarded.IsCancellationRequested);
            }
        }
        finally
        {
            sender.ReleaseSuccess();
            try
            {
                await ObserveOwnedOutcomeAsync(sender.ActualReturnedTask, forwarded, failure);
            }
            finally
            {
                await ObserveOwnedOutcomeAsync(pending, forwarded, failure);
            }
        }
    }

    private static async Task ObserveOwnedOutcomeAsync(Task? task, CancellationToken forwarded, Exception failure)
    {
        if (task is null)
            return;
        try
        {
            await task.WaitAsync(Timeout, CancellationToken.None);
        }
        catch (OperationCanceledException exception) when (task.IsCanceled && exception.CancellationToken == forwarded)
        {
        }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, failure))
        {
        }
        Assert.True(task.IsCompleted);
    }

    private sealed record SdkCall(string Operation, ServiceBusMessage? Message, DateTimeOffset? When, long? Sequence, CancellationToken Token);

    private sealed class RecordingSender : ServiceBusSender
    {
        private readonly TaskCompletionSource<long> _schedule = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _cancel = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<SdkCall> Calls { get; } = new();
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? ActualReturnedTask { get; private set; }
        public override string EntityPath => "scheduled-entity";

        public override Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(new SdkCall("schedule", message, scheduledEnqueueTime, null, cancellationToken));
            ActualReturnedTask = _schedule.Task;
            Entered.TrySetResult();
            return _schedule.Task;
        }

        public override Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(new SdkCall("cancel", null, null, sequenceNumber, cancellationToken));
            ActualReturnedTask = _cancel.Task;
            Entered.TrySetResult();
            return _cancel.Task;
        }

        public void ReleaseSuccess()
        {
            _schedule.TrySetResult(SequenceNumber);
            _cancel.TrySetResult();
        }

        public void ReleaseFault(Exception failure)
        {
            if (ReferenceEquals(ActualReturnedTask, _schedule.Task))
                _schedule.TrySetException(failure);
            else
                _cancel.TrySetException(failure);
        }

        public void ReleaseCanceled(CancellationToken token)
        {
            if (ReferenceEquals(ActualReturnedTask, _schedule.Task))
                _schedule.TrySetCanceled(token);
            else
                _cancel.TrySetCanceled(token);
        }

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class NoCallsProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class ExpectedScheduledOperationException : Exception;
}
