using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Lifecycle;

public sealed class PipeContextSupervisorOwnedStopAdmissionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-PIPE-CONTEXT-SHUTDOWN", "synchronous-owned-stop-error-admits-and-drains-captured-work")]
    public async Task StopAsync_SynchronousOwnedFailureStillAdmitsAndDrainsCapturedWorkAsync(bool failSecond)
    {
        var primary = new IOException("second owned stop rejected synchronously");
        var coordinator = new StopCoordinator(failSecond, primary);
        var factory = new TrackingFactory(coordinator);
        var supervisor = new PipeContextSupervisor<FixtureContext>(factory);
        var pipe = new HeldPipe();
        Task? send = null;
        Task? stop = null;

        try
        {
            send = supervisor.SendAsync(pipe, CancellationToken.None);
            await JoinAsync(pipe.Entered);
            await JoinAsync(supervisor.Ready);
            Assert.Equal(3, supervisor.TotalCount);
            Assert.Equal(3, factory.Owned.Count);
            Assert.Equal(1, factory.ActiveCreationCount);
            Assert.Same(factory.Context, pipe.ObservedContext);
            Assert.All(factory.Owned, child => Assert.False(child.Completed.IsCompleted));

            stop = supervisor.StopAsync(CancellationToken.None);
            await JoinAsync(factory.Borrowed.StopEntered);
            Task first = await Task.WhenAny(stop, coordinator.ThirdAdmission)
                .WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.True(ReferenceEquals(first, stop) || ReferenceEquals(first, coordinator.ThirdAdmission));
            Assert.Equal(1, factory.Borrowed.StopCalls);
            Task borrowedRaw = Assert.Single(factory.Borrowed.RawStops);
            Task firstOwnedRaw = coordinator.FirstRawStop
                ?? throw new InvalidOperationException("The first owned stop was not admitted.");
            Assert.False(borrowedRaw.IsCompleted);
            Assert.False(firstOwnedRaw.IsCompleted);
            Assert.False(factory.Borrowed.Completed.IsCompleted);
            if (failSecond)
            {
                Assert.Equal(1, coordinator.SynchronousFailures);
                Assert.Same(primary, coordinator.LastFailure);
            }
            else
                Assert.Equal(0, coordinator.SynchronousFailures);

            if (stop.IsCompleted)
            {
                Exception? premature = await ObserveAsync(stop);
                if (failSecond)
                    Assert.Same(primary, premature);
                else
                    Assert.Null(premature);
            }

            // This is observed before any fixture fallback or release.
            Assert.Equal(3, coordinator.StopAdmissions);
            Assert.False(stop.IsCompleted);

            coordinator.Release();
            factory.Borrowed.Release();
            pipe.Release();
            foreach (OwnedAgent child in factory.Owned)
                foreach (Task raw in child.RawStops)
                    await JoinAsync(raw);
            await JoinAsync(borrowedRaw);
            await JoinAsync(send);
            Exception? failure = await ObserveAsync(stop);
            if (failSecond)
            {
                Assert.Same(primary, failure);
                Assert.False(supervisor.Stopped.IsCancellationRequested);
            }
            else
            {
                Assert.Null(failure);
                Assert.True(supervisor.Stopped.IsCancellationRequested);
            }
            await JoinAsync(supervisor.Completed);
            Assert.All(factory.Owned, child => Assert.True(child.Completed.IsCompletedSuccessfully));
            Assert.True(factory.Borrowed.Completed.IsCompletedSuccessfully);
        }
        finally
        {
            coordinator.DisableFailureAndRelease();
            factory.Borrowed.Release();
            pipe.Release();
            var cleanupFailures = new List<Exception>();
            // Every actual retained operation is joined independently, even after an earlier failure.
            foreach (OwnedAgent child in factory.Owned)
            {
                foreach (Task raw in child.RawStops)
                    await CleanupAsync(raw, null, cleanupFailures);
                if (!child.Completed.IsCompleted)
                {
                    Task? fallback = null;
                    try { fallback = child.StopAsync(new FixtureStopContext(), CancellationToken.None); }
                    catch (Exception exception) { cleanupFailures.Add(exception); }
                    if (fallback is not null)
                        await CleanupAsync(fallback, null, cleanupFailures);
                }
                await CleanupAsync(child.Completed, null, cleanupFailures);
                await CleanupAsync(child.Ready, null, cleanupFailures);
                await CleanupAsync(child.Context, null, cleanupFailures);
                try { await child.DisposeAsync(); }
                catch (Exception exception) { cleanupFailures.Add(exception); }
            }
            foreach (Task raw in factory.Borrowed.RawStops)
                await CleanupAsync(raw, null, cleanupFailures);
            if (send is not null)
                await CleanupAsync(send, null, cleanupFailures);
            foreach (Task raw in factory.Borrowed.RawStops)
                await CleanupAsync(raw, null, cleanupFailures);
            if (stop is not null)
                await CleanupAsync(stop, failSecond ? primary : null, cleanupFailures);
            Task? retry = null;
            try { retry = supervisor.StopAsync(CancellationToken.None); }
            catch (Exception exception) { cleanupFailures.Add(exception); }
            if (retry is not null)
                await CleanupAsync(retry, null, cleanupFailures);
            await CleanupAsync(supervisor.Completed, null, cleanupFailures);
            if (factory.ActiveCreationCount != 0)
            {
                await CleanupAsync(factory.Borrowed.Completed, null, cleanupFailures);
                await CleanupAsync(factory.Borrowed.Ready, null, cleanupFailures);
                await CleanupAsync(factory.Borrowed.Context, null, cleanupFailures);
                try { await factory.Borrowed.DisposeAsync(); }
                catch (Exception exception) { cleanupFailures.Add(exception); }
            }
            if (cleanupFailures.Count == 1)
                ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
            if (cleanupFailures.Count > 1)
                throw new AggregateException(cleanupFailures);
        }
    }

    private static Task JoinAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);

    private static async Task<Exception?> ObserveAsync(Task task)
    {
        try { await JoinAsync(task); return null; }
        catch (Exception exception) when (task.IsCompleted && exception is not TimeoutException) { return exception; }
    }

    private static async Task CleanupAsync(Task task, Exception? expected, List<Exception> failures)
    {
        try { await JoinAsync(task); }
        catch (Exception exception) when (task.IsCompleted && ReferenceEquals(exception, expected)) { }
        catch (Exception exception) { failures.Add(exception); }
    }

    private sealed class StopCoordinator(bool failSecond, IOException primary)
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _third = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _admissions;
        private int _failEnabled = failSecond ? 1 : 0;
        private int _failures;
        public int StopAdmissions => Volatile.Read(ref _admissions);
        public int SynchronousFailures => Volatile.Read(ref _failures);
        public Exception? LastFailure { get; private set; }
        public Task? FirstRawStop { get; private set; }
        public Task ThirdAdmission => _third.Task;
        public Task ReleaseTask => _release.Task;
        public int Admit() => Interlocked.Increment(ref _admissions);
        public bool Reject(int ordinal)
        {
            if (ordinal != 2 || Volatile.Read(ref _failEnabled) == 0)
                return false;
            LastFailure = primary;
            Interlocked.Increment(ref _failures);
            return true;
        }
        public IOException Primary => primary;
        public void RecordRaw(int ordinal, Task task)
        {
            if (ordinal == 1) FirstRawStop = task;
            if (ordinal == 3) _third.TrySetResult();
        }
        public void Release() => _release.TrySetResult();
        public void DisableFailureAndRelease() { Volatile.Write(ref _failEnabled, 0); Release(); }
    }

    private sealed class OwnedAgent(FixtureContext context, StopCoordinator coordinator) : IPipeContextAgent<FixtureContext>
    {
        private readonly object _lock = new();
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _stop;
        private int _disposed;
        public Task<FixtureContext> Context { get; } = Task.FromResult(context);
        public Task Ready => Task.CompletedTask;
        public Task Completed => _completed.Task;
        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
        public CancellationToken Stopping => CancellationToken.None;
        public CancellationToken Stopped => CancellationToken.None;
        public IReadOnlyList<Task> RawStops { get { lock (_lock) return _stop is null ? [] : [_stop]; } }
        public Task StopAsync(StopContext stopContext, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stopContext);
            cancellationToken.ThrowIfCancellationRequested();
            lock (_lock)
            {
                if (_stop is not null) return _stop;
                if (_completed.Task.IsCompletedSuccessfully) return Task.CompletedTask;
                int ordinal = coordinator.Admit();
                if (coordinator.Reject(ordinal))
                {
                    _completed.TrySetResult();
                    throw coordinator.Primary;
                }
                _stop = RunStopAsync();
                coordinator.RecordRaw(ordinal, _stop);
                return _stop;
            }
        }
        private async Task RunStopAsync() { await coordinator.ReleaseTask.ConfigureAwait(false); _completed.TrySetResult(); }
        public ValueTask DisposeAsync() { Interlocked.Exchange(ref _disposed, 1); return ValueTask.CompletedTask; }
    }

    private sealed class BorrowedAgent : IActivePipeContextAgent<FixtureContext>
    {
        private readonly object _lock = new();
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _stop;
        private int _stopCalls;
        private int _disposed;
        public Task<FixtureContext> Context { get; set; } = null!;
        public Task Ready => Task.CompletedTask;
        public Task Completed => _completed.Task;
        public Task StopEntered => _entered.Task;
        public int StopCalls => Volatile.Read(ref _stopCalls);
        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
        public CancellationToken Stopping => CancellationToken.None;
        public CancellationToken Stopped => CancellationToken.None;
        public IReadOnlyList<Task> RawStops { get { lock (_lock) return _stop is null ? [] : [_stop]; } }
        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            lock (_lock)
            {
                if (_stop is not null) return _stop;
                Interlocked.Increment(ref _stopCalls);
                _stop = RunStopAsync();
                _entered.TrySetResult();
                return _stop;
            }
        }
        private async Task RunStopAsync() { await _release.Task.ConfigureAwait(false); _completed.TrySetResult(); }
        public Task FaultedAsync(Exception exception) { ArgumentNullException.ThrowIfNull(exception); return Task.CompletedTask; }
        // Bookkeeping must not complete the independently held Stop operation or lifecycle.
        public ValueTask DisposeAsync() { Interlocked.Exchange(ref _disposed, 1); return ValueTask.CompletedTask; }
        public void Release() => _release.TrySetResult();
    }

    private sealed class TrackingFactory(StopCoordinator coordinator) : IPipeContextFactory<FixtureContext>
    {
        public FixtureContext Context { get; } = new();
        public List<OwnedAgent> Owned { get; } = [];
        public BorrowedAgent Borrowed { get; } = new();
        public int ActiveCreationCount { get; private set; }
        public IPipeContextAgent<FixtureContext> CreateContext(ISupervisor supervisor)
        {
            for (int i = 0; i < 3; i++)
            {
                var child = new OwnedAgent(Context, coordinator);
                Owned.Add(child);
                supervisor.Add(child);
            }
            return Owned[0];
        }
        public IActivePipeContextAgent<FixtureContext> CreateActiveContext(ISupervisor supervisor,
            IPipeContextHandle<FixtureContext> context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Borrowed.Context = context.Context;
            supervisor.Add(Borrowed);
            ActiveCreationCount++;
            return Borrowed;
        }
    }

    private sealed class HeldPipe : IPipe<FixtureContext>
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Entered => _entered.Task;
        public FixtureContext? ObservedContext { get; private set; }
        public async Task SendAsync(FixtureContext context)
        {
            ObservedContext = context;
            _entered.TrySetResult();
            await _release.Task.ConfigureAwait(false);
        }
        public void Probe(ProbeContext context) { ArgumentNullException.ThrowIfNull(context); }
        public void Release() => _release.TrySetResult();
    }

    private sealed class FixtureContext : BasePipeContext;
    private sealed class FixtureStopContext : BasePipeContext, StopContext
    {
        public string Reason => "Independent fixture cleanup";
    }
}
