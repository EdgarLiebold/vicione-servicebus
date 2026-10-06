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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-SHUTDOWN", "transport-child-stop-failure-does-not-skip-send-and-owned")]
    public async Task TransportChildStopFailure_DoesNotSkipSendAndOwnedContextsAsync(bool failConsume)
    {
        var primary = new IOException("consume child stop failed");
        var factory = new SuccessfulBorrowFactory();
        var supervisor = new TestTransportSupervisor(factory);
        var consume = new TerminalStopAgent(failConsume ? primary : null);
        var send = new TerminalStopAgent(null);
        supervisor.AddConsumeAgent(consume);
        supervisor.AddSendAgent(send);
        Task initialize = supervisor.SendAsync(Pipe.Empty<LifecycleContext>(), CancellationToken.None);
        Task? stop = null;
        try
        {
            await initialize.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.NotNull(factory.OwnedAgent);
            Assert.Equal(0, factory.OwnedAgent.StopCount);
            Assert.False(factory.OwnedAgent.Completed.IsCompleted);
            Assert.False(consume.Completed.IsCompleted);
            Assert.False(send.Completed.IsCompleted);
            stop = supervisor.StopAsync(CancellationToken.None);
            Exception? failure = await Record.ExceptionAsync(() => stop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            Assert.IsNotType<TimeoutException>(failure);
            Assert.True(stop.IsCompleted);
            Assert.Equal(1, consume.StopCount);
            Assert.True(consume.Completed.IsCompletedSuccessfully);
            if (failure is not null)
                Assert.Same(primary, failure);

            Assert.Equal(1, send.StopCount);
            Assert.Equal(1, factory.OwnedAgent.StopCount);
            Assert.True(send.Completed.IsCompletedSuccessfully);
            Assert.True(factory.OwnedAgent.Completed.IsCompletedSuccessfully);
            if (failConsume)
                Assert.Same(primary, failure);
            else
            {
                Assert.Null(failure);
                Assert.True(supervisor.Completed.IsCompletedSuccessfully);
            }
        }
        finally
        {
            try
            {
                await ObserveTerminalTransportTaskAsync(initialize);
                if (stop is not null)
                    await ObserveTerminalTransportTaskAsync(stop);
            }
            finally
            {
                try
                {
                    if (!consume.Completed.IsCompleted)
                        await consume.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                }
                finally
                {
                    try
                    {
                        if (!send.Completed.IsCompleted)
                            await send.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    }
                    finally
                    {
                        if (factory.OwnedAgent is not null && !factory.OwnedAgent.Completed.IsCompleted)
                            await factory.OwnedAgent.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                        await supervisor.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                        await Task.WhenAll(consume.Completed, send.Completed, supervisor.Completed)
                            .WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                    }
                }
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-SHUTDOWN", "transport-stop-retains-multiple-phase-failures")]
    public async Task TransportStopMultipleFailures_RetainsBothCausesAndStopsOwnedContextsAsync()
    {
        var consumeFailure = new IOException("consume phase stop failed");
        var sendFailure = new ApplicationException("send phase stop failed");
        var factory = new SuccessfulBorrowFactory();
        var supervisor = new TestTransportSupervisor(factory);
        var consume = new TerminalStopAgent(consumeFailure);
        var send = new TerminalStopAgent(sendFailure);
        supervisor.AddConsumeAgent(consume);
        supervisor.AddSendAgent(send);
        Task initialize = supervisor.SendAsync(Pipe.Empty<LifecycleContext>(), CancellationToken.None);
        Task? stop = null;
        try
        {
            await initialize.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.NotNull(factory.OwnedAgent);
            Assert.False(factory.OwnedAgent.Completed.IsCompleted);
            stop = supervisor.StopAsync(CancellationToken.None);
            Exception? observed = await Record.ExceptionAsync(() => stop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            Assert.IsNotType<TimeoutException>(observed);
            Assert.True(stop.IsFaulted);
            Assert.Equal(1, consume.StopCount);
            Assert.Equal(1, send.StopCount);
            Assert.Equal(1, factory.OwnedAgent.StopCount);
            Assert.True(consume.Completed.IsCompletedSuccessfully);
            Assert.True(send.Completed.IsCompletedSuccessfully);
            Assert.True(factory.OwnedAgent.Completed.IsCompletedSuccessfully);
            var failures = Assert.IsType<AggregateException>(observed);
            Assert.Equal(2, failures.InnerExceptions.Count);
            Assert.Same(consumeFailure, failures.InnerExceptions[0]);
            Assert.Same(sendFailure, failures.InnerExceptions[1]);
            Assert.True(supervisor.Completed.IsCompletedSuccessfully);
        }
        finally
        {
            try
            {
                await ObserveTerminalTransportTaskAsync(initialize);
                if (stop is not null)
                    await ObserveTerminalTransportTaskAsync(stop);
            }
            finally
            {
                try
                {
                    if (!consume.Completed.IsCompleted)
                        await ObserveTerminalTransportTaskAsync(consume.StopAsync(CancellationToken.None));
                }
                finally
                {
                    try
                    {
                        if (!send.Completed.IsCompleted)
                            await ObserveTerminalTransportTaskAsync(send.StopAsync(CancellationToken.None));
                    }
                    finally
                    {
                        try
                        {
                            if (factory.OwnedAgent is not null && !factory.OwnedAgent.Completed.IsCompleted)
                                await ObserveTerminalTransportTaskAsync(factory.OwnedAgent.StopAsync(CancellationToken.None));
                        }
                        finally
                        {
                            await ObserveTerminalTransportTaskAsync(supervisor.StopAsync(CancellationToken.None));
                            await Task.WhenAll(consume.Completed, send.Completed, supervisor.Completed)
                                .WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                        }
                    }
                }
            }
        }
    }

    private static async Task ObserveTerminalTransportTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private sealed class TestTransportSupervisor(IPipeContextFactory<LifecycleContext> factory)
        : ViciOne.ServiceBus.Transports.TransportPipeContextSupervisor<LifecycleContext>(factory);

    private sealed class SuccessfulBorrowFactory : IPipeContextFactory<LifecycleContext>
    {
        public RecordingOwnedAgent OwnedAgent { get; private set; } = null!;

        public IPipeContextAgent<LifecycleContext> CreateContext(ISupervisor supervisor)
        {
            OwnedAgent = new RecordingOwnedAgent(new LifecycleContext());
            supervisor.Add(OwnedAgent);
            return OwnedAgent;
        }

        public IActivePipeContextAgent<LifecycleContext> CreateActiveContext(ISupervisor supervisor,
            IPipeContextHandle<LifecycleContext> context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return supervisor.AddActiveContext(context, context.Context);
        }
    }

    private sealed class TerminalStopAgent(Exception? stopFailure) : IAgent
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int StopCount { get; private set; }
        public Task Ready => Task.CompletedTask;
        public Task Completed => _completed.Task;
        public CancellationToken Stopping => CancellationToken.None;
        public CancellationToken Stopped => CancellationToken.None;

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            StopCount++;
            _completed.TrySetResult();
            return stopFailure is null ? Task.CompletedTask : Task.FromException(stopFailure);
        }
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
