using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusDeadLetterStateTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    public static IEnumerable<object[]> Cases =>
        from session in new[] { false, true }
        from withException in new[] { false, true }
        from outcome in new[] { 0, 1, 2 }
        select new object[] { session, withException, outcome };

    [Theory]
    [MemberData(nameof(Cases))]
    [RequirementCoverage("REQ-VSB-ASB-SETTLEMENT-CANCELLATION", "deadletter-success-alone-suppresses-later-settlement")]
    public async Task DeadLetterOutcome_OnlySuccessSuppressesLaterCompleteAndAbandonAsync(bool session, bool withException, int outcome)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        using var processor = new CancellationTokenSource();
        using var completionToken = new CancellationTokenSource();
        var probe = new SettlementProbe();
        await using ServiceBusReceiver receiver = session
            ? new RecordingSessionReceiver(probe)
            : new RecordingReceiver(probe);
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("delivery-state"), messageId: "message-29", sessionId: session ? "session-17" : null,
            lockedUntil: DateTimeOffset.UtcNow.AddMinutes(5));
        ProcessMessageEventArgs? messageArgs = session ? null : new ProcessMessageEventArgs(message, receiver, processor.Token);
        // SDK7.20.2 public mock constructor allocates an internal expiry CTS; dispose our fixture-owned resource.
        using CancellationTokenSource? sdkLockCancellation = messageArgs is null ? null : ReadSdkLockCancellationSource(messageArgs);
        MessageLockContext context = session
            ? new ServiceBusSessionMessageLockContext(new ProcessSessionMessageEventArgs(message, (ServiceBusSessionReceiver)receiver, processor.Token), message, processor.Token)
            : new ServiceBusMessageLockContext(messageArgs!, message, processor.Token);
        var processingFailure = new InvalidOperationException("receive pipeline failed");
        var sdkFailure = new ExpectedSettlementException();
        Task? pending = null;
        var following = new List<Task>();
        try
        {
            pending = withException
                ? context.DeadLetterAsync(processingFailure, caller.Token)
                : context.DeadLetterAsync(caller.Token);
            await Task.WhenAny(probe.Entered.Task, pending).WaitAsync(Timeout, TestToken);
            if (!probe.Entered.Task.IsCompleted)
                await pending.WaitAsync(Timeout, TestToken);
            Assert.True(probe.Entered.Task.IsCompleted);
            Assert.False(pending.IsCompleted);
            Assert.False(probe.DeadLetterTask.IsCompleted);
            SettlementCall call = Assert.Single(probe.Calls);
            Assert.Equal("deadletter", call.Operation);
            Assert.Same(message, call.Message);
            Assert.Equal(processor.Token, call.Token);
            Assert.NotEqual(caller.Token, call.Token);
            Assert.NotNull(call.Properties);
            Assert.Equal(withException ? "fault" : "dead-letter", Assert.IsType<string>(call.Properties[MessageHeaders.Reason]));
            if (withException)
                Assert.Equal(processingFailure.Message, Assert.IsType<string>(call.Properties[MessageHeaders.FaultMessage]));
            else
                Assert.Single(call.Properties);
            Assert.Equal(session ? withException ? "fault" : "dead-letter" : null, call.Reason);
            Assert.Equal(session && withException ? processingFailure.Message : null, call.Description);

            if (outcome == 0)
            {
                probe.ReleaseSuccess();
                await pending.WaitAsync(Timeout, TestToken);
                Assert.True(pending.IsCompletedSuccessfully);
            }
            else if (outcome == 1)
            {
                probe.ReleaseFault(sdkFailure);
                ExpectedSettlementException observed = await Assert.ThrowsAsync<ExpectedSettlementException>(() => pending.WaitAsync(Timeout, TestToken));
                Assert.Same(sdkFailure, observed);
                Assert.True(pending.IsFaulted);
            }
            else
            {
                Assert.Equal(2, outcome);
                await completionToken.CancelAsync();
                probe.ReleaseCanceled(completionToken.Token);
                OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(Timeout, TestToken));
                Assert.Equal(completionToken.Token, observed.CancellationToken);
                Assert.True(pending.IsCanceled);
            }
            Assert.Single(probe.Calls);

            following.Add(context.CompleteAsync(caller.Token));
            following.Add(context.AbandonAsync(processingFailure, caller.Token));
            await Task.WhenAll(following).WaitAsync(Timeout, TestToken);
            SettlementCall[] calls = probe.Calls.ToArray();
            if (outcome == 0)
                Assert.Single(calls);
            else
            {
                // State is still eligible after failure/cancellation: both REAL SDK boundaries are reached.
                Assert.Equal(new[] { "deadletter", "complete", "abandon" }, calls.Select(record => record.Operation).ToArray());
                Assert.All(calls.Skip(1), record =>
                {
                    Assert.Same(message, record.Message);
                    Assert.Equal(processor.Token, record.Token);
                });
                Assert.Equal(processingFailure.Message, Assert.IsType<string>(calls[2].Properties![MessageHeaders.FaultMessage]));
            }
        }
        finally
        {
            probe.ReleaseSuccess();
            try
            {
                await ObserveOwnedOutcomeAsync(probe.DeadLetterTask, sdkFailure, completionToken.Token);
            }
            finally
            {
                try
                {
                    await ObserveOwnedOutcomeAsync(pending, sdkFailure, completionToken.Token);
                }
                finally
                {
                    await Task.WhenAll(following).WaitAsync(Timeout, CancellationToken.None);
                }
            }
        }
    }

    private static CancellationTokenSource ReadSdkLockCancellationSource(ProcessMessageEventArgs args)
    {
        PropertyInfo? property = typeof(ProcessMessageEventArgs).GetProperty(
            "MessageLockLostCancellationSource", BindingFlags.Instance | BindingFlags.NonPublic);
        return (CancellationTokenSource)(property?.GetValue(args)
            ?? throw new InvalidOperationException("The pinned SDK callback expiry source was not available for fixture cleanup."));
    }

    private static async Task ObserveOwnedOutcomeAsync(Task? task, Exception expectedFailure, CancellationToken expectedCancellation)
    {
        if (task is null)
            return;
        try
        {
            await task.WaitAsync(Timeout, CancellationToken.None);
        }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, expectedFailure))
        {
        }
        catch (OperationCanceledException exception) when (task.IsCanceled && exception.CancellationToken == expectedCancellation)
        {
        }
        Assert.True(task.IsCompleted);
    }

    private sealed record SettlementCall(
        string Operation, ServiceBusReceivedMessage Message, CancellationToken Token,
        IDictionary<string, object>? Properties = null, string? Reason = null, string? Description = null);

    private sealed class SettlementProbe
    {
        private readonly TaskCompletionSource _deadLetter = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<SettlementCall> Calls { get; } = new();
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task DeadLetterTask => _deadLetter.Task;

        public Task RecordAsync(string operation, ServiceBusReceivedMessage message, CancellationToken token,
            IDictionary<string, object>? properties = null, string? reason = null, string? description = null)
        {
            Calls.Enqueue(new SettlementCall(operation, message, token,
                properties is null ? null : new Dictionary<string, object>(properties), reason, description));
            if (operation != "deadletter")
                return Task.CompletedTask;
            Entered.TrySetResult();
            return _deadLetter.Task;
        }

        public void ReleaseSuccess() => _deadLetter.TrySetResult();
        public void ReleaseFault(Exception failure) => _deadLetter.TrySetException(failure);
        public void ReleaseCanceled(CancellationToken token) => _deadLetter.TrySetCanceled(token);
    }

    private sealed class RecordingReceiver(SettlementProbe probe) : ServiceBusReceiver
    {
        public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default) =>
            probe.RecordAsync("complete", message, cancellationToken);
        public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null!, CancellationToken cancellationToken = default) =>
            probe.RecordAsync("abandon", message, cancellationToken, propertiesToModify);
        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null!, CancellationToken cancellationToken = default) =>
            probe.RecordAsync("deadletter", message, cancellationToken, propertiesToModify);
        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify,
            string deadLetterReason, string deadLetterErrorDescription = null!, CancellationToken cancellationToken = default) =>
            probe.RecordAsync("deadletter", message, cancellationToken, propertiesToModify, deadLetterReason, deadLetterErrorDescription);
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingSessionReceiver(SettlementProbe probe) : ServiceBusSessionReceiver
    {
        public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default) =>
            probe.RecordAsync("complete", message, cancellationToken);
        public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null!, CancellationToken cancellationToken = default) =>
            probe.RecordAsync("abandon", message, cancellationToken, propertiesToModify);
        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null!, CancellationToken cancellationToken = default) =>
            probe.RecordAsync("deadletter", message, cancellationToken, propertiesToModify);
        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify,
            string deadLetterReason, string deadLetterErrorDescription = null!, CancellationToken cancellationToken = default) =>
            probe.RecordAsync("deadletter", message, cancellationToken, propertiesToModify, deadLetterReason, deadLetterErrorDescription);
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ExpectedSettlementException : Exception;
}
