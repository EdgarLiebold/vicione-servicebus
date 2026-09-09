using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Lifecycle;

public sealed class PipeContextSupervisorShutdownTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-SHUTDOWN", "borrowed-failure-does-not-skip-owned-stop")]
    public async Task BorrowedContextStopFailure_DoesNotSkipOwnedContextShutdownAsync()
    {
        var expected = new StopFailureException();
        var factory = new CoordinatedFactory(expected);
        var supervisor = new PipeContextSupervisor<LifecycleContext>(factory);
        var pipe = new BlockingPipe();
        Task send = supervisor.SendAsync(pipe, CancellationToken.None);
        await pipe.Entered.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            StopFailureException actual = await Assert.ThrowsAsync<StopFailureException>(
                () => supervisor.StopAsync(CancellationToken.None));

            Assert.Same(expected, actual);
            Assert.Equal(1, factory.BorrowedAgent.StopCount);
            Assert.Equal(1, factory.OwnedAgent.StopCount);
            Assert.True(factory.OwnedAgent.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Stopping.IsCancellationRequested);
            Assert.False(supervisor.Stopped.IsCancellationRequested);
        }
        finally
        {
            pipe.Release();
            await send;
        }

        await supervisor.StopAsync(CancellationToken.None);

        Assert.True(supervisor.Completed.IsCompletedSuccessfully);
        Assert.True(supervisor.Stopped.IsCancellationRequested);
    }

    private sealed class CoordinatedFactory(Exception stopFailure) : IPipeContextFactory<LifecycleContext>
    {
        public RecordingOwnedAgent OwnedAgent { get; private set; } = null!;

        public FailingBorrowedAgent BorrowedAgent { get; private set; } = null!;

        public IPipeContextAgent<LifecycleContext> CreateContext(ISupervisor supervisor)
        {
            OwnedAgent = new RecordingOwnedAgent(new LifecycleContext());
            supervisor.Add(OwnedAgent);
            return OwnedAgent;
        }

        public IActivePipeContextAgent<LifecycleContext> CreateActiveContext(
            ISupervisor supervisor,
            IPipeContextHandle<LifecycleContext> context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BorrowedAgent = new FailingBorrowedAgent(context.Context, stopFailure);
            supervisor.Add(BorrowedAgent);
            return BorrowedAgent;
        }
    }

    private sealed class RecordingOwnedAgent(LifecycleContext context) : IPipeContextAgent<LifecycleContext>
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _stopCount;

        public int StopCount => Volatile.Read(ref _stopCount);

        public bool IsDisposed => Completed.IsCompleted;

        public Task<LifecycleContext> Context { get; } = Task.FromResult(context);

        public Task Ready => Task.CompletedTask;

        public Task Completed => _completed.Task;

        public CancellationToken Stopping => CancellationToken.None;

        public CancellationToken Stopped => CancellationToken.None;

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _stopCount);
            _completed.TrySetResult();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _completed.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingBorrowedAgent(Task<LifecycleContext> context, Exception stopFailure) : IActivePipeContextAgent<LifecycleContext>
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeCount;
        private int _stopCount;

        public int StopCount => Volatile.Read(ref _stopCount);

        public bool IsDisposed => Volatile.Read(ref _disposeCount) != 0;

        public Task<LifecycleContext> Context { get; } = context;

        public Task Ready => Task.CompletedTask;

        public Task Completed => _completed.Task;

        public CancellationToken Stopping => CancellationToken.None;

        public CancellationToken Stopped => CancellationToken.None;

        public Task FaultedAsync(Exception exception) => Task.CompletedTask;

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _stopCount);
            _completed.TrySetResult();
            return Task.FromException(stopFailure);
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingPipe : IPipe<LifecycleContext>
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;

        public async Task SendAsync(LifecycleContext context)
        {
            _entered.TrySetResult();
            await _release.Task.ConfigureAwait(false);
        }

        public void Probe(ProbeContext context)
        {
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed class LifecycleContext : BasePipeContext;

    private sealed class StopFailureException : Exception;
}
