using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Owns background bus startup and stops the bus depot when disposed.</summary>
internal sealed class AsyncBusHandle :
    IAsyncBusHandle
{
    readonly IBusDepot _depot;
    readonly SemaphoreSlim _disposeGate = new(1, 1);
    readonly ILogger<AsyncBusHandle> _logger;
    readonly IOptions<ViciOneServiceBusHostOptions> _options;
    readonly Task _startTask;
    readonly TimeProvider _timeProvider;
    readonly CancellationTokenSource _tokenSource;
    bool _stopped;

    public AsyncBusHandle(
        IBusDepot depot,
        ILogger<AsyncBusHandle> logger,
        IOptions<ViciOneServiceBusHostOptions> options,
        TimeProvider? timeProvider = null)
    {
        _depot = depot ?? throw new ArgumentNullException(nameof(depot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? TimeProvider.System;

        _tokenSource = new CancellationTokenSource();

        _logger.LogInformation("Starting ViciOne.ServiceBus");

        _startTask = Task.Run(() => depot.StartAsync(_tokenSource.Token), _tokenSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _disposeGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            if (_stopped)
                return;

            if (!_startTask.IsCompleted)
            {
                _logger.LogInformation("Canceling ViciOne.ServiceBus startup (disposed)");
                await _tokenSource.CancelAsync().ConfigureAwait(false);
            }

            try
            {
                await _startTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_tokenSource.IsCancellationRequested)
            {
                CompleteDisposal();
                return;
            }
            catch
            {
                CompleteDisposal();
                throw;
            }

            _logger.LogInformation("Stopping ViciOne.ServiceBus (disposed)");
            if (_options.Value.StopTimeout is { } stopTimeout)
            {
                using var tokenSource = new CancellationTokenSource(stopTimeout, _timeProvider);
                await _depot.StopAsync(tokenSource.Token).ConfigureAwait(false);
            }
            else
                await _depot.StopAsync(CancellationToken.None).ConfigureAwait(false);

            CompleteDisposal();
        }
        finally
        {
            _disposeGate.Release();
        }
    }

    void CompleteDisposal()
    {
        _stopped = true;
        _tokenSource.Dispose();
    }
}
