using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport.Middleware;

/// <summary>Schedules dead-letter cleanup and auto-delete keepalive operations for a SQL receive queue.</summary>
internal sealed class SqlQueueMaintenance
{
    static readonly TimeSpan MaintenanceInterval = TimeSpan.FromSeconds(30);

    readonly ClientContext _client;
    readonly ReceiveSettings _receiveSettings;
    readonly TimeProvider _timeProvider;
    readonly TimeSpan? _touchQueueInterval;
    DateTime? _lastMaintenance;
    DateTime? _lastTouched;

    public SqlQueueMaintenance(ClientContext client, ReceiveSettings receiveSettings, TimeProvider timeProvider)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _receiveSettings = receiveSettings ?? throw new ArgumentNullException(nameof(receiveSettings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        if (receiveSettings.AutoDeleteOnIdle.HasValue)
            _touchQueueInterval = TimeSpan.FromSeconds(
                SqlTransportDefaults.ToDatabaseAutoDeleteSeconds(receiveSettings.AutoDeleteOnIdle)!.Value / 2d);
    }

    public TimeSpan MaximumPollingInterval => _touchQueueInterval is { } interval && interval < _receiveSettings.PollingInterval
        ? interval
        : _receiveSettings.PollingInterval;

    /// <summary>Runs every maintenance operation currently due, tolerating expected polling interruptions.</summary>
    public async Task RunAsync(CancellationToken cancellationToken, CancellationToken stoppingToken)
    {
        try
        {
            await RunDueOperationsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || stoppingToken.IsCancellationRequested)
        {
        }
        catch (TimeoutException)
        {
        }
    }

    async Task RunDueOperationsAsync(CancellationToken cancellationToken)
    {
        int? deadLetterCount = 0;
        DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        if (IsDue(_lastMaintenance, MaintenanceInterval, utcNow))
        {
            deadLetterCount = await _client.DeadLetterQueueAsync(
                _receiveSettings.QueueName,
                _receiveSettings.MaintenanceBatchSize,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (deadLetterCount < _receiveSettings.MaintenanceBatchSize)
                _lastMaintenance = utcNow;
        }

        if (_touchQueueInterval.HasValue
            && deadLetterCount is null or 0
            && (!_lastTouched.HasValue || _lastTouched.Value + _touchQueueInterval.Value <= utcNow))
        {
            await _client.TouchQueueAsync(_receiveSettings.EntityName, cancellationToken: cancellationToken).ConfigureAwait(false);
            _lastTouched = utcNow;
        }
    }

    static bool IsDue(DateTime? lastRun, TimeSpan interval, DateTime utcNow)
    {
        return !lastRun.HasValue || lastRun.Value + interval < utcNow;
    }
}
