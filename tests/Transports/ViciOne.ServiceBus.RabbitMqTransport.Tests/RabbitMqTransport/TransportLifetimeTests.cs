using System.Runtime.CompilerServices;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport;

public sealed class TransportLifetimeTests
{
    [Theory]
    [InlineData("channel")]
    [InlineData("connection")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TRANSPORT-LIFETIME", "lease-invalidation-and-exact-disposal")]
    public async Task Invalidation_RefusesNewLeasesPreservesTheBrokerReasonAndDisposesExactlyOnceAsync(
        string subject)
    {
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondLeaseReleased = 0;
        var disposedBeforeLastRelease = 0;
        var disposeCount = 0;
        var scheduled = new TaskCompletionSource<Func<Task>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lifetime = new TransportLifetime(subject, () =>
        {
            if (Volatile.Read(ref secondLeaseReleased) == 0)
                Interlocked.Exchange(ref disposedBeforeLastRelease, 1);

            Interlocked.Increment(ref disposeCount);
            disposed.TrySetResult();
            return Task.CompletedTask;
        }, dispose => Assert.True(scheduled.TrySetResult(dispose), "Disposal was scheduled more than once."));

        Assert.True(lifetime.TryLease(out TransportLifetime.Lease? first));
        Assert.True(lifetime.TryLease(out TransportLifetime.Lease? second));

        ShutdownEventArgs reason = Refused();
        lifetime.Invalidate(reason);
        lifetime.Invalidate(new ShutdownEventArgs(ShutdownInitiator.Library, 491, "later reason"));

        Assert.False(lifetime.TryLease(out _));
        Assert.Same(reason, lifetime.CloseReason);
        Assert.Same(reason, lifetime.NotAvailable().ShutdownReason);

        first!.Dispose();
        first.Dispose();
        Assert.False(scheduled.Task.IsCompleted);
        Interlocked.Exchange(ref secondLeaseReleased, 1);
        second!.Dispose();
        Func<Task> dispose = await scheduled.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        await dispose().WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        await disposed.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        await lifetime.DisposeAsync();
        await lifetime.DisposeAsync();

        Assert.Equal(0, Volatile.Read(ref disposedBeforeLastRelease));
        Assert.Equal(1, Volatile.Read(ref disposeCount));
    }

    [Theory]
    [InlineData("channel")]
    [InlineData("connection")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TRANSPORT-LIFETIME", "local-close-reason")]
    public async Task OwnerDisposal_UsesTheLocalReasonAndWaitsForTheLastIdempotentLeaseAsync(string subject)
    {
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lifetime = new TransportLifetime(subject, () =>
        {
            disposed.TrySetResult();
            return Task.CompletedTask;
        });

        Assert.True(lifetime.TryLease(out TransportLifetime.Lease? lease));

        Task ownerDisposal = lifetime.DisposeAsync().AsTask();
        RabbitMQ.Client.Exceptions.OperationInterruptedException refusal = lifetime.NotAvailable();
        ShutdownEventArgs localReason = Assert.IsType<ShutdownEventArgs>(refusal.ShutdownReason);

        Assert.Equal(ShutdownInitiator.Library, localReason.Initiator);
        Assert.Equal((ushort)491, localReason.ReplyCode);
        Assert.Contains(subject, localReason.ReplyText, StringComparison.Ordinal);

        lease!.Dispose();
        lease.Dispose();

        await disposed.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        await ownerDisposal.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("channel")]
    [InlineData("connection")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TRANSPORT-LIFETIME", "disposal-failure-identity-and-stack")]
    public async Task DisposalFailure_PreservesItsExactInstanceAndOriginalStackAsync(string subject)
    {
        var failure = new InvalidOperationException("the socket was already gone");
        var lifetime = new TransportLifetime(subject, () => ThrowDisposalFailureAsync(failure));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => lifetime.DisposeAsync().AsTask());

        Assert.Same(failure, actual);
        Assert.Contains(nameof(ThrowDisposalFailureAsync), actual.StackTrace, StringComparison.Ordinal);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static ShutdownEventArgs Refused() => new(
        ShutdownInitiator.Peer,
        405,
        "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue",
        50,
        10);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ThrowDisposalFailureAsync(Exception failure)
    {
        await Task.Yield();
        throw failure;
    }
}
