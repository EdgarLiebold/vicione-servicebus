using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Receiving;

public sealed class PendingReceiveSettlementBoundaryTests
{
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData(Operation.Validate)]
    [InlineData(Operation.Complete)]
    [InlineData(Operation.Fault)]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "unsafe-base-exception-still-tries-fallback")]
    public async Task UnsafeBaseException_DoesNotPreventSettlementFallbackAsync(Operation operation)
    {
        using var firstContext = CreateReceiveContext();
        using var secondContext = CreateReceiveContext();
        var failure = new UnsafeBaseException();
        var calls = new List<int>();
        var pending = new PendingReceiveLockContext();
        Assert.True(pending.Enqueue(firstContext, new CallbackLock((_, _, _) =>
        {
            calls.Add(1);
            return Task.FromException(failure);
        })));
        Assert.False(pending.Enqueue(secondContext, new CallbackLock((_, _, _) =>
        {
            calls.Add(2);
            return Task.CompletedTask;
        })));

        await InvokeAsync(pending, operation, new InvalidOperationException("consumer failure"), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 1, 2 }, calls);
        Assert.Equal(1, failure.BaseLookupCount);
        Assert.Equal(operation != Operation.Validate, pending.IsEmpty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "exhausted-unsafe-base-failure-preserves-cause-and-recovery")]
    public async Task UnsafeFinalFailure_ExhaustsFallbacksAndPreservesTheOriginalCauseAsync(bool nullBase)
    {
        using var firstContext = CreateReceiveContext();
        using var secondContext = CreateReceiveContext();
        using var successorContext = CreateReceiveContext();
        var finalFailure = new UnsafeBaseException(nullBase);
        var calls = new List<int>();
        var pending = new PendingReceiveLockContext();
        Assert.True(pending.Enqueue(firstContext, new CallbackLock((_, _, _) =>
        {
            calls.Add(1);
            return Task.FromException(new InvalidOperationException("stale first lock"));
        })));
        Assert.False(pending.Enqueue(secondContext, new CallbackLock((_, _, _) =>
        {
            calls.Add(2);
            return Task.FromException(finalFailure);
        })));

        Exception actual = await Assert.ThrowsAnyAsync<Exception>(() => pending.CompleteAsync(TestContext.Current.CancellationToken));

        Assert.Same(finalFailure, actual);
        Assert.Equal(new[] { 1, 2 }, calls);
        Assert.True(pending.IsEmpty);
        Assert.True(pending.Enqueue(successorContext, new CallbackLock((_, _, _) => Task.CompletedTask)));
        await pending.CompleteAsync(TestContext.Current.CancellationToken);
        Assert.True(pending.IsEmpty);
    }

    [Theory]
    [InlineData(Operation.Validate, false)]
    [InlineData(Operation.Complete, false)]
    [InlineData(Operation.Fault, false)]
    [InlineData(Operation.Validate, true)]
    [InlineData(Operation.Complete, true)]
    [InlineData(Operation.Fault, true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "exhausted-fifo-fallback-preserves-final-cause-and-successor")]
    public async Task FailedFallbacks_PreserveFinalFailureAndAllowANewDeliveryAsync(Operation operation, bool aggregateWrapper)
    {
        using var firstContext = CreateReceiveContext();
        using var secondContext = CreateReceiveContext();
        using var thirdContext = CreateReceiveContext();
        using var successorContext = CreateReceiveContext();
        using var caller = new CancellationTokenSource();
        var originalFailure = new InvalidOperationException("consumer failure");
        var failures = new[]
        {
            new InvalidOperationException("first stale delivery"),
            new InvalidOperationException("second stale delivery"),
            new InvalidOperationException("last stale delivery")
        };
        var calls = new List<(int Delivery, Operation Operation, Exception? Failure, CancellationToken Token)>();
        var wrappers = failures.Select(failure => new Exception("provider wrapper", failure)).ToArray();
        var pending = new PendingReceiveLockContext();
        Assert.True(pending.Enqueue(firstContext, Lock(0)));
        Assert.False(pending.Enqueue(secondContext, Lock(1)));
        Assert.False(pending.Enqueue(thirdContext, Lock(2)));

        Exception actual = await Assert.ThrowsAnyAsync<Exception>(() =>
            InvokeAsync(pending, operation, originalFailure, caller.Token));

        Assert.Same(aggregateWrapper ? wrappers[2] : failures[2], actual);
        Assert.Equal(new[] { 0, 1, 2 }, calls.Select(x => x.Delivery));
        Assert.All(calls, call =>
        {
            Assert.Equal(operation, call.Operation);
            Assert.Equal(caller.Token, call.Token);
            Assert.Same(operation == Operation.Fault ? originalFailure : null, call.Failure);
        });
        Assert.True(pending.IsEmpty);

        var successorCalls = new List<Operation>();
        Assert.True(pending.Enqueue(successorContext, new CallbackLock((next, _, _) =>
        {
            successorCalls.Add(next);
            return Task.CompletedTask;
        })));
        await pending.ValidateLockStatusAsync(caller.Token);
        Assert.False(pending.IsEmpty);
        await pending.CompleteAsync(caller.Token);
        await pending.CompleteAsync(caller.Token);

        Assert.Equal(new[] { Operation.Validate, Operation.Complete }, successorCalls);
        Assert.Equal(3, calls.Count);
        Assert.True(pending.IsEmpty);

        CallbackLock Lock(int index) => new((next, failure, token) =>
        {
            calls.Add((index, next, failure, token));
            return Task.FromException(aggregateWrapper ? new AggregateException(wrappers[index]) : wrappers[index]);
        });
    }

    [Theory]
    [InlineData(Operation.Validate)]
    [InlineData(Operation.Complete)]
    [InlineData(Operation.Fault)]
    [RequirementCoverage("REQ-VSB-RECEIVE-LOCK", "canceled-waiter-retains-admitted-owner")]
    public async Task CanceledWaiter_DoesNotConsumeAnotherDeliverySettlementAsync(Operation waitingOperation)
    {
        using var context = CreateReceiveContext();
        using var caller = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operations = new List<Operation>();
        var pending = new PendingReceiveLockContext();
        Assert.True(pending.Enqueue(context, new CallbackLock(async (operation, _, token) =>
        {
            operations.Add(operation);
            Assert.Equal(TestContext.Current.CancellationToken, token);
            entered.TrySetResult();
            await release.Task;
        })));

        Task owner = pending.CompleteAsync(TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Task waiter = InvokeAsync(pending, waitingOperation, new InvalidOperationException("waiting fault"), caller.Token);
            Assert.False(waiter.IsCompleted);
            caller.Cancel();

            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                waiter.WaitAsync(Timeout, TestContext.Current.CancellationToken));

            Assert.Equal(caller.Token, canceled.CancellationToken);
            Assert.Equal(TaskStatus.Canceled, waiter.Status);
            Assert.False(owner.IsCompleted);
            Assert.False(pending.IsEmpty);
            Assert.Equal(new[] { Operation.Complete }, operations);
        }
        finally
        {
            release.TrySetResult();
            await owner.WaitAsync(Timeout, CancellationToken.None);
        }

        Assert.True(pending.IsEmpty);
        await pending.CompleteAsync(TestContext.Current.CancellationToken);
        Assert.Equal(new[] { Operation.Complete }, operations);
    }

    private static Task InvokeAsync(ReceiveLockContext context, Operation operation, Exception failure, CancellationToken token) => operation switch
    {
        Operation.Validate => context.ValidateLockStatusAsync(token),
        Operation.Complete => context.CompleteAsync(token),
        Operation.Fault => context.FaultedAsync(failure, token),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    private static ReceiveEndpointDispatcherReceiveContext CreateReceiveContext() => new(
        new ReceiveMessageLimitsTestContext(
            new MessageLimits { MaxBodyBytes = 1024, MaxEnvelopeBytes = 1024, MaxJsonDepth = 16 },
            new Uri("loopback://settlement/input")),
        [], new Dictionary<string, object>());

    public enum Operation
    {
        Validate,
        Complete,
        Fault
    }

    private sealed class CallbackLock(Func<Operation, Exception?, CancellationToken, Task> callback) : ReceiveLockContext
    {
        public Task CompleteAsync(CancellationToken cancellationToken = default) => callback(Operation.Complete, null, cancellationToken);
        public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default) => callback(Operation.Fault, exception, cancellationToken);
        public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default) => callback(Operation.Validate, null, cancellationToken);
    }

    private sealed class UnsafeBaseException(bool nullBase = false) : Exception
    {
        public int BaseLookupCount { get; private set; }

        public override Exception GetBaseException()
        {
            BaseLookupCount++;
            return nullBase ? null! : throw new InvalidOperationException("base lookup failed");
        }
    }
}
