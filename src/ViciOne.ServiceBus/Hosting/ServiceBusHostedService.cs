using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

internal sealed class ServiceBusHostedService :
    IHostedService,
    IAsyncDisposable
{
    readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    readonly object _stateLock = new();
    readonly IOptions<ViciOneServiceBusHostOptions> _options;
    readonly IServiceProvider _provider;
    readonly TimeProvider _timeProvider;
    IBusDepot? _depot;
    Task? _startTask;
    bool _stopping;
    bool _stopped;

    public ServiceBusHostedService(
        IServiceProvider provider,
        IOptions<ViciOneServiceBusHostOptions> options,
        TimeProvider? timeProvider = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask DisposeAsync()
    {
        await StopCoreAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (_stopped || _stopping)
                throw new InvalidOperationException("The hosted service cannot be started after stopping has begun.");

            if (_startTask is null)
            {
                _depot = _provider.GetRequiredService<IBusDepot>();
                _startTask = ExecuteWithTimeoutAsync(
                    StartDepotAsync,
                    _options.Value.StartTimeout,
                    cancellationToken);
            }

            return _startTask.IsCompleted || _options.Value.WaitUntilStarted
                ? _startTask
                : Task.CompletedTask;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopCoreAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task StartDepotAsync(CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IBusDepot depot;
            lock (_stateLock)
                depot = _depot ?? throw new InvalidOperationException("The bus depot was not initialized before startup.");

            await depot.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (_stopped)
                return;
        }

        await ExecuteWithTimeoutAsync(StopDepotAsync, _options.Value.StopTimeout, cancellationToken).ConfigureAwait(false);
    }

    async Task StopDepotAsync(CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IBusDepot? depot;
            lock (_stateLock)
            {
                if (_stopped)
                    return;

                _stopping = true;
                depot = _depot;
            }

            if (depot is not null)
                await depot.StopAsync(cancellationToken).ConfigureAwait(false);

            lock (_stateLock)
                _stopped = true;
        }
        finally
        {
            lock (_stateLock)
                _stopping = false;

            _lifecycleGate.Release();
        }
    }

    async Task ExecuteWithTimeoutAsync(
        Func<CancellationToken, Task> operation,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        if (timeout is null)
        {
            await operation(cancellationToken).ConfigureAwait(false);
            return;
        }

        using var timeoutTokenSource = new CancellationTokenSource(timeout.Value, _timeProvider);
        if (!cancellationToken.CanBeCanceled)
        {
            await operation(timeoutTokenSource.Token).ConfigureAwait(false);
            return;
        }

        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
        await operation(linkedTokenSource.Token).ConfigureAwait(false);
    }
}
