using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transports;

public sealed class AsyncBusHandleLifecycleTestDriver : IAsyncDisposable
{
    private readonly AsyncBusHandle _handle;
    private readonly TestBusDepot _depot;
    private readonly Task _startTask;

    public AsyncBusHandleLifecycleTestDriver(
        bool blockStop = false,
        TimeSpan? stopTimeout = null,
        TimeProvider? timeProvider = null)
    {
        _depot = new TestBusDepot(blockStop);
        _handle = new AsyncBusHandle(
            _depot,
            NullLogger<AsyncBusHandle>.Instance,
            Options.Create(new ViciOneServiceBusHostOptions { StopTimeout = stopTimeout }),
            timeProvider);
        _startTask = (Task)(typeof(AsyncBusHandle)
            .GetField("_startTask", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(_handle)
            ?? throw new InvalidOperationException("The asynchronous bus handle start task was not found."));
    }

    public Task CancellationObserved => _depot.CancellationObserved;

    public Task StartEntered => _depot.StartEntered;

    public Task StartCompletion => _startTask;

    public Task StopEntered => _depot.StopEntered;

    public int StopCount => _depot.StopCount;

    public void CompleteStart() => _depot.CompleteStart();

    public void ReleaseCancellation() => _depot.ReleaseCancellation();

    public void ReleaseStop() => _depot.ReleaseStop();

    public ValueTask DisposeAsync() => _handle.DisposeAsync();

    private sealed class TestBusDepot(bool blockStop) : IBusDepot
    {
        private readonly TaskCompletionSource _cancellationObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _cancellationRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _start =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _startEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _stopEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _stopRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _stopCount;

        public Task CancellationObserved => _cancellationObserved.Task;

        public Task StartEntered => _startEntered.Task;

        public Task StopEntered => _stopEntered.Task;

        public int StopCount => Volatile.Read(ref _stopCount);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _startEntered.TrySetResult();

            try
            {
                await _start.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _cancellationObserved.TrySetResult();
                await _cancellationRelease.Task.ConfigureAwait(false);
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _stopCount);
            _stopEntered.TrySetResult();

            if (blockStop)
                await _stopRelease.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void CompleteStart() => _start.TrySetResult();

        public void ReleaseCancellation() => _cancellationRelease.TrySetResult();

        public void ReleaseStop() => _stopRelease.TrySetResult();
    }
}
