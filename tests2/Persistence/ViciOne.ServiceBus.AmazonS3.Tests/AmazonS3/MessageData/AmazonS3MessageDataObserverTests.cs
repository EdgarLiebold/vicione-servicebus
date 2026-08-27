namespace ViciOne.ServiceBus.AmazonS3.Tests.AmazonS3.MessageData;

using System.Reflection;
using global::Amazon.S3;
using global::Amazon.S3.Model;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonS3MessageDataObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-S3-OBSERVER", "lifecycle-no-op-and-prestart-fail-fast-boundary")]
    public async Task BusObserverLifecycle_IsNoOpExceptFailFastPreStart()
    {
        var client = DispatchProxy.Create<IAmazonS3, FailingS3DispatchProxy>();
        var clientProxy = (FailingS3DispatchProxy)(object)client;
        var startupFailure = new InvalidOperationException("causal S3 startup failure");
        clientProxy.Failure = startupFailure;
        var repository = new AmazonS3MessageDataRepository(
            client,
            new AmazonS3MessageDataRepositoryOptions("observer-message-data"));
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        var failure = new InvalidOperationException("causal observer failure");

        repository.PostCreate(bus);
        repository.CreateFaulted(failure);
        await repository.PostStart(bus, Task.FromResult<BusReady>(null!));
        await repository.StartFaulted(bus, failure);
        await repository.PreStop(bus);
        await repository.PostStop(bus);
        await repository.StopFaulted(bus, failure);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.PreStart(bus));
        Assert.Same(startupFailure, actual);
        Assert.Equal(CancellationToken.None, clientProxy.ObservedCancellationToken);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancellationFailure = new OperationCanceledException(cancellation.Token);
        clientProxy.Failure = cancellationFailure;
        OperationCanceledException actualCancellation =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => repository.EnsureReady(cancellation.Token));
        Assert.Same(cancellationFailure, actualCancellation);
        Assert.Equal(cancellation.Token, clientProxy.ObservedCancellationToken);

        Assert.Throws<ArgumentNullException>(() => repository.PostCreate(null!));
        Assert.Throws<ArgumentNullException>(() => repository.CreateFaulted(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PreStart(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.PostStart(null!, Task.FromResult<BusReady>(null!)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PostStart(bus, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StartFaulted(bus, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PreStop(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.PostStop(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.StopFaulted(bus, null!));
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The no-op observer boundary must not invoke the bus.");
    }

    private class FailingS3DispatchProxy : DispatchProxy
    {
        public Exception Failure { get; set; } = null!;

        public CancellationToken ObservedCancellationToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(IAmazonS3.GetBucketAclAsync), targetMethod?.Name);
            ObservedCancellationToken = Assert.IsType<CancellationToken>(args![1]);
            return Task.FromException<GetBucketAclResponse>(Failure);
        }
    }
}
