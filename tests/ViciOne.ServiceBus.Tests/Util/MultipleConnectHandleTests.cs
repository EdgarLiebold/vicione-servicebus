using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class MultipleConnectHandleTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-CONNECT-HANDLE-LIFETIME", "composite-awaits-each-child-exactly-once")]
    public async Task AsyncDisposal_AwaitsEachChildAndDisconnectsItExactlyOnceAsync()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var asynchronous = new AwaitableConnectHandle(release.Task);
        var synchronous = new SynchronousConnectHandle();
        var composite = new MultipleConnectHandle(asynchronous, synchronous);

        Task disposal = composite.DisposeAsync().AsTask();

        Assert.False(disposal.IsCompleted);
        Assert.Equal(1, asynchronous.AsyncDisposalCount);
        Assert.Equal(1, asynchronous.DisconnectCount);
        Assert.Equal(1, synchronous.DisconnectCount);

        release.TrySetResult();
        await disposal.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(1, asynchronous.AsyncDisposalCount);
        Assert.Equal(1, asynchronous.DisconnectCount);
        Assert.Equal(1, synchronous.DisconnectCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONNECT-HANDLE-LIFETIME", "composite-validates-all-owned-registrations")]
    public void Constructors_RejectMissingHandleCollectionsAndElements()
    {
        Assert.Equal("handles", Assert.Throws<ArgumentNullException>(() =>
            new MultipleConnectHandle((IEnumerable<ConnectHandle>)null!)).ParamName);
        Assert.Equal("handles", Assert.Throws<ArgumentNullException>(() =>
            new MultipleConnectHandle((ConnectHandle[])null!)).ParamName);
        Assert.Equal("handles", Assert.Throws<ArgumentException>(() =>
            new MultipleConnectHandle(new SynchronousConnectHandle(), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONNECT-HANDLE-LIFETIME", "synchronous-disconnect-attempts-every-child")]
    public void Disconnect_AttemptsEveryChildAndPreservesASingleFailure()
    {
        var failure = new InvalidOperationException("disconnect failed");
        var failing = new SynchronousConnectHandle(failure);
        var succeeding = new SynchronousConnectHandle();
        var composite = new MultipleConnectHandle(failing, succeeding);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(composite.Disconnect);

        Assert.Same(failure, exception);
        Assert.Equal(1, failing.DisconnectCount);
        Assert.Equal(1, succeeding.DisconnectCount);
    }

    private sealed class AwaitableConnectHandle(Task release) : ConnectHandle
    {
        public int AsyncDisposalCount { get; private set; }
        public int DisconnectCount { get; private set; }

        public void Dispose() => Disconnect();

        public void Disconnect() => DisconnectCount++;

        public async ValueTask DisposeAsync()
        {
            AsyncDisposalCount++;
            Disconnect();
            await release.ConfigureAwait(false);
        }
    }

    private sealed class SynchronousConnectHandle(Exception? failure = null) : ConnectHandle
    {
        public int DisconnectCount { get; private set; }

        public void Dispose() => Disconnect();

        public void Disconnect()
        {
            DisconnectCount++;
            if (failure != null)
                throw failure;
        }
    }
}
