using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Agents;

public sealed class PipeContextSupervisorFailureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-IDEMPOTENCY", "cleanup-failure-after-success")]
    public async Task SuccessfulOperation_WhenEveryCleanupStepFails_RemainsSuccessfulAndRunsOnceAsync()
    {
        var events = new List<string>();
        var factory = new RecordingFactory(events, faultThrows: true, stopThrows: true, disposeThrows: true);
        var supervisor = new PipeContextSupervisor<LifecycleContext>(factory);
        var pipe = new RecordingPipe(events);

        await supervisor.SendAsync(pipe, TestContext.Current.CancellationToken);

        Assert.Equal(1, pipe.SendCount);
        Assert.Equal(["pipe", "stop", "dispose"], events);
        Assert.Null(factory.ActiveContext.ObservedFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-PRIMARY-FAILURE", "dispose-failure")]
    public async Task OperationFailure_WhenDisposeFails_PreservesTheExactOperationFailureAsync()
    {
        await AssertPrimaryFailureWinsAsync(disposeThrows: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-PRIMARY-FAILURE", "stop-failure")]
    public async Task OperationFailure_WhenStopFails_PreservesTheExactOperationFailureAsync()
    {
        await AssertPrimaryFailureWinsAsync(stopThrows: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-PRIMARY-FAILURE", "fault-notification-failure")]
    public async Task OperationFailure_WhenFaultNotificationFails_PreservesTheExactOperationFailureAsync()
    {
        await AssertPrimaryFailureWinsAsync(faultThrows: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-PRIMARY-FAILURE", "all-cleanup-failures")]
    public async Task OperationFailure_WhenEveryCleanupStepFails_PreservesTheExactOperationFailureAsync()
    {
        await AssertPrimaryFailureWinsAsync(faultThrows: true, stopThrows: true, disposeThrows: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-CLEANUP", "success-order-and-cancellation-token")]
    public async Task SuccessfulOperation_StopsThenDisposesWithoutReportingAFaultAsync()
    {
        var events = new List<string>();
        var factory = new RecordingFactory(events);
        var supervisor = new PipeContextSupervisor<LifecycleContext>(factory);
        var pipe = new RecordingPipe(events);
        using var cancellationTokenSource = new CancellationTokenSource();

        await supervisor.SendAsync(pipe, cancellationTokenSource.Token);

        Assert.Equal(["pipe", "stop", "dispose"], events);
        Assert.Equal(cancellationTokenSource.Token, factory.ActiveContext.StopCancellationToken);
        Assert.Null(factory.ActiveContext.ObservedFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-CLEANUP", "failure-order-and-exception-identity")]
    public async Task OperationFailure_ReportsFaultThenStopsThenDisposesAsync()
    {
        var events = new List<string>();
        var expected = new OperationFailureException();
        var factory = new RecordingFactory(events);
        var supervisor = new PipeContextSupervisor<LifecycleContext>(factory);
        var pipe = new RecordingPipe(events, expected);

        OperationFailureException actual = await Assert.ThrowsAsync<OperationFailureException>(
            () => supervisor.SendAsync(pipe, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Same(expected, factory.ActiveContext.ObservedFailure);
        Assert.Equal(["pipe", "fault", "stop", "dispose"], events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-ACQUISITION", "context-task-failure")]
    public async Task ContextAcquisitionFailure_IsReportedAndSkipsTheOperationAsync()
    {
        var events = new List<string>();
        var expected = new OperationFailureException();
        var factory = new RecordingFactory(events, Task.FromException<LifecycleContext>(expected));
        var supervisor = new PipeContextSupervisor<LifecycleContext>(factory);
        var pipe = new RecordingPipe(events);

        OperationFailureException actual = await Assert.ThrowsAsync<OperationFailureException>(
            () => supervisor.SendAsync(pipe, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Same(expected, factory.ActiveContext.ObservedFailure);
        Assert.Equal(0, pipe.SendCount);
        Assert.Equal(["fault", "stop", "dispose"], events);
    }

    private static async Task AssertPrimaryFailureWinsAsync(
        bool faultThrows = false,
        bool stopThrows = false,
        bool disposeThrows = false)
    {
        var events = new List<string>();
        var expected = new OperationFailureException();
        var factory = new RecordingFactory(events, faultThrows: faultThrows, stopThrows: stopThrows, disposeThrows: disposeThrows);
        var supervisor = new PipeContextSupervisor<LifecycleContext>(factory);
        var pipe = new RecordingPipe(events, expected);

        OperationFailureException actual = await Assert.ThrowsAsync<OperationFailureException>(
            () => supervisor.SendAsync(pipe, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(["pipe", "fault", "stop", "dispose"], events);
        Assert.Equal(1, pipe.SendCount);
        Assert.Same(expected, factory.ActiveContext.ObservedFailure);
    }

    private sealed class RecordingFactory : IPipeContextFactory<LifecycleContext>
    {
        private readonly Task<LifecycleContext> _context;
        private readonly bool _disposeThrows;
        private readonly List<string> _events;
        private readonly bool _faultThrows;
        private readonly bool _stopThrows;

        public RecordingFactory(
            List<string> events,
            Task<LifecycleContext>? context = null,
            bool faultThrows = false,
            bool stopThrows = false,
            bool disposeThrows = false)
        {
            _events = events;
            _context = context ?? Task.FromResult(new LifecycleContext());
            _faultThrows = faultThrows;
            _stopThrows = stopThrows;
            _disposeThrows = disposeThrows;
        }

        public RecordingActiveContext ActiveContext { get; private set; } = null!;

        public IPipeContextAgent<LifecycleContext> CreateContext(ISupervisor supervisor) => new PassiveContextAgent(_context);

        public IActivePipeContextAgent<LifecycleContext> CreateActiveContext(
            ISupervisor supervisor,
            PipeContextHandle<LifecycleContext> context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); ActiveContext = new RecordingActiveContext(context.Context, _events, _faultThrows, _stopThrows, _disposeThrows);
            return ActiveContext;
        }
    }

    private sealed class RecordingActiveContext(
        Task<LifecycleContext> context,
        List<string> events,
        bool faultThrows,
        bool stopThrows,
        bool disposeThrows) : IActivePipeContextAgent<LifecycleContext>
    {
        public bool IsDisposed { get; private set; }

        public Task<LifecycleContext> Context { get; } = context;

        public Task Ready => Task.CompletedTask;

        public Task Completed => Task.CompletedTask;

        public CancellationToken Stopping => CancellationToken.None;

        public CancellationToken Stopped => CancellationToken.None;

        public Exception? ObservedFailure { get; private set; }

        public CancellationToken StopCancellationToken { get; private set; }

        public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); events.Add("fault");
            ObservedFailure = exception;

            return faultThrows
                ? Task.FromException(new CleanupFailureException("Fault notification failed."))
                : Task.CompletedTask;
        }

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); events.Add("stop");
            StopCancellationToken = context.CancellationToken;

            return stopThrows
                ? Task.FromException(new CleanupFailureException("Stop failed."))
                : Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            events.Add("dispose");
            IsDisposed = true;

            return disposeThrows
                ? ValueTask.FromException(new CleanupFailureException("Dispose failed."))
                : ValueTask.CompletedTask;
        }
    }

    private sealed class PassiveContextAgent : IPipeContextAgent<LifecycleContext>
    {
        public PassiveContextAgent(Task<LifecycleContext> context)
        {
            Context = context;
            Ready = context;
        }

        public bool IsDisposed => false;

        public Task<LifecycleContext> Context { get; }

        public Task Ready { get; }

        public Task Completed => Task.CompletedTask;

        public CancellationToken Stopping => CancellationToken.None;

        public CancellationToken Stopped => CancellationToken.None;

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingPipe(List<string> events, Exception? failure = null) : IPipe<LifecycleContext>
    {
        public int SendCount { get; private set; }

        public Task SendAsync(LifecycleContext context)
        {
            events.Add("pipe");
            SendCount++;

            return failure is null
                ? Task.CompletedTask
                : Task.FromException(failure);
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class LifecycleContext : BasePipeContext;

    private sealed class OperationFailureException : Exception;

    private sealed class CleanupFailureException(string message) : Exception(message);
}
