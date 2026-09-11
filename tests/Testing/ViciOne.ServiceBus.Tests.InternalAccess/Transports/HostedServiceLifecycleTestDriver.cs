using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Hosting;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transports;

public sealed class HostedServiceLifecycleTestDriver : IAsyncDisposable
{
    private readonly TestBusDepot _depot;
    private readonly RecordingLogger _logger = new();
    private readonly RecordingLoggerFactory _loggerFactory;
    private readonly ServiceBusHostedService _hostedService;

    public HostedServiceLifecycleTestDriver(
        bool blockFirstStop = false,
        bool waitUntilStarted = true,
        TimeSpan? startTimeout = null,
        TimeSpan? stopTimeout = null,
        TimeProvider? timeProvider = null)
    {
        _depot = new TestBusDepot(blockFirstStop);
        _loggerFactory = new RecordingLoggerFactory(_logger);
        _hostedService = new ServiceBusHostedService(
            new TestServiceProvider(_depot),
            Options.Create(new ViciOneServiceBusHostOptions
            {
                WaitUntilStarted = waitUntilStarted,
                StartTimeout = startTimeout,
                StopTimeout = stopTimeout,
            }),
            _loggerFactory,
            timeProvider);
    }

    public Task StartEntered => _depot.StartEntered;

    public Task StopEntered => _depot.StopEntered;

    public Task<Exception> BackgroundStartFailureObserved => _logger.BackgroundStartFailureObserved;

    public int StartCount => _depot.StartCount;

    public int StopCount => _depot.StopCount;

    public Task StartAsync(CancellationToken cancellationToken) => _hostedService.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => _hostedService.StopAsync(cancellationToken);

    public void CompleteStart() => _depot.CompleteStart();

    public void FailStart(Exception exception) => _depot.FailStart(exception);

    public void ReleaseStop() => _depot.ReleaseStop();

    public ValueTask DisposeAsync() => _hostedService.DisposeAsync();

    private sealed class TestServiceProvider(IBusDepot depot) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IBusDepot) ? depot : null;
    }

    private sealed class TestBusDepot(bool blockFirstStop) : IBusDepot
    {
        private readonly TaskCompletionSource _start =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _startEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _stopEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _stopRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _startCount;
        private int _stopCount;

        public Task StartEntered => _startEntered.Task;

        public Task StopEntered => _stopEntered.Task;

        public int StartCount => Volatile.Read(ref _startCount);

        public int StopCount => Volatile.Read(ref _stopCount);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _startCount);
            _startEntered.TrySetResult();
            return _start.Task.WaitAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            int call = Interlocked.Increment(ref _stopCount);
            _stopEntered.TrySetResult();

            if (blockFirstStop && call == 1)
                await _stopRelease.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
        }

        public void CompleteStart() => _start.TrySetResult();

        public void FailStart(Exception exception) => _start.TrySetException(exception);

        public void ReleaseStop() => _stopRelease.TrySetResult();
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly TaskCompletionSource<Exception> _backgroundStartFailureObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Exception> BackgroundStartFailureObserved => _backgroundStartFailureObserved.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error && exception is not null
                && string.Equals(formatter(state, exception), "Background bus startup did not complete successfully", StringComparison.Ordinal))
            {
                _backgroundStartFailureObserved.TrySetResult(exception);
            }
        }
    }

    private sealed class RecordingLoggerFactory(ILogger logger) : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => logger;

        public void Dispose()
        {
        }
    }
}
